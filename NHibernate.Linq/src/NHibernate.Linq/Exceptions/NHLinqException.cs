using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NHibernate.Linq.Exceptions
{
	public class NHLinqException : Exception
	{
		public NHLinqException()
		{

		}

		public NHLinqException(string message)
			: base(message)
		{

		}
		public NHLinqException(string message, Exception innerException)
			: base(message, innerException)
		{

		}
	}
}
