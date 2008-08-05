using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate.Criterion;

namespace NHibernate.Linq.Visitors
{
	public class ProjectionWithImplication
	{
		public ProjectionWithImplication(IProjection projection,Action<ICriteria> action)
		{
			this.Action = action;
			this.Projections = projection;
		}
		public ProjectionWithImplication(IProjection projection)
		{
			this.Action = delegate { };
			this.Projections = projection;
		}
		public IProjection Projections { get; set; }
		public Action<ICriteria> Action { get; set; }
	}
}
