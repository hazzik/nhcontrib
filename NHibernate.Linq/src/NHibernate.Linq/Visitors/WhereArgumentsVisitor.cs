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

        private ICriterion GetExistsCriteria(MethodCallExpression expr)
        {
            EntityExpression rootEntity = EntityExpressionVisitor.FirstEntity(expr);
            string propertyName = MemberNameVisitor.GetMemberName(rootCriteria, expr);

            DetachedCriteria query = DetachedCriteria.For(rootEntity.Type)
                .SetProjection(Projections.Id())
                .Add(Restrictions.IsNotEmpty(propertyName));

            if (expr.Arguments.Count > 1)
            {
                var arg = (LambdaExpression)LinqUtil.StripQuotes(expr.Arguments[1]);
                string alias = arg.Parameters[0].Name;

                DetachedCriteria subquery = query.CreateCriteria(propertyName, alias);

                var temp = new WhereArgumentsVisitor(subquery.Adapt(session), session);
                temp.Visit(arg.Body);

                foreach (ICriterion c in temp.CurrentCriterions)
                {
                    subquery.Add(c);
                }
            }

            string identifierName = rootEntity.MetaData.IdentifierPropertyName;
            return Subqueries.PropertyIn(identifierName, query);
        }

		private ICriterion GetLikeCriteria(MethodCallExpression expr, MatchMode matchMode)
		{
			return Restrictions.Like(MemberNameVisitor.GetMemberName(rootCriteria, expr.Object),
                                     String.Format("{0}", QueryUtil.GetExpressionValue(expr.Arguments[0])),
                                     matchMode);
		}

        private ICriterion GetCollectionContainsCriteria(MethodCallExpression expr)
        {
            EntityExpression rootEntity = EntityExpressionVisitor.FirstEntity(expr.Object);

            DetachedCriteria query = DetachedCriteria.For(rootEntity.Type)
                .SetProjection(Projections.Id());

            var arg = (CollectionAccessExpression)expr.Object;
            var visitor = new MemberNameVisitor(query.Adapt(session), true);
            visitor.Visit(arg);

            //TODO: this won't work for collections of values
            var containedEntity = QueryUtil.GetExpressionValue(expr.Arguments[0]);
            var collectionIdPropertyName = visitor.MemberName + "." + arg.ElementExpression.MetaData.IdentifierPropertyName;
            var idValue = arg.ElementExpression.MetaData.GetIdentifier(containedEntity, EntityMode.Poco);

            query.Add(Restrictions.Eq(collectionIdPropertyName, idValue));

            string identifierName = rootEntity.MetaData.IdentifierPropertyName;
            return Subqueries.PropertyIn(identifierName, query);
        }

        private ICriterion GetCollectionContainsCriteria(Expression list, Expression containedExpr)
		{
            var values = QueryUtil.GetExpressionValue(list) as ICollection;
            
            if (values == null)
                throw new InvalidOperationException("Expression argument must be of type ICollection.");

			return Restrictions.In(MemberNameVisitor.GetMemberName(rootCriteria, containedExpr),
                                       values);
		}
	}
}
