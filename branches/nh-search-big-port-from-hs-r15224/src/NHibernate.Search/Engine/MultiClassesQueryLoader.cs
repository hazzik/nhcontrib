using System.Collections;
using System.Collections.Generic;
using Iesi.Collections.Generic;

namespace NHibernate.Search.Engine
{
    public class MultiClassesQueryLoader : ILoader
    {
        private ISession session;
        private ISearchFactoryImplementor searchFactoryImplementor;
        private List<RootEntityMetadata> entityMatadata;
        //useful if loading with a query is unsafe
        private ObjectLoader objectLoader;

        public void Init(ISession session, ISearchFactoryImplementor searchFactoryImplementor)
        {
            this.session = session;
            this.searchFactoryImplementor = searchFactoryImplementor;
            this.objectLoader = new ObjectLoader();
            this.objectLoader.Init(session, searchFactoryImplementor);
        }

        public void SetEntityTypes(System.Type[] entityTypes)
        {
            List<System.Type> safeEntityTypes;
            //TODO should we go find the root entity for a given class rather than just checking for it's root status?
            //     root entity could lead to quite inefficient queries in Hibernate when using table per class
            if (entityTypes.Length == 0)
            {
                //support all classes
                safeEntityTypes = new List<System.Type>();
                foreach (KeyValuePair<System.Type, DocumentBuilder> entry in searchFactoryImplementor.DocumentBuilders)
                {
                    if (entry.Value.IsRoot)
                        safeEntityTypes.Add(entry.Key);
                }
            }
            else
            {
                safeEntityTypes = new List<System.Type>(entityTypes);
            }
            entityMatadata = new List<RootEntityMetadata>(safeEntityTypes.Count);
            foreach (System.Type type in safeEntityTypes)
            {
                entityMatadata.Add(new RootEntityMetadata(type, searchFactoryImplementor, session));
            }
        }

        public object Load(EntityInfo entityInfo)
        {
            return ObjectLoaderHelper.Load(entityInfo, session);
        }

        public IList Load(params EntityInfo[] entityInfos)
        {
            if (entityInfos.Length == 0) return new object[0];
            if (entityInfos.Length == 1)
            {
                object entity = Load(entityInfos[0]);
                if (entity == null)
                {
                    return new object[0];
                }
                IList list = new ArrayList(1);
                list.Add(entity);
                return list;
            }

            //split EntityInfo per root entity
            Dictionary<RootEntityMetadata, List<EntityInfo>> entityinfoBuckets =
                    new Dictionary<RootEntityMetadata, List<EntityInfo>>(entityMatadata.Count);
            foreach (EntityInfo entityInfo in entityInfos)
            {
                bool found = false;
                foreach (RootEntityMetadata rootEntityInfo in entityMatadata)
                {
                    if (rootEntityInfo.mappedSubclasses.Contains(entityInfo.Clazz))
                    {
                        List<EntityInfo> bucket = entityinfoBuckets[rootEntityInfo];
                        if (bucket == null)
                        {
                            bucket = new List<EntityInfo>();
                            entityinfoBuckets.Add(rootEntityInfo, bucket);
                        }
                        bucket.Add(entityInfo);
                        found = true;
                        break; //we stop looping for the right bucket
                    }
                }
                if (!found) throw new AssertionFailure("Could not find root entity for " + entityInfo.Clazz);
            }

            //initialize objects by bucket
            foreach (KeyValuePair<RootEntityMetadata, List<EntityInfo>> entry in entityinfoBuckets)
            {
                RootEntityMetadata key = entry.Key;
                List<EntityInfo> value = entry.Value;
                EntityInfo[] bucketEntityInfos = value.ToArray();
                if (key.useObjectLoader)
                {
                    objectLoader.Load(bucketEntityInfos);
                }
                else
                {
                    ObjectLoaderHelper.InitializeObjects(bucketEntityInfos,
                            key.criteria, key.rootEntity, searchFactoryImplementor);
                }
            }
            return ObjectLoaderHelper.ReturnAlreadyLoadedObjectsInCorrectOrder(entityInfos, session);
        }

        private class RootEntityMetadata
        {
            public readonly System.Type rootEntity;
            public readonly ISet<System.Type> mappedSubclasses;
            public readonly ICriteria criteria;
            public readonly bool useObjectLoader;

            public RootEntityMetadata(System.Type rootEntity, ISearchFactoryImplementor searchFactoryImplementor, ISession session)
            {
                this.rootEntity = rootEntity;
                DocumentBuilder provider = searchFactoryImplementor.DocumentBuilders[rootEntity];
                if (provider == null) throw new AssertionFailure("Provider not found for class: " + rootEntity);
                this.mappedSubclasses = provider.MappedSubclasses;
                this.criteria = session.CreateCriteria(rootEntity);
                this.useObjectLoader = !provider.SafeFromTupleId;
            }
        }
    }
}