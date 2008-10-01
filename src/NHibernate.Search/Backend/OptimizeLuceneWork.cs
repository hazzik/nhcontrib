namespace NHibernate.Search.Backend
{
    public class OptimizeLuceneWork : LuceneWork
    {
        public OptimizeLuceneWork(System.Type entity) : base(null, null, entity)
        {
        }

        public override T GetWorkDelegate<T>(IWorkVisitor<T> visitor)
        {
            return visitor.GetDelegate(this);
        }
    }
}