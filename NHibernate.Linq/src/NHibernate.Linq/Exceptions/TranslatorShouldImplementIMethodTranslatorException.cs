using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate.Linq.Visitors;

namespace NHibernate.Linq.Exceptions
{
	public class TranslatorShouldImplementIMethodTranslatorException:NHLinqException
	{
		public TranslatorShouldImplementIMethodTranslatorException(System.Type type)
			: base(string.Format("Type {0} should implement {1}", type.FullName,typeof(IMethodTranslator).FullName))
		{
			
		}
	}
}
