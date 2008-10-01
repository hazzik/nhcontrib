using System;
using System.Collections;
using log4net;

namespace NHibernate.Search.Engine
{
    public class ObjectLoader : ILoader
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ObjectLoader));

        private ISession session;

        #region ILoader Members

        public void Init(ISession session, ISearchFactoryImplementor searchFactoryImplementor)
        {
            this.session = session;
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
                Object entity = Load(entityInfos[0]);
                if (entity == null)
                {
                    return new object[0];
                }
                IList list = new ArrayList(1);
                list.Add(entity);
                return list;
            }

            // Use load to benefit from the batch-size
            // We don't face proxy casting issues since the exact class is extracted from the index
            foreach (EntityInfo entityInfo in entityInfos)
            {
                session.Load(entityInfo.Clazz, entityInfo.Id);
            }

            ArrayList result = new ArrayList(entityInfos.Length);

            foreach (EntityInfo entityInfo in entityInfos)
            {
                try
                {
                    object entity = session.Load(entityInfo.Clazz, entityInfo.Id);
                    NHibernateUtil.Initialize(entity);
                    result.Add(entity);
                }
                catch (Exception e)
                {
                    if (LoaderHelper.IsObjectNotFoundException(e))
                    {
                        log.Debug("Object found in Search index but not in database: "
                                  + entityInfo.Clazz + " with id " + entityInfo.Id);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return result;
        }

        #endregion
    }
}