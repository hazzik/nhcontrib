using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NHibernate.Criterion;
using NHibernate.Dialect.Function;

namespace NHibernate.Linq.Visitors.MethodTranslators
{
	public class DBFunctionMethodTranslator:IMethodTranslator
	{
		#region IMethodTranslator Members

		public void Initialize(ISession session, ICriteria rootCriteria)
		{
			this.session = session;
			this.rootCriteria = rootCriteria;
		}

		private ISession session;
		private ICriteria rootCriteria;

		public IProjection GetProjection(MethodCallExpression expression)
		{
			var sqlfunction = new StandardSQLFunction(expression.Method.Name);

			var projections = new IProjection[expression.Arguments.Count-1];
			var criteria = DetachedCriteria.For(this.rootCriteria.GetRootEntityTypeIfAvailable());
			for (int i = 1; i < expression.Arguments.Count; i++)
			{

				var visitor = new SelectArgumentsVisitor(rootCriteria, this.session);
				visitor.Visit(expression.Arguments[i]);
				projections[i - 1] = visitor.Projection;
			}
			var returnType = NHibernateUtil.GuessType(expression.Method.ReturnType);
			return Projections.SqlFunction(sqlfunction, returnType , projections);
		}

		#endregion
	}
}