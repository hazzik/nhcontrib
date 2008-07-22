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
                //convert to the type of the operand, not the type of the conversion
                Visit(expr.Operand);
            }

            return expr;
        }


		public static IProjection[] GetBinaryCriteria(
			ICriteria rootCriteria,
			ISession session,
			BinaryExpression expr)
		{
			var projections = new IProjection[2];
			var leftVisitor = new BinaryCriterionVisitor(rootCriteria, session);
			var rightVisitor = new BinaryCriterionVisitor(rootCriteria, session);

			if (expr.Left is ConstantExpression && ((ConstantExpression)expr.Left).Value == null)
				projections[0] = null;
			else
			{
				leftVisitor.Visit(expr.Left);
				projections[0] = leftVisitor.Projection;
			}
			if (expr.Right is ConstantExpression && ((ConstantExpression)expr.Right).Value == null)
				projections[1] = null;
			else
			{
				rightVisitor.Visit(expr.Right);
				projections[1] = rightVisitor.Projection;
			}
			return projections;
		}
    }
	
}
