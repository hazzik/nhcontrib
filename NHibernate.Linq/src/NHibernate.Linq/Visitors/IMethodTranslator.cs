using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NHibernate.Criterion;

namespace NHibernate.Linq.Visitors
{
	public interface IMethodTranslator
	{
		void Initialize(ISession session, ICriteria rootCriteria);
		IProjection GetProjection(MethodCallExpression expression);
	}
}
