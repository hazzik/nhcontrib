using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NHibernate.Criterion;
using NHibernate.Linq.Expressions;
using NHibernate.Linq.Util;


namespace NHibernate.Linq.Visitors.MethodTranslators
{
	public class ListMethodTranslator : IMethodTranslator
	{
		#region IMethodTranslator Members

		public void Initialize(ISession session, ICriteria rootCriteria)
		{
			this.session = session;
			this.rootCriteria = rootCriteria;
		}

		private ISession session;
		private ICriteria rootCriteria;

		public ProjectionWithImplication GetProjection(MethodCallExpression expression)
		{
			switch (expression.Method.Name)
			{
				case "Contains":
					return GetContainsProjection(expression);
				default:
					throw new NotImplementedException(expression.Method.Name);
			}

		}
		protected virtual ProjectionWithImplication GetContainsProjection(MethodCallExpression expression)
		{
			if (expression.Object is ConstantExpression)
				return GetContainsProjectionWithConstantSource(expression);
			else
				return GetContainsProjectionWithAssociationSource(expression);
		}
		protected virtual ProjectionWithImplication GetContainsProjectionWithConstantSource(MethodCallExpression expression)
		{
			var source = expression.Object;
			var items = expression.Arguments[0];
			if (source is ConstantExpression)
			{
				var values = QueryUtil.GetExpressionValue(source) as ICollection;
				return
					new ProjectionWithImplication(Projections.Conditional(
						Restrictions.In(MemberNameVisitor.GetMemberName(this.rootCriteria, items),
										values), Projections.Constant(true), Projections.Constant(false)));
			}
			throw new InvalidOperationException("Invalid operation at EnumerableMethodTranslator");
		}
		protected virtual ProjectionWithImplication GetContainsProjectionWithAssociationSource(MethodCallExpression expression)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}