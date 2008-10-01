using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Iesi.Collections.Generic;
using log4net;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using NHibernate.Cfg;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Mapping;
using NHibernate.Search.Attributes;
using NHibernate.Search.Backend;
using NHibernate.Search.Backend.Configuration;
using NHibernate.Search.Backend.Impl;
using NHibernate.Search.Cfg;
using NHibernate.Search.Engine;
using NHibernate.Search.Filter;
using NHibernate.Search.Reader;
using NHibernate.Search.Store;
using NHibernate.Search.Store.Optimization;
using NHibernate.Util;

namespace NHibernate.Search.Impl
{
    public class SearchFactoryImpl : ISearchFactoryImplementor
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SearchFactoryImpl));
        private static readonly object searchFactoryKey = new object();

        //it's now a <Configuration, SearchFactory> map
        [ThreadStatic]
        private static WeakHashtable contexts;

        private readonly Dictionary<System.Type, DocumentBuilder> documentBuilders =
            new Dictionary<System.Type, DocumentBuilder>();

        // Keep track of the index modifiers per DirectoryProvider since multiple entity can use the same directory provider
        private readonly Dictionary<IDirectoryProvider, DirectoryProviderData> dirProviderData =
            new Dictionary<IDirectoryProvider, DirectoryProviderData>();

        private readonly Dictionary<IDirectoryProvider, IOptimizerStrategy> dirProviderOptimizerStrategy =
            new Dictionary<IDirectoryProvider, IOptimizerStrategy>();

        private readonly IWorker worker;
        private readonly IReaderProvider readerProvider;
        private IBackendQueueProcessorFactory backendQueueProcessorFactory;
        private readonly Dictionary<string, FilterDef> filterDefinitions = new Dictionary<string, FilterDef>();
        private IDictionary<string, Analyzer> analyzers;
        private IFilterCachingStrategy filterCachingStrategy;

        // 1 for true, 0 for false
        private int stopped = 0;

        /*
         * Each directory provider (index) can have its own performance settings
         */

        private readonly Dictionary<IDirectoryProvider, LuceneIndexingParameters> dirProviderIndexingParams =
            new Dictionary<IDirectoryProvider, LuceneIndexingParameters>();

        private string indexingStrategy = "event";
        private int cacheBitResultsSize;

        public int FilterCacheBitResultsSize
        {
            get { return cacheBitResultsSize; }
        }

        #region Constructors

        private SearchFactoryImpl(Configuration cfg)
        {
            INHSConfiguration configure = CfgHelper.Configure(cfg);

            this.indexingStrategy = DefineIndexingStrategy(configure);

            Analyzer analyzer = InitAnalyzer(cfg);
            InitDocumentBuilders(configure, cfg, analyzer);

            ISet<System.Type> classes = new HashedSet<System.Type>(documentBuilders.Keys);
            foreach (DocumentBuilder documentBuilder in documentBuilders.Values)
                documentBuilder.PostInitialize(classes);
            worker = WorkerFactory.CreateWorker(configure, this);
            readerProvider = ReaderProviderFactory.CreateReaderProvider(cfg, this);
            BuildFilterCachingStrategy(cfg.Properties);
            this.filterCachingStrategy = BuildFilterCachingStrategy(cfg.Properties);
            this.cacheBitResultsSize = ConfigurationParseHelper.GetIntValue(cfg.Properties, Environment.CacheBitResultSize,
                CachingWrapperFilter.DEFAULT_SIZE);

        }

        private static String DefineIndexingStrategy(INHSConfiguration cfg)
        {
            String indexingStrategy;
            if (cfg.Properties.TryGetValue(Environment.IndexingStrategy, out indexingStrategy) == false)
                indexingStrategy = "event";

            if (!("event".Equals(indexingStrategy, StringComparison.InvariantCultureIgnoreCase) ||
                "manual".Equals(indexingStrategy, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new SearchException(Environment.IndexingStrategy + " unknown: " + indexingStrategy);
            }
            return indexingStrategy;
        }

        #endregion

        #region Property methods

        public string IndexingStrategy
        {
            get { return indexingStrategy; }
        }

        public IBackendQueueProcessorFactory BackendQueueProcessorFactory
        {
            get { return backendQueueProcessorFactory; }
            set { backendQueueProcessorFactory = value; }
        }

        public IDictionary<System.Type, DocumentBuilder> DocumentBuilders
        {
            get { return documentBuilders; }
        }

        public ISet<System.Type> GetClassesInDirectoryProvider(IDirectoryProvider directoryProvider)
        {
            DirectoryProviderData value;
            if (dirProviderData.TryGetValue(directoryProvider, out value) == false)
                return new HashedSet<System.Type>();
            return (ISet<System.Type>)value.classes.Clone();
        }

        public IFilterCachingStrategy FilterCachingStrategy
        {
            get { return filterCachingStrategy; }
        }

        public IReaderProvider ReaderProvider
        {
            get { return readerProvider; }
        }

        public IWorker Worker
        {
            get { return worker; }
        }

        #endregion

        #region Private methods

        private string GetProperty(IDictionary<string, string> props, string key)
        {
            return props.ContainsKey(key) ? props[key] : string.Empty;
        }

        private static Analyzer InitAnalyzer(Configuration cfg)
        {
            System.Type analyzerClass;

            String analyzerClassName = cfg.GetProperty(Environment.AnalyzerClass);
            if (analyzerClassName != null)
                try
                {
                    analyzerClass = ReflectHelper.ClassForName(analyzerClassName);
                }
                catch (Exception e)
                {
                    throw new SearchException(
                        string.Format("Lucene analyzer class '{0}' defined in property '{1}' could not be found.",
                                      analyzerClassName, Environment.AnalyzerClass), e);
                }
            else
                analyzerClass = typeof(StandardAnalyzer);
            // Initialize analyzer
            Analyzer defaultAnalyzer;
            try
            {
                defaultAnalyzer = (Analyzer)Activator.CreateInstance(analyzerClass);
            }
            catch (InvalidCastException)
            {
                throw new SearchException(
                    string.Format("Lucene analyzer does not implement {0}: {1}", typeof(Analyzer).FullName,
                                  analyzerClassName)
                    );
            }
            catch (Exception)
            {
                throw new SearchException("Failed to instantiate lucene analyzer with type " + analyzerClassName);
            }
            return defaultAnalyzer;
        }

        private void BindFilterDef(FullTextFilterDefAttribute defAnn, System.Type mappedClass)
        {
            if (filterDefinitions.ContainsKey(defAnn.Name))
                throw new SearchException("Multiple definitions of FullTextFilterDef.Name = " + defAnn.Name + ":" +
                                          mappedClass.FullName);

            FilterDef filterDef = new FilterDef();
            filterDef.Impl = defAnn.Impl;
            try
            {
                Activator.CreateInstance(filterDef.Impl);
            }
            catch (Exception e)
            {
                throw new SearchException("Unable to create Filter class: " + filterDef.Impl.FullName, e);
            }

            foreach (MethodInfo method in filterDef.Impl.GetMethods())
            {
                if (AttributeUtil.HasAttribute<FactoryAttribute>(method))
                {
                    if (filterDef.FactoryMethod != null)
                        throw new SearchException("Multiple Factory methods found " + defAnn.Name + ":" +
                                                  filterDef.Impl.FullName + "." + method.Name);
                    filterDef.FactoryMethod = method;
                }
                if (AttributeUtil.HasAttribute<KeyAttribute>(method))
                {
                    if (filterDef.KeyMethod != null)
                        throw new SearchException("Multiple Key methods found " + defAnn.Name + ":" +
                                                  filterDef.Impl.FullName + "." + method.Name);
                    filterDef.KeyMethod = method;
                }
                // NB Don't need the setter logic that Java has
            }
            filterDefinitions[defAnn.Name] = filterDef;
        }

        private void BindFilterDefs(System.Type mappedClass)
        {
            // We only need one test here as we just support multiple FullTextFilter attributes rather than a collection
            foreach (FullTextFilterDefAttribute defAnn in AttributeUtil.GetAttributes<FullTextFilterDefAttribute>(mappedClass))
                BindFilterDef(defAnn, mappedClass);
        }

        private static IFilterCachingStrategy BuildFilterCachingStrategy(IDictionary<string, string> properties)
        {
            IFilterCachingStrategy strategy;
            String impl;
            properties.TryGetValue(Environment.FilterCachingStrategy, out impl);
            if (string.IsNullOrEmpty(impl) || impl.Equals("mru", StringComparison.InvariantCultureIgnoreCase))
            {
                strategy = new MRUFilterCachingStrategy();
            }
            else
            {
                try
                {
                    System.Type filterCachingStrategyClass = ReflectHelper.ClassForName(impl);
                    strategy = (IFilterCachingStrategy)Activator.CreateInstance(filterCachingStrategyClass);
                }
                catch (Exception e)
                {
                    throw new SearchException("Unable to instantiate filterCachingStrategy class: " + impl, e);
                }
            }
            strategy.Initialize(properties);
            return strategy;
        }

        private void InitDocumentBuilders(INHSConfiguration nhsCfg, Configuration cfg, Analyzer analyzer)
        {
            InitContext context = new InitContext(nhsCfg);
            DirectoryProviderFactory factory = new DirectoryProviderFactory();
            foreach (PersistentClass clazz in cfg.ClassMappings)
            {
                System.Type mappedClass = clazz.MappedClass;
                if (mappedClass != null)
                {
                    if (AttributeUtil.HasAttribute<IndexedAttribute>(mappedClass))
                    {
                        DirectoryProviderFactory.DirectoryProviders providers =
                            factory.CreateDirectoryProviders(mappedClass, cfg, this);

                        DocumentBuilder documentBuilder = new DocumentBuilder(mappedClass, context, providers.Providers, providers.SelectionStrategy);

                        documentBuilders[mappedClass] = documentBuilder;
                    }
                    BindFilterDefs(mappedClass);
                }
            }
            analyzers = context.InitLazyAnalyzers();
            factory.StartDirectoryProviders();
        }

        #endregion

        #region Public methods

        public IEnumerable<IDirectoryProvider> DirectoryProviders
        {
            get { return dirProviderData.Keys; }
        }


        public IDirectoryProvider[] GetDirectoryProviders(System.Type entity)
        {
            DocumentBuilder value;
            if (DocumentBuilders.TryGetValue(entity, out value) == false)
                return null;
            return value.DirectoryProviders;
        }

        public static SearchFactoryImpl GetSearchFactory(Configuration cfg)
        {
            if (contexts == null)
                contexts = new WeakHashtable();
            SearchFactoryImpl searchFactory = (SearchFactoryImpl)contexts[cfg];
            if (searchFactory == null)
            {
                searchFactory = new SearchFactoryImpl(cfg);
                contexts[cfg] = searchFactory;
            }
            return searchFactory;
        }

        public DocumentBuilder GetDocumentBuilder(object entity)
        {
            System.Type type = NHibernateUtil.GetClass(entity);
            return GetDocumentBuilder(type);
        }

        public DocumentBuilder GetDocumentBuilder(System.Type type)
        {
            DocumentBuilder builder;
            DocumentBuilders.TryGetValue(type, out builder);
            return builder;
        }

        public object GetLockObjForDirectoryProvider(IDirectoryProvider provider)
        {
            return dirProviderData[provider];
        }

        public object GetDirectoryProviderLock(IDirectoryProvider dp)
        {
            DirectoryProviderData value;
            if(dirProviderData.TryGetValue(dp, out value)==false)
                return null;

            return value.dirLock;
        }

        public FilterDef GetFilterDefinition(string name)
        {
            FilterDef value;
            if (filterDefinitions.TryGetValue(name, out value) == false)
                return null;
            return value;
        }

        public IOptimizerStrategy GetOptimizerStrategy(IDirectoryProvider provider)
        {
            DirectoryProviderData value;
            if (dirProviderData.TryGetValue(provider, out value) == false)
                return null;
            return value.optimizerStrategy;
        }

        public void Optimize()
        {
            ICollection<System.Type> clazzes = DocumentBuilders.Keys;
            foreach (System.Type clazz in clazzes)
                Optimize(clazz);
        }

        public void Optimize(System.Type entityType)
        {
            if (!DocumentBuilders.ContainsKey(entityType))
                throw new SearchException("Entity not indexed " + entityType);

            List<LuceneWork> queue = new List<LuceneWork>();
            queue.Add(new OptimizeLuceneWork(entityType));
            WaitCallback cb = BackendQueueProcessorFactory.GetProcessor(queue);
            cb(null);
        }

        public Analyzer GetAnalyzer(String name)
        {
            Analyzer analyzer;

            if (analyzers.TryGetValue(name, out analyzer) == false)
                throw new SearchException("Unknown Analyzer definition: " + name);
            return analyzer;
        }

        public Analyzer GetAnalyzer(System.Type clazz)
        {
            if (clazz == null)
            {
                throw new ArgumentNullException("clazz", "A class has to be specified for retrieving a scoped analyzer");
            }

            DocumentBuilder builder;
            if (documentBuilders.TryGetValue(clazz, out builder)==false)
            {
                throw new ArgumentException("Entity for which to retrieve the scoped analyzer is not an [Indexed] entity: " + clazz.Name);
            }

            return builder.Analyzer;
        }	

        public void AddOptimizerStrategy(IDirectoryProvider provider, IOptimizerStrategy optimizerStrategy)
        {
            DirectoryProviderData data;
            if (dirProviderData.TryGetValue(provider, out data) == false)
            {
                dirProviderData[provider] = data = new DirectoryProviderData();
            }
            data.optimizerStrategy = optimizerStrategy;
        }

        public LuceneIndexingParameters GetIndexingParameters(IDirectoryProvider provider)
        {
            return dirProviderIndexingParams[provider];
        }

        public void AddIndexingParameters(IDirectoryProvider provider, LuceneIndexingParameters indexingParameters)
        {
            dirProviderIndexingParams[provider] = indexingParameters;
        }

        public void AddClassToDirectoryProvider(System.Type clazz, IDirectoryProvider directoryProvider)
        {
            //no need to set a read barrier, we only use this class in the init thread
            DirectoryProviderData data;
            dirProviderData.TryGetValue(directoryProvider, out data);
            if (data == null)
            {
                data = new DirectoryProviderData();
                dirProviderData.Add(directoryProvider, data);
            }
            data.classes.Add(clazz);
        }

        public void Close()
        {
            if (Interlocked.CompareExchange(ref stopped, 1, 0) != 0)
                return;

            try
            {
                worker.Close();
            }
            catch (Exception e)
            {
                log.Error("Worker raises an exception on close()", e);
            }

            try
            {
                readerProvider.Destroy();
            }
            catch (Exception e)
            {
                log.Error("ReaderProvider raises an exception on destroy()", e);
            }

            foreach (IDirectoryProvider directoryProvider in DirectoryProviders)
            {
                try
                {
                    directoryProvider.Stop();
                }
                catch (Exception e)
                {
                    log.Error("DirectoryProvider raises an exception on stop() ", e);
                }
            }
        }

        public void AddDirectoryProvider(IDirectoryProvider provider)
        {
            dirProviderData.Add(provider, new DirectoryProviderData());
        }

        #endregion

        private class DirectoryProviderData
        {
            public readonly object dirLock = new object();
            public IOptimizerStrategy optimizerStrategy;
            public readonly ISet<System.Type> classes = new HashedSet<System.Type>();
        }

    }


}