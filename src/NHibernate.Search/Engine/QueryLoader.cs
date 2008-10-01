using System;
using System.Collections;
using log4net;
using NHibernate.Criterion;

namespace NHibernate.Search.Engine
{
    public class QueryLoader : ILoader
    {

        private ISession session;
        private System.Type entityType;
        private ISearchFactoryImplementor searchFactoryImplementor;
        private ICriteria criteria;
        private bool isExplicitCriteria;

        public void Init(ISession session, ISearchFactoryImplementor searchFactoryImplementor)
        {
            this.session = session;
            this.searchFactoryImplementor = searchFactoryImplementor;
        }

        public void SetEntityType(System.Type entityType)
        {
            this.entityType = entityType;
        }

        public Object Load(EntityInfo entityInfo)
        {
            //if explicit criteria, make sure to use it to load the objects
            if (isExplicitCriteria) 
                Load(new EntityInfo[] { entityInfo });
            return ObjectLoaderHelper.Load(entityInfo, session);
        }

        public IList Load(params EntityInfo[] entityInfos)
        {
            if (entityInfos.Length == 0) return new object[0];
            if (entityType == null) throw new AssertionFailure("EntityType not defined");
            if (criteria == null) criteria = session.CreateCriteria(entityType);

            ObjectLoaderHelper.InitializeObjects(entityInfos, criteria, entityType, searchFactoryImplementor);
            return ObjectLoaderHelper.ReturnAlreadyLoadedObjectsInCorrectOrder(entityInfos, session);
        }

        public void SetCriteria(ICriteria criteria)
        {
            isExplicitCriteria = criteria != null;
            this.criteria = criteria;
        }
    }
}