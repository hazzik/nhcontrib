using System;

namespace NHibernate.Search
{
    /// <summary>
    /// Extends the NHibernate <seealso cref="ISession"/> with Full text search and indexing capabilities
    /// </summary>
    public interface IFullTextSession : ISession
    {
        /// <summary>
        /// Create a Query on top of a native Lucene Query returning the matching objects
        /// of <typeparam name="TEntity"/> and their respective subclasses.
        /// </summary>
        IFullTextQuery CreateFullTextQuery<TEntity>(string defaultField, string query);

        /// <summary>
        /// Create a Query on top of a native Lucene Query returning the matching objects
        /// of <typeparam name="TEntity"/> and their respective subclasses.
        /// </summary>
        IFullTextQuery CreateFullTextQuery<TEntity>(string query);

        /// <summary>
        /// Create a Query on top of a native Lucene Query returning the matching objects
        /// of type <code>entities</code> and their respective subclasses.
        /// If no entity is provided, no type filtering is done.
        /// </summary>
        IFullTextQuery CreateFullTextQuery(Lucene.Net.Search.Query luceneQuery, params System.Type[] entities);

        /// <summary>
        /// Force the (re)indexing of a given <b>managed</b> object.
        /// Indexation is batched per transaction</summary>
        /// <param name="entity">he entity to index - must not be null</param>
        IFullTextSession Index(Object entity);

        /// <summary>
        /// get the search factory
        /// </summary>
        ISearchFactory SearchFactory { get; set;}

        /// <summary>
        /// Purge the instance with the specified identity from the index, but not the database.
        /// </summary>
        /// <param name="clazz"></param>
        /// <param name="id"></param>
        void Purge(System.Type clazz, object id);

        /// <summary>
        /// Purge all instances from the index, but not the database.
        /// </summary>
        /// <param name="clazz"></param>
        void PurgeAll(System.Type clazz);

        /// <summary>
        /// flush full text changes to the index
        /// Force Hibernate Search to apply all changes to the index no waiting for the batch limit
        /// </summary>
        void FlushToIndexes();
    }
}