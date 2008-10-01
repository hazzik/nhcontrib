namespace NHibernate.Search.Attributes
{
    /// <summary>
    /// Defines the term vector storing strategy
    /// </summary>
    public enum TermVector
    {
        ///	<summary>
        /// Store term vectors. 
        /// </summary>
        Yes,
        /// <summary> 
        /// Do not store term vectors. 
        /// </summary>
        No,
        /// <summary>
        /// Store the term vector + Token offset information
        /// </summary>
        WithOffsets,
        ///	<summary>
        /// Store the term vector + token position information
        /// </summary>
        WithPositions,
        ///	<summary>
        /// Store the term vector + Token position and offset information
        /// </summary>
        WithPositionOffsets
    }
}
}