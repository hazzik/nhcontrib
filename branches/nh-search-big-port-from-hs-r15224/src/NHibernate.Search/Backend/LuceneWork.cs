using System.Threading;
using Lucene.Net.Documents;

namespace NHibernate.Search.Backend
{
    /// <summary>
    /// Represent a Serializable Lucene unit work
    /// WARNING: This class aims to be serializable and passed in an asynchronous way across VMs
    ///          any non backward compatible serialization change should be done with great care
    ///          and publically announced. Specifically, new versions of Hibernate Search should be
    ///          able to handle changes produced by older versions of Hibernate Search if reasonably possible.
    ///          That is why each subclass susceptible to be pass along have a magic serialization number.
    ///          NOTE: we are relying on Lucene's Document to play nice unfortunately
    /// </summary>
    public abstract class LuceneWork
    {
        private readonly Document document;
        private readonly System.Type entityClass;
        private readonly object id;
        private readonly string idInString;

        /// <summary>
        /// Flag indicating that the lucene work has to be indexed in batch mode
        /// </summary>
        private bool isBatch;

        #region Constructors

        protected LuceneWork(object id, string idInString, System.Type entityClass)
            : this(id, idInString, entityClass, null)
        {
        }

        protected LuceneWork(object id, string idInString, System.Type entityClass, Document document)
        {
            this.id = id;
            this.idInString = idInString;
            this.entityClass = entityClass;
            this.document = document;
        }

        protected LuceneWork(object id, string idInString, System.Type entityClass, Document document, bool isBatch)
        {
            this.document = document;
            this.entityClass = entityClass;
            this.id = id;
            this.idInString = idInString;
            this.isBatch = isBatch;
        }

        #endregion

        #region Property methods

        public System.Type EntityClass
        {
            get { return entityClass; }
        }

        public object Id
        {
            get { return id; }
        }

        public string IdInString
        {
            get { return idInString; }
        }

        public Document Document
        {
            get { return document; }
        }

        public bool IsBatch
        {
            get { return isBatch; }
            set { isBatch = value; }
        }

        #endregion

        public abstract T GetWorkDelegate<T>(IWorkVisitor<T> visitor);
    }
}