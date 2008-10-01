using System.Collections.Generic;
using Lucene.Net.Index;
using NHibernate.Search.Engine;
using NHibernate.Search.Store;

namespace NHibernate.Search.Reader
{
    /// <summary>
    /// Responsible for providing and managing the lifecycle of a read-only reader
    /// <para>
    /// Note that the reader must be closed once opened.
    /// The ReaderProvider implementation must have a no-arg constructor
    /// </para>
    /// </summary>
    public interface IReaderProvider
    {
        /// <summary>
        /// Open a reader on all the listed directory providers
        /// the opened reader has to be closed through #closeReader()
        /// The opening can be virtual
        /// </summary>
        /// <param name="directoryProviders">The directory providers.</param>
        IndexReader OpenReader(IDirectoryProvider[] directoryProviders);

        /// <summary>
        ///  close a reader previously opened by #openReader
        /// The closing can be virtual
        /// </summary>
        /// <param name="reader">The reader.</param>
        void CloseReader(IndexReader reader);

        /// <summary>
        /// Initializes the reader provider before its use
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <param name="searchFactoryImplementor">The search factory implementor.</param>
        void Initialize(IDictionary<string, string> properties, ISearchFactoryImplementor searchFactoryImplementor);

        /// <summary>
        /// called when a SearchFactory is destroyed. This method typically releases resources
        /// This method is guaranteed to be executed after readers are released by queries (assuming no user error). 
        /// </summary>
        void Destroy();
    }
}