using Lucene.Net.Documents;

namespace NHibernate.Search.Bridge
{
    /// <summary>Link between a .NET property and a Lucene Document
    /// Usually a .NET property will be linked to a Document Field
    /// </summary>
    public interface IFieldBridge
    {
        /// <summary>
        /// Manipulate the document to index the given value.
        /// A common implementation is to add a Field <code>name</code> to the given document following
        /// the parameters (<code>store</code>, <code>index</code>, <code>boost</code>) if the
        /// <code>value</code> is not null
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <param name="document">The document.</param>
        /// <param name="luceneOptions">Contains the parameters used for adding <code>value</code> to
        /// the Lucene <code>document</code>.</param>
        void Set(string name, object value, Document document, LuceneOptions luceneOptions);
    }
}