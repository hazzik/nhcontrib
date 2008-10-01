using Lucene.Net.Documents;

namespace NHibernate.Search.Backend
{
    public class AddLuceneWork : LuceneWork
    {
        private const long serialVersionUID = -2450349312813297371L;

        public AddLuceneWork(object id, string idInString, System.Type entityClass, Document document)
            : base(id, idInString, entityClass, document)
        {
        }

        public override T GetWorkDelegate<T>(IWorkVisitor<T> visitor)
        {
            return visitor.GetDelegate(this);
        }
    }
}