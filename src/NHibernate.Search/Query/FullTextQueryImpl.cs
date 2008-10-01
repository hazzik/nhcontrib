using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Iesi.Collections.Generic;
using log4net;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using NHibernate.Engine;
using NHibernate.Engine.Query;
using NHibernate.Impl;
using NHibernate.Search.Attributes;
using NHibernate.Search.Engine;
using NHibernate.Search.Filter;
using NHibernate.Search.Impl;
using NHibernate.Search.Reader;
using NHibernate.Search.Store;
using NHibernate.Search.Util;
using NHibernate.Transform;
using NHibernate.Util;

namespace NHibernate.Search.Query
{
    public class FullTextQueryImpl : AbstractQueryImpl, IFullTextQuery
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(FullTextQueryImpl));
        private readonly Lucene.Net.Search.Query luceneQuery;
        private System.Type[] classes;
        private ISet<System.Type> classesAndSubclasses;
        private int resultSize = -1;
        private Sort sort;
        private ISearchFactoryImplementor searchFactoryImplementor;
        private Lucene.Net.Search.Filter filter;
        //optimization: if we can avoid the filter clause (we can most of the time) do it as it has a significant perf impact
        private bool needClassFilterClause;
        private Dictionary<string, FullTextFilterImpl> filterDefinitions;
        private ICriteria criteria;
        private String[] indexProjection;
        private IResultTransformer resultTransformer;
        private int? firstResult;
        private int? maxResults;
        private int fetchSize;


        /// <summary>
        /// classes must be immutable
        /// </summary>
        public FullTextQueryImpl(Lucene.Net.Search.Query query, System.Type[] classes, ISession session,
                                 ParameterMetadata parameterMetadata)
            : base(query.ToString(), FlushMode.Unspecified, session.GetSessionImplementation(), parameterMetadata)
        {
            luceneQuery = query;
            this.classes = classes;
        }

        protected override IDictionary<string, LockMode> LockModes
        {
            get { throw new NotImplementedException("Full Text Query doesn't support lock modes"); }
        }

        private ISearchFactoryImplementor SearchFactory
        {
            get
            {
                if (searchFactoryImplementor == null)
                    searchFactoryImplementor = ContextHelper.GetSearchFactoryBySFI(Session);
                return searchFactoryImplementor;
            }
        }

        #region IFullTextQuery Members

        public int ResultSize
        {
            get
            {
                if (resultSize >= 0)
                {
                    return resultSize;
                }

                //get result size without object initialization
                Searcher searcher = BuildSearcher();

                if (searcher == null)
                {
                    resultSize = 0;
                }
                else
                {
                    try
                    {
                        resultSize = GetQueryAndHits(searcher).Hits.Length();
                    }
                    catch (IOException e)
                    {
                        throw new HibernateException("Unable to query Lucene index", e);
                    }
                    finally
                    {
                        CloseSearcher(searcher, searchFactoryImplementor.ReaderProvider);
                    }
                }
                return resultSize;
            }
        }

        public IFullTextQuery SetCriteriaQuery(ICriteria criteria)
        {
            this.criteria = criteria;
            return this;
        }

        public IFullTextQuery SetProjection(params String[] fields)
        {
            if (fields == null || fields.Length == 0)
            {
                this.indexProjection = null;
            }
            else
            {
                this.indexProjection = fields;
            }
            return this;
        }

        public IFullTextQuery setFirstResult(int firstResult)
        {
            if (firstResult < 0)
            {
                throw new ArgumentException("'first' pagination parameter less than 0");
            }
            this.firstResult = firstResult;
            return this;
        }

        public IFullTextQuery setMaxResults(int maxResults)
        {
            if (maxResults < 0)
            {
                throw new ArgumentException("'max' pagination parameter less than 0");
            }
            this.maxResults = maxResults;
            return this;
        }

        public IFullTextQuery SetFetchSize(int fetchSize)
        {
            base.SetFetchSize(fetchSize);
            if (fetchSize <= 0)
            {
                throw new ArgumentException("'fetch size' parameter less than or equals to 0");
            }
            this.fetchSize = fetchSize;
            return this;
        }

        public IFullTextQuery SetResultTransformer(IResultTransformer transformer)
        {
            base.SetResultTransformer(transformer);
            this.resultTransformer = transformer;
            return this;
        }

        public IFullTextFilter EnableFullTextFilter(String name)
        {
            if (filterDefinitions == null)
            {
                filterDefinitions = new Dictionary<String, FullTextFilterImpl>();
            }
            FullTextFilterImpl filterDefinition;
            if (filterDefinitions.TryGetValue(name, out filterDefinition))
                return filterDefinition;

            filterDefinition = new FullTextFilterImpl();
            filterDefinition.Name = name;
            FilterDef filterDef = searchFactoryImplementor.GetFilterDefinition(name);
            if (filterDef == null)
            {
                throw new SearchException("Unkown @FullTextFilter: " + name);
            }
            filterDefinitions.Add(name, filterDefinition);
            return filterDefinition;
        }

        public void disableFullTextFilter(String name)
        {
            filterDefinitions.Remove(name);
        }

        public override IEnumerable Enumerable()
        {
            return Enumerable<object>();
        }

        /// <summary>
        /// Return an interator on the results.
        /// Retrieve the object one by one (initialize it during the next() operation)
        /// </summary>
        public override IEnumerable<T> Enumerable<T>()
        {
            //implement an interator which keep the id/class for each hit and get the object on demand
            //cause I can't keep the searcher and hence the hit opened. I dont have any hook to know when the
            //user stop using it
            //scrollable is better in this area

            //NH TODO: we do know we stop using it, because we have IEnumerable.Dispose

            //find the directories
            Searcher searcher = BuildSearcher();
            if (searcher == null)
                return new IteratorImpl<T>(new List<EntityInfo>(), new NoLoader());
            try
            {
                QueryAndHits queryAndHits = GetQueryAndHits(searcher);
                int first = First();
                int max = Max(first, queryAndHits.Hits);
                ISession sess = (ISession)Session;

                int size = max - first + 1 < 0 ? 0 : max - first + 1;
                List<Engine.EntityInfo> infos = new List<Engine.EntityInfo>(size);
                DocumentExtractor extractor = new DocumentExtractor(queryAndHits.PreparedQuery, searcher, searchFactoryImplementor, indexProjection);
                for (int index = first; index <= max; index++)
                {
                    //TODO use indexSearcher.getIndexReader().document( hits.id(index), FieldSelector(indexProjection) );
                    Engine.EntityInfo extract = extractor.Extract(queryAndHits.Hits, index);
                    infos.Add(extract);
                }
                ILoader loader = GetLoader(sess);
                return new IteratorImpl<T>(infos, loader);
            }
            catch (IOException e)
            {
                throw new HibernateException("Unable to query Lucene index", e);
            }
            finally
            {
                CloseSearcher(searcher);
            }
        }

        private ILoader GetLoader(ISession session)
        {
            if (indexProjection != null)
            {
                ProjectionLoader loader = new ProjectionLoader();
                loader.Init(session, searchFactoryImplementor, resultTransformer, indexProjection);
                return loader;
            }
            if (criteria != null)
            {
                if (classes.Length > 1) throw new SearchException("Cannot mix criteria and multiple entity types");
                if (criteria is CriteriaImpl)
                {
                    String targetEntity = ((CriteriaImpl)criteria).EntityOrClassName;
                    if (classes.Length == 1 && !classes[0].Name.Equals(targetEntity))
                    {
                        throw new SearchException("Criteria query entity should match query entity");
                    }
                    try
                    {
                        System.Type entityType = ReflectHelper.ClassForName(targetEntity);
                        classes = new System.Type[] { entityType };
                    }
                    catch (Exception e)
                    {
                        throw new SearchException("Unable to load entity class from criteria: " + targetEntity, e);
                    }
                }
                QueryLoader loader = new QueryLoader();
                loader.Init(session, searchFactoryImplementor);
                loader.SetEntityType(classes[0]);
                loader.SetCriteria(criteria);
                return loader;
            }
            if (classes.Length == 1)
            {
                QueryLoader loader = new QueryLoader();
                loader.Init(session, searchFactoryImplementor);
                loader.SetEntityType(classes[0]);
                return loader;
            }
            else
            {
                MultiClassesQueryLoader loader = new MultiClassesQueryLoader();
                loader.Init(session, searchFactoryImplementor);
                loader.SetEntityTypes(classes);
                return loader;
            }
        }

        public override IList<T> List<T>()
        {
            ArrayList arrayList = new ArrayList();
            List(arrayList);
            return (T[])arrayList.ToArray(typeof(T));
        }

        public override IList List()
        {
            ArrayList arrayList = new ArrayList();
            List(arrayList);
            return arrayList;
        }

        public override void List(IList list)
        {
            //find the directories
            Searcher searcher = BuildSearcher();
            if (searcher == null)
                return;
            try
            {
                QueryAndHits queryAndHits = GetQueryAndHits(searcher);
                int first = First();
                int max = Max(first, queryAndHits.Hits);
                ISession sess = (ISession)this.Session;

                int size = max - first + 1 < 0 ? 0 : max - first + 1;
                List<EntityInfo> infos = new List<EntityInfo>(size);
                DocumentExtractor extractor = new DocumentExtractor(queryAndHits.PreparedQuery, searcher,
                                                                    searchFactoryImplementor, indexProjection);
                for (int index = first; index <= max; index++)
                {
                    infos.Add(extractor.Extract(queryAndHits.Hits, index));
                }
                ILoader loader = GetLoader(sess);
                IList tempList = loader.Load(infos.ToArray());
                //stay consistent with transformTuple which can only be executed during a projection
                if (resultTransformer != null && !(loader is ProjectionLoader))
                {
                    tempList = resultTransformer.TransformList(tempList);
                }
                foreach (object o in tempList)
                {
                    list.Add(o);
                }
            }
            catch (IOException e)
            {
                throw new HibernateException("Unable to query Lucene index", e);
            }
            finally
            {
                CloseSearcher(searcher);
            }
        }

        public Explanation Explain(int documentId)
        {
            Explanation explanation;
            Searcher searcher = BuildSearcher();
            if (searcher == null)
            {
                throw new SearchException("Unable to build explanation for document id:"
                        + documentId + ". no index found");
            }
            try
            {
                Lucene.Net.Search.Query query = FilterQueryByClasses(luceneQuery);
                BuildFilters();
                explanation = searcher.Explain(query, documentId);
            }
            catch (IOException e)
            {
                throw new HibernateException("Unable to query Lucene index and build explanation", e);
            }
            finally
            {
                CloseSearcher(searcher, searchFactoryImplementor.ReaderProvider);
            }
            return explanation;
        }

        private void CloseSearcher(Searchable searcher, IReaderProvider provider)
        {
            ISet<IndexReader> readers = ReaderProviderHelper.GetIndexReaders(searcher);
            foreach (IndexReader reader in readers)
            {
                try
                {
                    provider.CloseReader(reader);
                }
                catch (Exception e)
                {
                    log.Warn("Unable to properly close searcher during lucene query: " + QueryString, e);
                }
            }
        }

        public override IQuery SetLockMode(string alias, LockMode lockMode)
        {
            throw new NotImplementedException("Full Text Query doesn't support lock modes");
        }

        public IFullTextQuery SetSort(Sort sort)
        {
            this.sort = sort;
            return this;
        }

        public override int ExecuteUpdate()
        {
            // TODO: Implement FullTextQueryImpl.ExecuteUpdate()
            throw new NotImplementedException("Implement FullTextQueryImpl.ExecuteUpdate()");
        }

        #endregion

        private void CloseSearcher(Searchable searcher)
        {
            if (searcher == null)
                return;

            try
            {
                searcher.Close();
            }
            catch (IOException e)
            {
                log.Warn("Unable to properly close searcher during lucene query: " + QueryString, e);
            }
        }

        private Searcher BuildSearcher()
        {
            IDictionary<System.Type, DocumentBuilder> builders = searchFactoryImplementor.DocumentBuilders;
            List<IDirectoryProvider> directories = new List<IDirectoryProvider>();

            Similarity searcherSimilarity = null;
            //TODO check if caching this work for the last n list of classes makes a perf boost
            if (classes == null || classes.Length == 0)
            {
                //no class means all classes
                foreach (DocumentBuilder builder in builders.Values)
                {
                    searcherSimilarity = CheckSimilarity(searcherSimilarity, builder);
                    IDirectoryProvider[] directoryProviders =
                        builder.DirectoryProvidersSelectionStrategy.GetDirectoryProvidersForAllShards();
                    PopulateDirectories(directories, directoryProviders);

                }
                classesAndSubclasses = null;
            }
            else
            {
                ISet<System.Type> involvedClasses = new HashedSet<System.Type>();
                involvedClasses.AddAll(classes);
                foreach (System.Type type in classes)
                {
                    DocumentBuilder builder = builders[type];
                    if (builder != null)
                        involvedClasses.AddAll(builder.MappedSubclasses);
                }

                foreach (System.Type clazz in involvedClasses)
                {
                    DocumentBuilder builder = builders[clazz];
                    //TODO should we rather choose a polymorphic path and allow non mapped entities
                    if (builder == null)
                        throw new HibernateException("Not a mapped entity (don't forget to add @Indexed): " + clazz);

                    IDirectoryProvider[] directoryProviders = builder.DirectoryProvidersSelectionStrategy.GetDirectoryProvidersForAllShards();
                    searcherSimilarity = CheckSimilarity(searcherSimilarity, builder);
                    PopulateDirectories(directories, directoryProviders);
                }
                classesAndSubclasses = involvedClasses;
            }

            //compute optimization needClassFilterClause
            //if at least one DP contains one class that is not part of the targeted classesAndSubclasses we can't optimize
            if (classesAndSubclasses != null)
            {
                foreach (IDirectoryProvider dp in directories)
                {
                    ISet<System.Type> classesInDirectoryProvider =
                        searchFactoryImplementor.GetClassesInDirectoryProvider(dp);
                    // if a DP contains only one class, we know for sure it's part of classesAndSubclasses
                    if (classesInDirectoryProvider.Count > 1)
                    {
                        //risk of needClassFilterClause
                        foreach (System.Type type in classesInDirectoryProvider)
                        {
                            if (!classesAndSubclasses.Contains(type))
                            {
                                needClassFilterClause = true;
                                break;
                            }
                        }
                    }
                    if (needClassFilterClause)
                        break;
                }
            }

            //set up the searcher
            IndexSearcher @is = new IndexSearcher(searchFactoryImplementor.ReaderProvider.OpenReader(directories.ToArray()));
            @is.SetSimilarity(searcherSimilarity);
            return @is;
        }

        private static Similarity CheckSimilarity(Similarity similarity, DocumentBuilder builder)
        {
            if (similarity == null)
            {
                similarity = builder.Similarity;
            }
            else if (!similarity.GetType().Equals(builder.Similarity.GetType()))
            {
                throw new HibernateException(
                    "Cannot perform search on two entities with differing Similarity implementations (" +
                    similarity.GetType().Name + " && " + builder.Similarity.GetType().Name + ")");
            }

            return similarity;
        }

        private static void PopulateDirectories(IList<IDirectoryProvider> directories, IDirectoryProvider[] directoryProviders)
        {
            foreach (IDirectoryProvider provider in directoryProviders)
            {
                if (!directories.Contains(provider))
                {
                    directories.Add(provider);
                }
            }
        }

        /// <summary>
        /// Execute the lucene search and return the machting hits.
        /// </summary>
        /// <param name="searcher"></param>
        /// <returns></returns>
        private QueryAndHits GetQueryAndHits(Searcher searcher)
        {
            Hits hits;
            Lucene.Net.Search.Query query = FilterQueryByClasses(luceneQuery);
            BuildFilters();
            hits = searcher.Search(query, filter, sort);
            SetResultSize(hits);
            return new QueryAndHits(query, hits);
        }

        private void BuildFilters()
        {
            if (filterDefinitions == null || filterDefinitions.Count == 0)
            {
                return; // there is nothing to do if we don't have any filter definitions
            }

            ChainedFilter chainedFilter = new ChainedFilter();

            foreach (KeyValuePair<string, FullTextFilterImpl> pair in filterDefinitions)
            {
                Lucene.Net.Search.Filter filter = BuildLuceneFilter(pair.Value);
                chainedFilter.AddFilter(filter);
            }
            if (filter != null)
                chainedFilter.AddFilter(filter);
            filter = chainedFilter;
        }

        /// <summary>
        /// Builds a Lucene filter using the given <code>FullTextFilter</code>.
        /// </summary>
        /// <param name="fullTextFilter"></param>
        /// <returns></returns>
        private Lucene.Net.Search.Filter BuildLuceneFilter(FullTextFilterImpl fullTextFilter)
        {

            /*
             * FilterKey implementations and Filter(Factory) do not have to be threadsafe wrt their parameter injection
             * as FilterCachingStrategy ensure a memory barrier between concurrent thread calls
             */
            FilterDef def = searchFactoryImplementor.GetFilterDefinition(fullTextFilter.Name);
            Object instance = CreateFilterInstance(fullTextFilter, def);
            FilterKey key = CreateFilterKey(def, instance);

            // try to get the filter out of the cache
            Lucene.Net.Search.Filter filter = CacheInstance(def.CacheMode) ?
                    searchFactoryImplementor.FilterCachingStrategy.GetCachedFilter(key) :
                    null;

            if (filter == null)
            {
                filter = CreateFilter(def, instance);

                // add filter to cache if we have to
                if (CacheInstance(def.CacheMode))
                {
                    searchFactoryImplementor.FilterCachingStrategy.AddCachedFilter(key, filter);
                }
            }
            return filter;
        }

        private Lucene.Net.Search.Filter CreateFilter(FilterDef def, object instance)
        {
            Lucene.Net.Search.Filter filter = null;
            if (def.FactoryMethod != null)
            {
                try
                {
                    filter = (Lucene.Net.Search.Filter)def.FactoryMethod.Invoke(instance, new object[0]);
                }
                catch (InvalidCastException e)
                {
                    throw new SearchException("@Key method does not return a org.apache.lucene.search.Filter class: "
                            + def.Impl.Name + "." + def.FactoryMethod.Name);
                }
                catch (Exception e)
                {
                    throw new SearchException("Unable to access @Factory method: "
                            + def.Impl.Name + "." + def.FactoryMethod.Name);
                }
            }
            else
            {
                try
                {
                    filter = (Lucene.Net.Search.Filter)instance;
                }
                catch (InvalidCastException e)
                {
                    throw new SearchException("Filter implementation does not implement Lucene.Net.Search.Filter: "
                            + def.Impl.Name + ". "
                            + (def.FactoryMethod != null ? def.FactoryMethod.Name : ""), e);
                }
            }

            filter = AddCachingWrapperFilter(filter, def);
            return filter;
        }

        /// <summary>
        /// Decides whether to wrap the given filter around a <code>CachingWrapperFilter<code>.
        /// </summary>
        private Lucene.Net.Search.Filter AddCachingWrapperFilter(Lucene.Net.Search.Filter filter, FilterDef def)
        {
            if (CacheResults(def.CacheMode))
            {
                int cachingWrapperFilterSize = searchFactoryImplementor.FilterCacheBitResultsSize;
                filter = new NHibernate.Search.Filter.CachingWrapperFilter(filter, cachingWrapperFilterSize);
            }

            return filter;
        }

        private FilterKey CreateFilterKey(FilterDef def, object instance)
        {
            FilterKey key = null;
            if (!CacheInstance(def.CacheMode))
            {
                return key; // if the filter is not cached there is no key!
            }

            if (def.KeyMethod == null)
            {
                key = new DefaultFilterKey();
            }
            else
            {
                try
                {
                    key = (FilterKey)def.KeyMethod.Invoke(instance, new object[0]);
                }
                catch (InvalidCastException e)
                {
                    throw new SearchException("@Key method does not return FilterKey: "
                                              + def.Impl.Name + "." + def.KeyMethod.Name);
                }
                catch (Exception e)
                {
                    throw new SearchException("Unable to access @Key method: "
                                              + def.Impl.Name + "." + def.KeyMethod.Name);
                }
            }

            key.Impl = def.Impl;

            //Make sure Filters are isolated by filter def name
            StandardFilterKey wrapperKey = new StandardFilterKey();
            wrapperKey.AddParameter(def.Name);
            wrapperKey.AddParameter(key);
            return wrapperKey;
        }

        private static Object CreateFilterInstance(FullTextFilterImpl fullTextFilter, FilterDef def)
        {
            Object instance;
            try
            {
                instance = Activator.CreateInstance(def.Impl);
            }
            catch (Exception e)
            {
                throw new SearchException("Unable to create @FullTextFilterDef: " + def.Impl, e);
            }

            foreach (KeyValuePair<string, object> entry in fullTextFilter.Parameters)
            {
                def.Invoke(entry.Key, instance, entry.Value);
            }
            if (CacheInstance(def.CacheMode) && def.KeyMethod == null && fullTextFilter.Parameters.Count > 0)
            {
                throw new SearchException("Filter with parameters and no @Key method: " + fullTextFilter.Name);
            }
            return instance;
        }

        private static bool CacheInstance(FilterCacheModeType mode)
        {
            switch (mode)
            {
                case FilterCacheModeType.None:
                    return false;
                case FilterCacheModeType.InstanceAndBitResults:
                    return true;
                case FilterCacheModeType.InstanceOnly:
                    return true;
                default:
                    throw new NotSupportedException("Uknown FilterCacheModeType value " + mode);
            }
        }

        private static bool CacheResults(FilterCacheModeType mode)
        {
            switch (mode)
            {
                case FilterCacheModeType.None:
                    return false;
                case FilterCacheModeType.InstanceAndBitResults:
                    return true;
                case FilterCacheModeType.InstanceOnly:
                    return false;
                default:
                    throw new NotSupportedException("Uknown FilterCacheModeType value " + mode);
            }
        }

        private Lucene.Net.Search.Query FilterQueryByClasses(Lucene.Net.Search.Query luceneQuery)
        {
            if (!needClassFilterClause)
            {
                return luceneQuery;
            }
            //A query filter is more practical than a manual class filtering post query (esp on scrollable resultsets)
            //it also probably minimise the memory footprint	
            BooleanQuery classFilter = new BooleanQuery();
            //annihilate the scoring impact of DocumentBuilder.CLASS_FIELDNAME
            classFilter.SetBoost(0);
            foreach (System.Type type in classesAndSubclasses)
            {
                Term t = new Term(DocumentBuilder.CLASS_FIELDNAME, type.Name);
                TermQuery termQuery = new TermQuery(t);
                classFilter.Add(termQuery, BooleanClause.Occur.SHOULD);
            }

            BooleanQuery filteredQuery = new BooleanQuery();
            filteredQuery.Add(luceneQuery, BooleanClause.Occur.MUST);
            filteredQuery.Add(classFilter, BooleanClause.Occur.MUST);
            return filteredQuery;
        }

        private int Max(int first, Hits hits)
        {
            if (maxResults == null) 
                return hits.Length() - 1;

            if (maxResults + first < hits.Length()) 
                return first + maxResults.Value - 1;

            return hits.Length() - 1;
        }

        private int First()
        {
            return firstResult ?? 0;
        }

        private void SetResultSize(Hits hits)
        {
            resultSize = hits.Length();
        }

        public IFullTextQuery SetFilter(Lucene.Net.Search.Filter filter)
        {
            this.filter = filter;
            return this;
        }

        private class QueryAndHits
        {
            public QueryAndHits(Lucene.Net.Search.Query preparedQuery, Hits hits)
            {
                this.PreparedQuery = preparedQuery;
                this.Hits = hits;
            }

            public readonly Lucene.Net.Search.Query PreparedQuery;
            public readonly Hits Hits;
        }

        private class DefaultFilterKey : FilterKey
        {
            public override int GetHashCode()
            {
                return Impl.GetHashCode();
            }

            public override bool Equals(Object obj)
            {
                if (!(obj is FilterKey)) return false;
                FilterKey that = (FilterKey)obj;
                return this.Impl.Equals(that.Impl);
            }
        }
    }
}