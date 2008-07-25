using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NHibernate.Criterion;
using NHibernate.Dialect.Function;
using NHibernate.Linq.Expressions;

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
		//TODO: Use property position
		public IProjection GetProjection(MethodCallExpression expression)
		{
			var attributes = expression.Method.GetCustomAttributes(typeof (SqlFunctionAttribute),true);
			int propertyPosition = 0;
			string functionName;
			if (attributes != null && attributes.Length > 0)
			{
				var attribute = attributes[0] as SqlFunctionAttribute;
				propertyPosition = attribute.PropertyPosition;
				functionName = string.Format("{0}.{1}", attribute.Owner, expression.Method.Name);
			}
			else
				functionName = expression.Method.Name;

			var sqlfunction = new StandardSQLFunction(functionName);

			var projections = new IProjection[expression.Arguments.Count];
			for (int i = 0; i < expression.Arguments.Count; i++)
			{
				
				var visitor = new SelectArgumentsVisitor(rootCriteria, this.session);
				visitor.Visit(expression.Arguments[i]);
				projections[i] = visitor.Projection;
			}
			var returnType = NHibernateUtil.GuessType(expression.Method.ReturnType);
			return Projections.SqlFunction(sqlfunction, returnType , projections);
		}

		#endregion
	}
}