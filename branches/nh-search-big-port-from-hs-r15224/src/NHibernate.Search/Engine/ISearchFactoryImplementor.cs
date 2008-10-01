using System.Collections.Generic;
using Iesi.Collections.Generic;
using NHibernate.Search.Backend;
using NHibernate.Search.Filter;
using NHibernate.Search.Store;
using NHibernate.Search.Store.Optimization;

namespace NHibernate.Search.Engine
{
    /// <summary>
    /// Interface which gives access to the different directory providers and their configuration.
    /// </summary>
    public interface ISearchFactoryImplementor : ISearchFactory
    {
        IBackendQueueProcessorFactory BackendQueueProcessorFactory { get; set; }

        IDictionary<System.Type, DocumentBuilder> DocumentBuilders { get; }

        IWorker Worker { get; }

        void AddOptimizerStrategy(IDirectoryProvider provider, IOptimizerStrategy optimizerStrategy);

        IOptimizerStrategy GetOptimizerStrategy(IDirectoryProvider provider);

        IFilterCachingStrategy FilterCachingStrategy { get; }

        FilterDef GetFilterDefinition(string name);

        LuceneIndexingParameters GetIndexingParameters(IDirectoryProvider provider);

        void AddIndexingParameters(IDirectoryProvider provider, LuceneIndexingParameters indexingParameters);

        string IndexingStrategy { get; }

        void Close();

        void AddClassToDirectoryProvider(System.Type clazz, IDirectoryProvider directoryProvider);

        ISet<System.Type> GetClassesInDirectoryProvider(IDirectoryProvider directoryProvider);

        IEnumerable<IDirectoryProvider> DirectoryProviders { get; }

        object GetDirectoryProviderLock(IDirectoryProvider dp);

        void AddDirectoryProvider(IDirectoryProvider provider);

        int FilterCacheBitResultsSize { get; }
    }
}