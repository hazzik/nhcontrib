namespace NHibernate.Search.Backend
{
    public class PurgeAllLuceneWork : LuceneWork
    {
        public PurgeAllLuceneWork(System.Type entity)
            : base(null, null, entity, null)
        {
        }

        public override T GetWorkDelegate<T>(IWorkVisitor<T> visitor)
        {
            return visitor.GetDelegate(this);
        }
    }
}