using NHibernate.Criterion;

namespace NHibernate.Linq.Visitors
{
	public delegate ICriterion Compare(IProjection left, IProjection right);
}
