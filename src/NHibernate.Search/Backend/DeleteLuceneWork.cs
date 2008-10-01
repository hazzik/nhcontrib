namespace NHibernate.Search.Backend
{
    public class DeleteLuceneWork : LuceneWork
    {
        private const long serialVersionUID = -854604138119230246L;
        
        public DeleteLuceneWork(object id, string idInString, System.Type entityClass)
            : base(id, idInString, entityClass)
        {
        }

        public override T GetWorkDelegate<T>(IWorkVisitor<T> visitor)
        {
            return visitor.GetDelegate(this);
        }
    }
}