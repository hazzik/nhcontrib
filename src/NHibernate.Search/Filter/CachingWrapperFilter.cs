using System.Collections;
using log4net;
using Lucene.Net.Index;
using NHibernate.Util;

namespace NHibernate.Search.Filter
{
    /// <summary> * 
    /// A slightly different version of Lucene's original <code>CachingWrapperFilter</code> which
    /// uses <code>SoftReferences</code> instead of <code>WeakReferences</code> in order to cache 
    /// the filter <code>BitSet</code>.
    /// </summary>
    /// <remarks>
    /// see org.apache.lucene.search.CachingWrapperFilter
    /// see http://opensource.atlassian.com/projects/hibernate/browse/HSEARCH-174
    /// </remarks>
    public class CachingWrapperFilter : Lucene.Net.Search.Filter
    {
        private readonly ILog log = LogManager.GetLogger(typeof(CachingWrapperFilter));

        public const int DEFAULT_SIZE = 5;

        private readonly int size;

        ///	<summary>
        /// The cache using soft references in order to store the filter bit sets. 
        /// </summary>
        [System.NonSerialized]
        private SoftLimitMRUCache cache;

        private readonly Lucene.Net.Search.Filter filter;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingWrapperFilter"/> class.
        /// </summary>
        /// <param name="filter">Filter to cache results of</param>
        public CachingWrapperFilter(Lucene.Net.Search.Filter filter)
            : this(filter, DEFAULT_SIZE)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingWrapperFilter"/> class.
        /// </summary>
        /// <param name="filter">Filter to cache results of</param>
        /// <param name="size">The size.</param>
        public CachingWrapperFilter(Lucene.Net.Search.Filter filter, int size)
        {
            this.filter = filter;
            this.size = size;
        }

        public override BitArray Bits(IndexReader reader)
        {
            if (cache == null)
            {
                log.DebugFormat("Initialising SoftLimitMRUCache with hard ref size of {}", size);
                cache = new SoftLimitMRUCache(size);
            }
            // no need for locking here, SoftLimitMRUCache manages that internally
            BitArray cached = (BitArray)cache[reader];
            if (cached != null)
            {
                return cached;
            }

            BitArray bits = filter.Bits(reader);

            cache.Put(reader, bits);

            return bits;
        }

        public override string ToString()
        {
            return GetType().Name + "(" + filter + ")";
        }

        public override bool Equals(object o)
        {
            if (!(o is CachingWrapperFilter))
                return false;
            return filter.Equals(((CachingWrapperFilter)o).filter);
        }

        public override int GetHashCode()
        {
            return filter.GetHashCode() ^ 0x1117BF25;
        }
    }
}