using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NHibernate.Linq.Exceptions
{
	public class MethodNotSupportedException:QueryCannotBeTranslatedException
	{
		public MethodNotSupportedException(MethodInfo method):base(string.Format("Method {0}.{1} is not supported",method.DeclaringType.FullName,method.Name))
		{

		}

		//public MethodNotSupportedException(string message)
		//    : base(message)
		//{

		//}
		//public MethodNotSupportedException(string message, Exception innerException)
		//    : base(message, innerException)
		//{

		//}
	}
}
