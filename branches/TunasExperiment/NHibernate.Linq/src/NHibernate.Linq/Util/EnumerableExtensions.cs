using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NHibernate.Linq.Util
{
	public static class EnumerableExtensions
	{
		public static void Each<T>(this IEnumerable<T> source,Action<T> action)
		{
			foreach (var s in source)
				action(s);
		}
	}
}
