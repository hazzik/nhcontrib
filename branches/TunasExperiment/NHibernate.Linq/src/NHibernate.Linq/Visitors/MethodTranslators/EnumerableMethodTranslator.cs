using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using NHibernate.Criterion;
using NHibernate.Impl;
using NHibernate.Linq.Exceptions;
using NHibernate.Linq.Expressions;
using NHibernate.Linq.Util;
using NHibernate.SqlCommand;
using NHibernate.Transform;
using NHibernate.Type;

namespace NHibernate.Linq.Visitors.MethodTranslators
{
	public class EnumerableMethodTranslator : IMethodTranslator
	{
		public void Initialize(ISession session,ICriteria rootCriteria)
		{
			this.criteria = rootCriteria;
			this.session = session;
		}

		private ICriteria criteria;
		private ISession session;

		
		public IProjection GetProjection(MethodCallExpression expression)
		{
			return GetProjectionInternal(expression);
		}


		protected virtual IProjection GetProjectionInternal(MethodCallExpression expression)
		{
			switch (expression.Method.Name)
			{
				case "Average":
					return GetAverageProjection(expression);
				case "Count":
				case "LongCount":
					return GetCountProjection(expression);
				case "Max":
					return GetMaxProjection(expression);
				case "Min":
					return GetMinProjection(expression);
				case "Sum":
					return GetSumProjection(expression);
				case "Any":
					return GetAnyProjection(expression);
				case "Contains":
					return GetContainsProjection(expression);
				default:
					throw new MethodNotSupportedException(expression.Method);
			}
		}

		protected virtual IProjection GetAnyProjection(MethodCallExpression expression)
		{
			var detached = GetAssociatedDetachedCriteria(expression);
			var collection = expression.Arguments[0] as CollectionAccessExpression;
			EntityExpression rootEntity = EntityExpressionVisitor.FirstEntity(expression);
			string propertyName = MemberNameVisitor.GetMemberName(this.criteria, expression);
			DetachedCriteria query = DetachedCriteria.For(rootEntity.Type)
				.SetProjection(Projections.Id())
				.Add(Restrictions.IsNotEmpty(propertyName));

			if (expression.Arguments.Count > 1)//means we have lambda
			{
				var arg = (LambdaExpression)LinqUtil.StripQuotes(expression.Arguments[1]);
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
			var criterion = Subqueries.PropertyIn(identifierName, query);
			return Projections.Conditional(criterion, Projections.Constant(true), Projections.Constant(false));
		}
		protected virtual IProjection GetAverageProjection(MethodCallExpression expression)
		{
			var detached = GetAssociatedDetachedCriteria(expression);
			var collection = expression.Arguments[0] as CollectionAccessExpression;
			string alias = collection.ElementExpression.Alias;
			detached.CreateCriteria(collection.Name, alias);

			if (expression.Arguments.Count > 1)//Means we have lambda
			{
				var lambda = LinqUtil.StripQuotes(expression.Arguments[1]) as LambdaExpression;
				var visitor = new MemberNameVisitor(this.criteria);
				visitor.Visit(lambda);
				detached.SetProjection(Projections.Avg(visitor.MemberName));
			}
			var subQueryProjection = Projections.SubQuery(detached);
			return subQueryProjection;
		}
		protected virtual IProjection GetMaxProjection(MethodCallExpression expression)
		{
			var detached = GetAssociatedDetachedCriteria(expression);
			var collection = expression.Arguments[0] as CollectionAccessExpression;
			string alias = collection.ElementExpression.Alias;
			detached.CreateCriteria(collection.Name, alias);
			if(expression.Arguments.Count>1)//Means we have lambda
			{
				var lambda = LinqUtil.StripQuotes(expression.Arguments[1]) as LambdaExpression;
				var visitor = new MemberNameVisitor(this.criteria);
				visitor.Visit(lambda);
				detached.SetProjection(Projections.Max(visitor.MemberName));
			}
			var subQueryProjection = Projections.SubQuery(detached);
			return subQueryProjection;
		}
		protected virtual IProjection GetMinProjection(MethodCallExpression expression)
		{
			var detached = GetAssociatedDetachedCriteria(expression);
			var collection = expression.Arguments[0] as CollectionAccessExpression;
			string alias = collection.ElementExpression.Alias;
			detached.CreateCriteria(collection.Name, alias);
			if (expression.Arguments.Count > 1)//Means we have lambda
			{
				var lambda = LinqUtil.StripQuotes(expression.Arguments[1]) as LambdaExpression;
				var visitor = new MemberNameVisitor(this.criteria);
				visitor.Visit(lambda);
				detached.SetProjection(Projections.Min(visitor.MemberName));
			}
			var subQueryProjection = Projections.SubQuery(detached);
			return subQueryProjection;
		}
		protected virtual IProjection GetSumProjection(MethodCallExpression expression)
		{
			var detached = GetAssociatedDetachedCriteria(expression);
			var collection = expression.Arguments[0] as CollectionAccessExpression;
			string alias = collection.ElementExpression.Alias;
			detached.CreateCriteria(collection.Name, alias);
			if (expression.Arguments.Count > 1)//Means we have lambda
			{
				var lambda = LinqUtil.StripQuotes(expression.Arguments[1]) as LambdaExpression;
				var visitor = new MemberNameVisitor(this.criteria);
				visitor.Visit(lambda);
				detached.SetProjection(Projections.Sum(visitor.MemberName));
			}
			var subQueryProjection = Projections.SubQuery(detached);
			return subQueryProjection;
		}
		protected virtual IProjection GetCountProjection(MethodCallExpression expression)
		{
			var detached = GetAssociatedDetachedCriteria(expression);
			var collection = expression.Arguments[0] as CollectionAccessExpression;
			string alias = collection.ElementExpression.Alias;
			var criteria = detached.CreateCriteria(collection.Name, alias);
			if(expression.Arguments.Count>1)//Means we have lambda, horay!
			{
				var expr = expression.Arguments[1] as LambdaExpression;
				var temp = new WhereArgumentsVisitor(criteria.Adapt(session), session);
				temp.Visit(expr.Body);
				temp.CurrentCriterions.Each(x => criteria.Add(x));
			}
			EntityExpression rootEntity = EntityExpressionVisitor.RootEntity(expression);
			string identifierName = rootEntity.MetaData.IdentifierPropertyName;
			detached.SetProjection(Projections.Count(string.Format("{0}.{1}",alias, rootEntity.MetaData.IdentifierPropertyName)));
			return Projections.SubQuery(detached);
		}
		protected virtual IProjection GetContainsProjection(MethodCallExpression expression)
		{
			var source = expression.Arguments[0];
			var items = expression.Arguments[1];
			if(source is ConstantExpression)
			{
				return GetContainsProjectionWithConstantLeft(expression);
			}
			throw new InvalidOperationException("Invalid operation at EnumerableMethodTranslator");

		}
		protected virtual IProjection GetContainsProjectionWithConstantLeft(MethodCallExpression expression)
		{
			var source = expression.Arguments[0];
			var items = expression.Arguments[1];
			var values = QueryUtil.GetExpressionValue(source) as ICollection;
			return
				Projections.Conditional(
					Restrictions.In(MemberNameVisitor.GetMemberName(this.criteria, items),
									values), Projections.Constant(true), Projections.Constant(false));
		}


		protected virtual DetachedCriteria GetAssociatedDetachedCriteria(MethodCallExpression expression)
		{
			EntityExpression rootEntity = EntityExpressionVisitor.RootEntity(expression);
			var criteria = DetachedCriteria.For(rootEntity.Type,"sub");
			string identifierName = rootEntity.MetaData.IdentifierPropertyName;
			criteria.Add(
				Restrictions.EqProperty(
					this.criteria.Alias + "." + identifierName,
					"sub." + identifierName)
				);
			return criteria; 
		}	
		private string GetMemberName(MethodCallExpression expression)
		{

			var name = MemberNameVisitor.GetMemberName(this.criteria, expression.Arguments[1]);
			return name;
		}

	}
}