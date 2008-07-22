using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NHibernate.Criterion;
using NHibernate.Linq.Exceptions;
using NHibernate.Linq.Expressions;
using NHibernate.Linq.Util;

namespace NHibernate.Linq.Visitors.MethodTranslators
{
	public class QueryableMethodTranslator:IMethodTranslator
	{
		public void Initialize(ISession session, ICriteria rootCriteria)
		{
			this.session = session;
			this.rootCriteria = rootCriteria;
		}
		private  ISession session;
		private ICriteria rootCriteria;
		public IProjection GetProjection(MethodCallExpression expression)
		{
			switch(expression.Method.Name)
			{
				case "Count":
				case "LongCount":
					return GetCountProjection(expression);
				default:
					return GetAggregateProjection(expression);

			}
		}
		protected IProjection GetAggregateProjection(MethodCallExpression expression)
		{
			if (!(expression.Arguments.Count > 1))
				throw new InvalidOperationException();
			var lambda = LinqUtil.StripQuotes(expression.Arguments[1]) as LambdaExpression;
			var temp = new SelectArgumentsVisitor(this.rootCriteria, this.session);
			temp.Visit(lambda.Body);
			Func<IProjection,IProjection> action;
			switch (expression.Method.Name)
			{
				case "Average":
					action = Projections.Avg;
					break;
				case "Min":
					action = Projections.Min;
					break;
				case "Max":
					action = Projections.Max;
					break;
				case "Sum":
					action = Projections.Sum;
					break;
				default:
					throw new InvalidOperationException();
			}
			return action(temp.Projection);

		}

		public virtual IProjection GetCountProjection(MethodCallExpression expression)
		{
			if (expression.Arguments.Count > 1)//Means we have lambda, horay!
			{
				var lambda = LinqUtil.StripQuotes(expression.Arguments[1]) as LambdaExpression;
				var temp = new WhereArgumentsVisitor(this.rootCriteria, this.session);
				temp.Visit(lambda.Body);

				temp.CurrentCriterions.Each(x=>this.rootCriteria.Add(x));
			}
			return Projections.RowCount();
		}
	}
}