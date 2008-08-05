using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate.Criterion;
using NHibernate.Linq.Expressions;

namespace NHibernate.Linq.Visitors
{
	public class CollectionProjection:IProjection
	{
		public CollectionProjection(CollectionAccessExpression expression)
		{
			this.expression = expression;
		}

		internal CollectionAccessExpression expression;
		#region IProjection Members

		public string[] Aliases
		{
			get { throw new NotImplementedException(); }
		}

		public string[] GetColumnAliases(string alias, int loc)
		{
			throw new NotImplementedException();
		}

		public string[] GetColumnAliases(int loc)
		{
			throw new NotImplementedException();
		}

		public NHibernate.Engine.TypedValue[] GetTypedValues(ICriteria criteria, ICriteriaQuery criteriaQuery)
		{
			throw new NotImplementedException();
		}

		public NHibernate.Type.IType[] GetTypes(string alias, ICriteria criteria, ICriteriaQuery criteriaQuery)
		{
			throw new NotImplementedException();
		}

		public NHibernate.Type.IType[] GetTypes(ICriteria criteria, ICriteriaQuery criteriaQuery)
		{
			throw new NotImplementedException();
		}

		public bool IsAggregate
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsGrouped
		{
			get { throw new NotImplementedException(); }
		}

		public NHibernate.SqlCommand.SqlString ToGroupSqlString(ICriteria criteria, ICriteriaQuery criteriaQuery, IDictionary<string, IFilter> enabledFilters)
		{
			throw new NotImplementedException();
		}

		public NHibernate.SqlCommand.SqlString ToSqlString(ICriteria criteria, int position, ICriteriaQuery criteriaQuery, IDictionary<string, IFilter> enabledFilters)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
