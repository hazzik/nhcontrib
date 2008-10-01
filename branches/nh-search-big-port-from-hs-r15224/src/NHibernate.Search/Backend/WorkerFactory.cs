using System;
using System.Collections;
using NHibernate.Cfg;
using NHibernate.Search.Backend.Impl;
using NHibernate.Search.Cfg;
using NHibernate.Search.Engine;
using NHibernate.Search.Impl;
using System.Collections.Generic;
using NHibernate.Util;

namespace NHibernate.Search.Backend
{
    public static class WorkerFactory
    {
        private static IDictionary<string,string> GetProperties(INHSConfiguration cfg)
        {
            IDictionary<string, string> props = cfg.Properties;
            IDictionary<string, string> workerProperties = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> entry in props)
            {
                if (!entry.Key.StartsWith(Environment.WorkerPrefix)) 
                    continue;
                workerProperties.Add(entry.Key, entry.Value);
            }
            return workerProperties;
        }

        public static IWorker CreateWorker(INHSConfiguration cfg, ISearchFactoryImplementor searchFactoryImplementor)
        {
            IDictionary<string, string> props = GetProperties(cfg);
            string impl = props[Environment.WorkerPrefix];
            IWorker worker;
            if (string.IsNullOrEmpty(impl))
            {
                worker = new TransactionalWorker();
            }
            else if ("transaction".Equals(impl,StringComparison.InvariantCultureIgnoreCase))
            {
                worker = new TransactionalWorker();
            }
            else
            {
                try
                {
                    System.Type workerClass = ReflectHelper.ClassForName(impl);
                    worker = (IWorker) Activator.CreateInstance(workerClass);
                }
                catch (Exception e)
                {
                    throw new SearchException("Unable to instanciate worker class: " + impl, e);
                }
            }
            worker.Initialize(props, searchFactoryImplementor);
            return worker;
        }
    }
}