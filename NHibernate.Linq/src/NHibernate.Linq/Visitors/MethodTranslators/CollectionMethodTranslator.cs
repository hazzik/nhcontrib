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
	public class CollectionMethodTranslator:IMethodTranslator
	{
		#region IMethodTranslator Members

		public void Initialize(ISession session, ICriteria rootCriteria)
		{
			this.rootCriteria = rootCriteria;
			this.session = session;
		}

		private ISession session;
		private ICriteria rootCriteria;
		public IProjection GetProjection(MethodCallExpression expression)
		{
			switch(expression.Method.Name)
			{
				case "Contains":
					return GetContainsProjection(expression);
				default:	
					throw new NotImplementedException(string.Format("{0}.{1}",expression.Method.DeclaringType,expression.Method.Name));
			}

		}
		protected virtual IProjection GetContainsProjection(MethodCallExpression expression)
		{
			EntityExpression rootEntity = EntityExpressionVisitor.FirstEntity(expression.Object);

			DetachedCriteria query = DetachedCriteria.For(rootEntity.Type)
				.SetProjection(Projections.Id());

			var arg = (CollectionAccessExpression)expression.Object;
			var visitor = new MemberNameVisitor(query.Adapt(session), true);
			visitor.Visit(arg);

			//TODO: this won't work for collections of values
			var containedEntity = QueryUtil.GetExpressionValue(expression.Arguments[0]);
			var collectionIdPropertyName = visitor.MemberName + "." + arg.ElementExpression.MetaData.IdentifierPropertyName;
			var idValue = arg.ElementExpression.MetaData.GetIdentifier(containedEntity, EntityMode.Poco);

			query.Add(Restrictions.Eq(collectionIdPropertyName, idValue));

			string identifierName = rootEntity.MetaData.IdentifierPropertyName;
			return Projections.Conditional(Subqueries.PropertyIn(identifierName, query), Projections.Constant(true),
			                               Projections.Constant(false));
		}
		#endregion
	}
}