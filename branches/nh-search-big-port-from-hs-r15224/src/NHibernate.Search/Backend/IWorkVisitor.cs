namespace NHibernate.Search.Backend
{
    /// <summary>
    /// A visitor delegate to manipulate a LuceneWork
    /// needs to implement this interface.
    /// This pattern enables any implementation to virtually add delegate
    /// methods to the base LuceneWork without having to change them.
    /// This contract however breaks if more subclasses of LuceneWork
    /// are created, as a visitor must support all existing types.
    /// </summary>
    public interface IWorkVisitor<T>
    {
        T GetDelegate(AddLuceneWork addLuceneWork);
        T GetDelegate(DeleteLuceneWork deleteLuceneWork);
        T GetDelegate(OptimizeLuceneWork optimizeLuceneWork);
        T GetDelegate(PurgeAllLuceneWork purgeAllLuceneWork);
    }
}