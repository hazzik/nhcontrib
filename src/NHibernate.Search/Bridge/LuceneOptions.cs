using Lucene.Net.Documents;
using NHibernate.Search.Attributes;

namespace NHibernate.Search.Bridge
{
    /// <summary> * 
    /// A wrapper class for Lucene parameters needed for indexing.
    /// </summary>
    public class LuceneOptions
    {
        private readonly Field.Store store;
        private readonly Field.Index index;
        private readonly TermVector termVector;
        private readonly float? boost;

        public LuceneOptions(Field.Store store, Field.Index index, TermVector termVector, float? boost)
        {
            this.store = store;
            this.index = index;
            this.termVector = termVector;
            this.boost = boost;
        }

        public virtual Field.Store Store
        {
            get { return store; }
        }

        public virtual Field.Index Index
        {
            get { return index; }
        }

        public virtual TermVector TermVector
        {
            get { return termVector; }
        }

        ///	<returns> 
        /// the boost value. If <code>boost == null</code>, the default boost value
        ///	1.0 is returned. 
        /// </returns>
        public virtual float Boost
        {
            get 
            {
                return boost ?? 1.0f;
            }
        }
    }
}