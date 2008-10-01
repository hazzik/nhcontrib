using System;
using log4net;
using NHibernate.Cfg;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Search.Backend;
using NHibernate.Search.Backend.Impl;
using NHibernate.Search.Engine;
using NHibernate.Search.Impl;

namespace NHibernate.Search.Event
{
    /// <summary>
    /// This listener supports setting a parent directory for all generated index files.
    ///  It also supports setting the analyzer class to be used.
    /// </summary>
    public class FullTextIndexEventListener : IPostDeleteEventListener,
                                              IPostInsertEventListener,
                                              IPostUpdateEventListener,
                                              IPostCollectionRecreateEventListener,
                                              IPostCollectionRemoveEventListener,
                                              IPostCollectionUpdateEventListener,
                                              IInitializable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(FullTextIndexEventListener));
        protected bool used;
        protected ISearchFactoryImplementor searchFactoryImplementor;

        #region Property methods

        public ISearchFactoryImplementor SearchFactory
        {
            get { return searchFactoryImplementor; }
        }

        #endregion

        #region Private methods

        private bool EntityIsIndexed(object entity)
        {
            DocumentBuilder builder;
            searchFactoryImplementor.DocumentBuilders.TryGetValue(entity.GetType(), out builder);
            return builder != null;
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Does the work, after checking that the entity type is indeed indexed.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="id"></param>
        /// <param name="workType"></param>
        /// <param name="e"></param>
        protected void ProcessWork(Object entity, object id, WorkType workType, AbstractEvent e)
        {
            if (!EntityIsIndexed(entity))
                return;
            Work work = new Work(entity, id, workType);
            searchFactoryImplementor.Worker.PerformWork(work, new EventSourceTransactionContext(e.Session));
        }

        #endregion

        public void OnPostRecreateCollection(PostCollectionRecreateEvent @event)
        {
            ProcessCollectionEvent(@event);
        }

        public void OnPostRemoveCollection(PostCollectionRemoveEvent @event)
        {
            ProcessCollectionEvent(@event);
        }

        public void OnPostUpdateCollection(PostCollectionUpdateEvent @event)
        {
            ProcessCollectionEvent(@event);
        }

        protected void ProcessCollectionEvent(AbstractCollectionEvent @event)
        {
            Object entity = @event.AffectedOwnerOrNull;
            if (entity == null)
            {
                //Hibernate cannot determine every single time the owner especially in case detached objects are involved
                // or property-ref is used
                //Should log really but we don't know if we're interested in this collection for indexing
                return;
            }
            if (used == false || EntityIsIndexed(entity) == false)
                return;

            object id = GetId(entity, @event);
            if (id == null)
            {
                log.WarnFormat(
                    "Unable to reindex entity on collection change, id cannot be extracted: {0}",
                    @event.GetAffectedOwnerEntityName()
                    );
                return;
            }
            ProcessWork(entity, id, WorkType.Collection, @event);
        }

        private static object GetId(Object entity, AbstractCollectionEvent @event)
        {
            object id = @event.AffectedOwnerIdOrNull;
            if (id == null)
            {
                //most likely this recovery is unnecessary since NHibernate Core probably try that
                EntityEntry entityEntry = @event.Session.PersistenceContext.GetEntry(entity);
                id = entityEntry == null ? null : entityEntry.Id;
            }
            return id;
        }

        #region Public methods

        public void Initialize(Configuration cfg)
        {
            searchFactoryImplementor = SearchFactoryImpl.GetSearchFactory(cfg);

            String indexingStrategy = cfg.GetProperty(Environment.IndexingStrategy) ?? "event";
            if ("event".Equals(indexingStrategy))
                used = searchFactoryImplementor.DocumentBuilders.Count != 0;
            else if ("manual".Equals(indexingStrategy))
                used = false;
            else
                throw new SearchException(Environment.IndexBase + " unknown: " + indexingStrategy);
        }

        public void OnPostDelete(PostDeleteEvent e)
        {
            if (used == false)
                return;
            ProcessWork(e.Entity, e.Id, WorkType.Delete, e);
        }

        public void OnPostInsert(PostInsertEvent e)
        {
            if (used == false)
                return;
            ProcessWork(e.Entity, e.Id, WorkType.Add, e);
        }

        public void OnPostUpdate(PostUpdateEvent e)
        {
            if (used == false)
                return;
            ProcessWork(e.Entity, e.Id, WorkType.Update, e);
        }

        #endregion
    }
}