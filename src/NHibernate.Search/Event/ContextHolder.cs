using System;
using NHibernate.Cfg;
using NHibernate.Search.Impl;
using NHibernate.Util;

namespace NHibernate.Search.Event
{
    public class ContextHolder
    {
        //of <Configuration, SearchFactoryImpl>
        [ThreadStatic]
        private static WeakHashtable contextMap = new WeakHashtable();

        //code doesn't have to be multithreaded because SF creation is not.
        //this is not a public API, should really only be used during the SessionFActory building
        public static SearchFactoryImpl GetOrBuildSearchFactory(Configuration cfg)
        {
            if (contextMap == null)
                contextMap = new WeakHashtable();

            SearchFactoryImpl searchFactory = (SearchFactoryImpl)contextMap[cfg];
            if (searchFactory == null)
            {
                //TODO: Need to figure out how to work with the current cfg.
                searchFactory = null; //new SearchFactoryImpl( new SearchConfigurationFromHibernateCore( cfg ) );
                contextMap.Add(cfg, searchFactory);
            }
            return searchFactory;
        }
    }
}