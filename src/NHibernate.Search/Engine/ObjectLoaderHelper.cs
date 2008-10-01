
using System;
using System.Collections;
using System.Collections.Generic;
using log4net;
using NHibernate.Criterion;

namespace NHibernate.Search.Engine
{
    public class ObjectLoaderHelper
    {
        private const int MAX_IN_CLAUSE = 500;
        private static readonly ILog log = LogManager.GetLogger(typeof(ObjectLoader));

        public static Object Load(EntityInfo entityInfo, ISession session)
        {
            //be sure to get an initialized object but save from ONFE and ENFE
            Object maybeProxy = session.Load(entityInfo.Clazz, entityInfo.Id);
            try
            {
                NHibernateUtil.Initialize(maybeProxy);
            }
            catch (Exception e)
            {
                if (LoaderHelper.IsObjectNotFoundException(e))
                {
                    log.DebugFormat("Object found in Search index but not in database: {0} with id {1}",
                            entityInfo.Clazz, entityInfo.Id);
                    maybeProxy = null;
                }
                else
                {
                    throw;
                }
            }
            return maybeProxy;
        }

        public static void InitializeObjects(EntityInfo[] entityInfos, ICriteria criteria, System.Type entityType,
                                             ISearchFactoryImplementor searchFactoryImplementor)
        {
            int maxResults = entityInfos.Length;
            if (maxResults == 0) return;

            DocumentBuilder builder = searchFactoryImplementor.DocumentBuilders[entityType];
            String idName = builder.IdentifierName;
            int loop = maxResults / MAX_IN_CLAUSE;
            bool exact = maxResults % MAX_IN_CLAUSE == 0;
            if (!exact) loop++;
            Disjunction disjunction = Restrictions.Disjunction();
            for (int index = 0; index < loop; index++)
            {
                int max = index * MAX_IN_CLAUSE + MAX_IN_CLAUSE <= maxResults ?
                        index * MAX_IN_CLAUSE + MAX_IN_CLAUSE :
                        maxResults;
                List<object> ids = new List<object>(max - index * MAX_IN_CLAUSE);
                for (int entityInfoIndex = index * MAX_IN_CLAUSE; entityInfoIndex < max; entityInfoIndex++)
                {
                    ids.Add(entityInfos[entityInfoIndex].Id);
                }
                disjunction.Add(Restrictions.In(idName, ids));
            }
            criteria.Add(disjunction);
            criteria.List(); //load all objects
        }


        public static IList ReturnAlreadyLoadedObjectsInCorrectOrder(EntityInfo[] entityInfos, ISession session)
        {
            //mandatory to keep the same ordering
            IList result = new ArrayList(entityInfos.Length);
            foreach (EntityInfo entityInfo in entityInfos)
            {
                Object element = session.Load(entityInfo.Clazz, entityInfo.Id);
                if (NHibernateUtil.IsInitialized(element))
                {
                    //all existing elements should have been loaded by the query,
                    //the other ones are missing ones
                    result.Add(element);
                }
                else if (log.IsDebugEnabled)
                {
                    log.DebugFormat("Object found in Search index but not in database: {0} with {1}",
                        entityInfo.Clazz, entityInfo.Id);
                }
            }
            return result;
        }
    }

}