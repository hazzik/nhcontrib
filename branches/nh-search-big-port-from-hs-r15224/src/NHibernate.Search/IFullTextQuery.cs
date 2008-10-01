using Lucene.Net.Search;
using NHibernate.Search.Bridge;
using NHibernate.Transform;

namespace NHibernate.Search
{
    /// <summary>
    /// The base interface for lucene powered searches.
    /// </summary>
    public interface IFullTextQuery : IQuery
    {
        /// <summary>
        /// Returns the number of hits for this search
        /// </summary>
        /// <remarks>
        /// Caution:
        /// The number of results might be slightly different from
        ///<code>List().Count</code> if the index is
        /// not in sync with the database at the time of query.
        /// </remarks>
        int ResultSize { get; }

        /// <summary>
        /// Allows to let lucene sort the results. This is useful when you have
        ///additional sort requirements on top of the default lucene ranking.
        /// Without lucene sorting you would have to retrieve the full result set and
        /// order the hibernate objects.
        /// </summary>
        /// <param name="sort">The lucene sort object.</param>
        /// <returns>this for method chaining</returns>
        IFullTextQuery SetSort(Sort sort);

        // NH: Semi-deprecated in hibernate search, not ported
        // a preferred way is to use the @FullTextFilterDef approach
        //FullTextQuery setFilter(Filter filter);

        /// <summary>
        /// Defines the Database Query used to load the Lucene results.
        /// Useful to load a given object graph by refining the fetch modes
        /// </summary>
        /// <remarks>
        /// No projection (criteria.SetProjection() ) allowed, the root entity must be the only returned type
        /// No where restriction can be defined either.
        /// </remarks>
        /// <param name="criteria"></param>
        /// <returns></returns>
        IFullTextQuery SetCriteriaQuery(ICriteria criteria);

        /// <summary>
        /// Defines the Lucene field names projected and returned in a query result
        /// Each field is converted back to it's object representation, an Object[] being returned for each "row"
        /// (similar to an HQL or a Criteria API projection).
        /// </summary>
        /// <remarks>
        /// A projectable field must be stored in the Lucene index and use a <seealso cref="ITwoWayFieldBridge"/>
        /// Unless notified in their JavaDoc, all built-in bridges are two-way. All [DocumentId] fields are projectable by design.
        /// If the projected field is not a projectable field, null is returned in the object[]
        /// </remarks>
        /// <param name="fields"></param>
        /// <returns></returns>
        IFullTextQuery SetProjection(params string[] fields);

        /// <summary>
        /// Enable a given filter by its name. Returns a FullTextFilter object that allows filter parameter injection
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IFullTextFilter EnableFullTextFilter(string name);

        /// <summary>
        ///  Disable a given filter by its name
        /// </summary>
        /// <param name="name"></param>
        void DisableFullTextFilter(string name);

        ///	 <summary> 
        /// Return the Lucene <seealso cref="Explanation"/>
        ///	object describing the score computation for the matching object/document
        ///	in the current query
        ///	</summary>
        ///	<param name="documentId"> Lucene Document id to be explain. This is NOT the object id </param>
        ///	<returns> Lucene Explanation </returns>
        Explanation Explain(int documentId);

        ///	<seealso cref="IQuery.SetFirstResult"/>
        new IFullTextQuery SetFirstResult(int firstResult);

        ///	<seealso cref="IQuery.SetMaxResults"/>
        new IFullTextQuery SetMaxResults(int maxResults);

        ///	<summary> Defines scrollable result fetch size </summary>
        IFullTextQuery SetFetchSize(int i);

        ///	 <summary>defines a result transformer used during projection, the Aliases provided are the projection aliases. </summary>
        new IFullTextQuery SetResultTransformer(IResultTransformer transformer);
    }
}