using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NHibernate.Criterion;
using NHibernate.Dialect.Function;
using NHibernate.Linq.Expressions;
using NHibernate.Type;

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
		public ProjectionWithImplication GetProjection(MethodCallExpression expression)
		{
			var attributes = expression.Method.GetCustomAttributes(typeof (SqlFunctionAttribute), true);
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

			return GetProjection(functionName, expression.Method.ReturnType, expression.Arguments.ToArray());
		}

		/// <summary>
		/// Provided in order to prevent code duplication in type specific method interpretation.
		/// </summary>
		/// <param name="functionName"></param>
		/// <param name="rerturntype"></param>
		/// <param name="arguments"></param>
		/// <returns></returns>
		public ProjectionWithImplication GetProjection(string functionName, System.Type returnType, params System.Linq.Expressions.Expression[] arguments)
		{
			var sqlfunction = new StandardSQLFunction(functionName);
			var projections = new IProjection[arguments.Length];
			for (int i = 0; i < arguments.Length; i++)
			{

				var visitor = new SelectArgumentsVisitor(rootCriteria, this.session);
				visitor.Visit(arguments[i]);
				projections[i] = visitor.Projection;
			}
			var type = NHibernateUtil.GuessType(returnType);
			return new ProjectionWithImplication(Projections.SqlFunction(sqlfunction, type, projections));
		}
		#endregion
	}
}