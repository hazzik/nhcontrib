using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using NHibernate.Criterion;
using NHibernate.Linq.Util;
using NHibernate.Linq.Expressions;
using Expression = System.Linq.Expressions.Expression;

namespace NHibernate.Linq.Visitors
{
    /// <summary>
    /// Provides ICriterion for a query given a Linq expression tree.
    /// </summary>
    public class WhereArgumentsVisitor : NHibernateExpressionVisitor
	{
		private readonly Stack<IList<ICriterion>> criterionStack = new Stack<IList<ICriterion>>();
        private readonly ICriteria rootCriteria;
        private readonly ISession session;

        public WhereArgumentsVisitor(ICriteria rootCriteria, ISession session)
		{
			criterionStack.Push(new List<ICriterion>());
			this.rootCriteria = rootCriteria;
            this.session = session;
		}

        public static IEnumerable<ICriterion> GetCriterion(ICriteria rootCriteria, ISession session, Expression expression)
        {
            var visitor = new WhereArgumentsVisitor(rootCriteria, session);
            visitor.Visit(expression);
            return visitor.CurrentCriterions;
        }

		/// <summary>
		/// Gets the current collection of <see cref="T:NHibernate.Criterion.ICriterion"/> objects.
		/// </summary>
		public IList<ICriterion> CurrentCriterions
		{
			get { return criterionStack.Peek(); }
		}

        protected override Expression VisitMethodCall(MethodCallExpression expr)
        {
        	IMethodTranslator translator=MethodTranslatorRegistry.Current.GetTranslatorInstanceForMethod(expr.Method);
			translator.Initialize(this.session,this.rootCriteria);
        	var projection = translator.GetProjection(expr);
        	var criterion = Restrictions.Eq(projection, true);
			CurrentCriterions.Add(criterion);
            return expr;
        }

        protected override Expression VisitBinary(BinaryExpression expr)
        {
            switch (expr.NodeType)
            {
                case ExpressionType.AndAlso:
                    VisitAndAlsoExpression(expr);
                    break;

                case ExpressionType.OrElse:
                    VisitOrElseExpression(expr);
                    break;

                default:
                    VisitBinaryCriterionExpression(expr);
                    break;
            }

            return expr;
        }

        private void VisitAndAlsoExpression(BinaryExpression expr)
        {
            Visit(expr.Left);
            Visit(expr.Right);
        }

        private void VisitOrElseExpression(BinaryExpression expr)
        {
            criterionStack.Push(new List<ICriterion>());
            Visit(expr.Left);
            Visit(expr.Right);
            IList<ICriterion> ors = criterionStack.Pop();

            var disjunction = new Disjunction();
            foreach (ICriterion crit in ors)
            {
                disjunction.Add(crit);
            }
            CurrentCriterions.Add(disjunction);
        }

        private void VisitBinaryCriterionExpression(BinaryExpression expr)
        {
			var projections = BinaryCriterionVisitor.GetBinaryCriteria(this.rootCriteria, this.session, expr);
        	Compare action;
        	var left = projections[0];
        	var right = projections[1];
			switch (expr.NodeType)
			{
				case ExpressionType.Equal:
					action = delegate(IProjection pr1, IProjection pr2)
					         	{
									if (pr1 == null)
										return Restrictions.IsNull(right);
									else if (pr2 == null)
										return Restrictions.IsNull(left);
									else
										return Restrictions.EqProperty(left, right);
								};
					break;

				case ExpressionType.GreaterThan:
					action = (l,r)=>Restrictions.GtProperty(left, right);
					break;

				case ExpressionType.GreaterThanOrEqual:
					action = (l, r) => Restrictions.GeProperty(left, right);
					break;

				case ExpressionType.LessThan:
					action = (l, r) => Restrictions.LtProperty(left, right);
					break;

				case ExpressionType.LessThanOrEqual:
					action = (l, r) => Restrictions.LeProperty(left, right);
					break;

				case ExpressionType.NotEqual:
					action = delegate(IProjection pr1, IProjection pr2)
					         	{
					         		if (pr1 == null)
					         			return Restrictions.IsNotNull(right);
					         		else if (pr2 == null)
					         			return Restrictions.IsNotNull(left);
					         		else
					         			return Restrictions.EqProperty(left, right);
					         	};
					break;
				default:
					throw new InvalidOperationException();
					break;
			}
        	var criterion = action(left, right);
			CurrentCriterions.Add(criterion);
        }

        protected override Expression VisitUnary(UnaryExpression expr)
        {
            switch (expr.NodeType)
            {
                case ExpressionType.Quote:
                    Visit(expr.Operand);
                    break;

                case ExpressionType.Not:
                    VisitNotExpression(expr);
                    break;
            }

            return expr;
        }

        private void VisitNotExpression(UnaryExpression expr)
        {
            var criterions = GetCriterion(rootCriteria, session, expr.Operand);

            Conjunction conjunction = Restrictions.Conjunction();
            foreach (var criterion in criterions)
                conjunction.Add(criterion);

            CurrentCriterions.Add(Restrictions.Not(conjunction));
        }

        protected override Expression VisitPropertyAccess(PropertyAccessExpression expr)
        {
            if (expr.Type == typeof(bool))
            {
                string name = MemberNameVisitor.GetMemberName(rootCriteria, expr);
				CurrentCriterions.Add(Restrictions.Eq(name, true));
            }

            return expr;
        }
	}
}
