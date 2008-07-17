using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NHibernate.Linq.Exceptions
{
	public class MethodTranslatorNotRegistered:NHLinqException
	{
		public MethodTranslatorNotRegistered(System.Type type)
			: base(string.Format("Translator for type {0} is not registered", type.FullName))
		{
			
		}
	}
}
