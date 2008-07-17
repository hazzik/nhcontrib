using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NHibernate.Linq.Exceptions
{
	public class MethodTranslatorDefaultConstructorMissingException:NHLinqException
	{
		public MethodTranslatorDefaultConstructorMissingException(System.Type type)
			: base(string.Format("Default constructor for type {0} is missing",type.FullName))
		{
			
		}
	}
}
