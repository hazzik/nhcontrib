namespace NHibernate.Search.Attributes
{
    /// <summary>
    /// Cache mode strategy for Full Text filters
    /// </summary>
    public enum FilterCacheModeType
    {
        ///<summary>
        /// No filter instance and no result is cached by Hibernate Search
        /// For every filter call, a new filter instance is created 
        /// </summary>
        None,

        /// <summary> 
        /// The filter instance is cached by Hibernate Search and reused across
        ///	concurrent filter.Bits() calls
        ///	Results are not cache by Hibernate Search 
        /// </summary>
        InstanceOnly,

        ///	<summary> 
        /// Both the filter instance and the BitSet results are cached.
        ///	The filter instance is cached by Hibernate Search and reused across
        ///	concurrent filter.Bits() calls
        ///	BitSet Results are cached per IndexReader  
        /// </summary>
        InstanceAndBitResults
    }
}