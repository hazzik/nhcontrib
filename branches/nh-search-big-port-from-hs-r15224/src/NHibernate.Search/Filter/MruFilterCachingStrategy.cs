using System.Collections.Generic;
using System.Collections.Specialized;
using NHibernate.Search.Backend.Configuration;
using NHibernate.Util;

namespace NHibernate.Search.Filter
{
    /// <summary> 
    /// Keep the most recently used Filters in the cache
    /// The cache is at least as big as <code>hibernate.search.filter.cache_strategy.size</code>
    /// Above this limit, Filters are kept as soft references
    /// </summary>
    public class MRUFilterCachingStrategy : IFilterCachingStrategy
    {
        private const int DEFAULT_SIZE = 128;
        private SoftLimitMRUCache cache;
        private const string SIZE = Environment.FilterCachingStrategy + ".size";

        public virtual void Initialize(IDictionary<string,string> properties)
        {
            int size = ConfigurationParseHelper.GetIntValue(properties, SIZE, DEFAULT_SIZE);
            cache = new SoftLimitMRUCache(size);
        }

        public virtual Lucene.Net.Search.Filter GetCachedFilter(FilterKey key)
        {
            return (Lucene.Net.Search.Filter)cache[key];
        }

        public virtual void AddCachedFilter(FilterKey key, Lucene.Net.Search.Filter filter)
        {
            cache.Put(key, filter);
        }
    }
}