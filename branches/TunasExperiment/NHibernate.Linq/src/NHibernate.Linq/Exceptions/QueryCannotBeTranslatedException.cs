using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NHibernate.Linq.Exceptions
{
	public class QueryCannotBeTranslatedException:NHLinqException
	{
		public QueryCannotBeTranslatedException()
		{

		}

		public QueryCannotBeTranslatedException(string message)
			: base(message)
		{

		}
		public QueryCannotBeTranslatedException(string message, Exception innerException)
			: base(message, innerException)
		{

		}
	}
}
