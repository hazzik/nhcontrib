using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using NHibernate.Type;
using NHibernate.SqlCommand;
using NHibernate.Metadata;
using NHibernate.Criterion;
using NHibernate.Linq.Expressions;
using NHibernate.Linq.Util;
using Expression = System.Linq.Expressions.Expression;

namespace NHibernate.Linq.Visitors
{
    /// <summary>
    /// Visits a BinaryExpression providing the appropriate NHibernate ICriterion.
    /// </summary>
    public class BinaryCriterionVisitor : NHibernateExpressionVisitor
    {
        private readonly ICriteria rootCriteria;
        private readonly ISession session;

        public BinaryCriterionVisitor(ICriteria rootCriteria, ISession session)
        {
            this.rootCriteria = rootCriteria;
            this.session = session;
        }

		public IProjection Projection { get; protected set; }
		public System.Type ConvertTo { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression expr)
        {
        	var translator = MethodTranslatorRegistry.Current.GetTranslatorInstanceForMethod(expr.Method);
			translator.Initialize(session,rootCriteria);
        	this.Projection = translator.GetProjection(expr);
            return expr;
        }

        protected override Expression VisitConstant(ConstantExpression expr)
        {
        	this.Projection = Projections.Constant(QueryUtil.GetExpressionValue(expr));
            return expr;
        }
		protected override Expression VisitBinary(BinaryExpression b)
		{
			var selectVisitor = new SelectArgumentsVisitor(this.rootCriteria, this.session);
			selectVisitor.Visit(b);
			this.Projection = selectVisitor.Projection;
			return b;
		}
        protected override Expression VisitEntity(EntityExpression expr)
        {
            string name = MemberNameVisitor.GetMemberName(rootCriteria, expr);
			this.Projection = Projections.Property(name);
            return expr;
        }

        protected override Expression VisitPropertyAccess(PropertyAccessExpression expr)
        {
			string name = MemberNameVisitor.GetMemberName(rootCriteria, expr);
			this.Projection = Projections.Property(name);
            return expr;
        }

        protected override Expression VisitCollectionAccess(CollectionAccessExpression expr)
        {
            return VisitPropertyAccess(expr);
        }

        protected override Expression VisitUnary(UnaryExpression expr)
        {
            if (expr.NodeType == ExpressionType.Convert)
            {
				ConvertTo = expr.Operand.Type;
                //convert to the type of the operand, not the type of the conversion
                Visit(expr.Operand);
            }

            return expr;
        }


		public static ICriterion GetBinaryCriteria(
			ICriteria rootCriteria,
			ISession session,
			BinaryExpression expr)
		{
			var projections = new IProjection[2];
			var leftVisitor = new BinaryCriterionVisitor(rootCriteria, session);
			var rightVisitor = new BinaryCriterionVisitor(rootCriteria, session);
			leftVisitor.Visit(expr.Left);
			rightVisitor.Visit(expr.Right);
			Func<IProjection, IProjection, ICriterion> action = delegate { throw new InvalidOperationException(); };

			switch (expr.NodeType)
			{
				case ExpressionType.Equal:
					action = delegate(IProjection lp, IProjection rp)
					         	{
									if(expr.Right.NodeType==ExpressionType.Constant)
									{
										var right = expr.Right as ConstantExpression;
										if (right.Value == null)
											return Restrictions.IsNull(lp);
										else if(lp is PropertyProjection && leftVisitor.ConvertTo!=null)
										{
											var leftProjectionAsProp = lp as PropertyProjection;
											return Restrictions.Eq(leftProjectionAsProp.PropertyName,
											                LinqUtil.ChangeType(right.Value, leftVisitor.ConvertTo));
										}
										return Restrictions.Eq(leftVisitor.Projection,right.Value);
									}
									else
										return Restrictions.EqProperty(leftVisitor.Projection, rightVisitor.Projection);
					         	};
					break;

				case ExpressionType.GreaterThan:
					action = Restrictions.GtProperty;
					break;

				case ExpressionType.GreaterThanOrEqual:
					action = Restrictions.GeProperty;
					break;

				case ExpressionType.LessThan:
					action = Restrictions.LtProperty;
					break;

				case ExpressionType.LessThanOrEqual:
					action = Restrictions.LeProperty;
					break;

				case ExpressionType.NotEqual:
					action = delegate(IProjection lp, IProjection rp)
					{
						ICriterion criterion;
						if (expr.Right.NodeType == ExpressionType.Constant)
						{
							var right = expr.Right as ConstantExpression;
							if (right.Value == null)
								criterion= Restrictions.IsNull(lp);
							else if (lp is PropertyProjection && leftVisitor.ConvertTo != null)
							{
								var leftProjectionAsProp = lp as PropertyProjection;
								criterion=Restrictions.Eq(leftProjectionAsProp.PropertyName,
												LinqUtil.ChangeType(right.Value, leftVisitor.ConvertTo));
							}
							else
								criterion = Restrictions.Eq(leftVisitor.Projection, right.Value);
						}
						else
							criterion = Restrictions.EqProperty(leftVisitor.Projection, rightVisitor.Projection);
						return Restrictions.Not(criterion);
					};
					break;
			}
	
			return action(leftVisitor.Projection,rightVisitor.Projection);
		}
    }
	
}
