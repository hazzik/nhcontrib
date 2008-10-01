namespace NHibernate.Search.Attributes
{
    /// <summary>
    /// Whether or not the value is stored in the document
    /// </summary>
    public enum Store
    {
        /// <summary>
        /// does not store the value in the index
        /// </summary>
        No,
        /// <summary>
        /// stores the value in the index
        /// </summary>
        Yes,
        /// <summary>
        /// stores the value in the index in a compressed form
        /// </summary>
        Compress
    }
}