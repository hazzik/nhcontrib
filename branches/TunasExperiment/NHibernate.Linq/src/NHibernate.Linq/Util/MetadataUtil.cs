using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate.Metadata;

namespace NHibernate.Linq.Util
{
	public static class MetadataUtil
	{
		public static IClassMetadata GetClassMetadata(this ISession session,System.Type type)
		{
			return session.SessionFactory.GetClassMetadata(type);
		}
	}
}
