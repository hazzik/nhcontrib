using System;
using System.Collections;
using System.Collections.Generic;
using log4net;
using NHibernate.Event;

namespace NHibernate.Search.Event
{
    /// <summary>
    /// Helper methods initializing Hibernate Search event listeners.
    /// </summary>
    public class EventListenerRegister {

        /// <summary>
        ///  Add the FullTextIndexEventListener to all listeners, if enabled in configuration
        ///  and if not already registered.
        /// </summary>
        /// <param name="listeners"></param>
        /// <param name="properties"></param>
        public static void EnableHibernateSearch(EventListeners listeners, IDictionary properties) {		
            // check whether search is explicitly disabled - if so there is nothing to do	
            String enableSearchListeners = (string)properties[Environment.AutoRegisterListeners];
            if ( "false".Equals( enableSearchListeners,StringComparison.InvariantCultureIgnoreCase)) {
                LogManager.GetLogger( typeof(EventListenerRegister)).Info(
                    "Property hibernate.search.autoregister_listeners is set to false." +
                    " No attempt will be made to register Hibernate Search event listeners." );
                return;
            }
            FullTextIndexEventListener searchListener = new FullTextIndexEventListener();
            // PostInsertEventListener
            listeners.PostInsertEventListeners = (
                                                     AddIfNeeded(
                                                         listeners.PostInsertEventListeners,
                                                         searchListener,
                                                         new IPostInsertEventListener[] { searchListener } )
                                                 );
            // PostUpdateEventListener
            listeners.PostUpdateEventListeners = (
                                                     AddIfNeeded(
                                                         listeners.PostUpdateEventListeners,
                                                         searchListener,
                                                         new IPostUpdateEventListener[] { searchListener } )
                                                 );
            // PostDeleteEventListener
            listeners.PostDeleteEventListeners = (
                                                     AddIfNeeded(
                                                         listeners.PostDeleteEventListeners,
                                                         searchListener,
                                                         new IPostDeleteEventListener[] { searchListener } )
                                                 );
		
            // PostCollectionRecreateEventListener
            listeners.PostCollectionRecreateEventListeners = (
                AddIfNeeded(
                    listeners.PostCollectionRecreateEventListeners,
                    searchListener,
                    new IPostCollectionRecreateEventListener[] { searchListener } )
                );
             //PostCollectionRemoveEventListener
            listeners.PostCollectionRemoveEventListeners = (
                AddIfNeeded(
                    listeners.PostCollectionRemoveEventListeners,
                    searchListener,
                    new IPostCollectionRemoveEventListener[] { searchListener } )
                );
             //PostCollectionUpdateEventListener
            listeners.PostCollectionUpdateEventListeners = (
                AddIfNeeded(
                    listeners.PostCollectionUpdateEventListeners,
                    searchListener,
                    new IPostCollectionUpdateEventListener[] { searchListener } )
                );
		
        }

        /// <summary>
        /// Verifies if a Search listener is already present; if not it will return
        /// a grown address adding the listener to it.
        /// </summary>
        private static T[] AddIfNeeded<T, TImpl>(T[] listeners, TImpl searchEventListener, T[] toUseOnNull)
            where TImpl : T
        {
            if ( listeners == null ) {
                return toUseOnNull;
            }
            if ( ! IsPresentInListeners( listeners ) ) {
                List<T> ts = new List<T>(listeners);
                ts.Add(searchEventListener);
                return ts.ToArray();
            }
            return listeners;
        }
	
        /// <summary>
        /// Verifies if a FullTextIndexEventListener is contained in the array.
        /// </summary>
        /// <param name="listeners"></param>
        /// <returns></returns>
        private static bool IsPresentInListeners<T>(IEnumerable<T> listeners)
        {
            foreach (IPostInsertEventListener eventListener in listeners)
            {
                if ( eventListener is FullTextIndexEventListener) {
                    return true;
                }
                if ( eventListener is FullTextIndexCollectionEventListener) {
                    return true;
                }
            }
            return false;
        }
		
    }
}