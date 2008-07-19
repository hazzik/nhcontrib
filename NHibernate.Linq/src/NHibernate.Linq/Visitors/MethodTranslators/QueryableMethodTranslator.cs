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
		public IProjection GetProjection(System.Linq.Expressions.MethodCallExpression expression)
		{
			return null;
		}
	}
}