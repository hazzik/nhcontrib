﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate.Engine;
using NHibernate.Envers.Configuration;
using NHibernate.Envers.Exceptions;
using NHibernate.Envers.Query;
using NHibernate.Envers.Synchronization;
using NHibernate.Envers.Tools;
using NHibernate.Event;

namespace NHibernate.Envers.Reader
{
    /**
     * @author Simon Duduica, port of Envers omonyme class by Adam Warski (adam at warski dot org)
     */
    public class AuditReader : IAuditReaderImplementor {
        private readonly AuditConfiguration verCfg;
        public ISessionImplementor SessionImplementor{ get; private set;}
        public ISession Session{ get; private set;}
        public IFirstLevelCache FirstLevelCache{ get; private set;}

        public AuditReader(AuditConfiguration verCfg, ISession session,
                                  ISessionImplementor sessionImplementor) {
            this.verCfg = verCfg;
            this.SessionImplementor = sessionImplementor;
            this.Session = session;

            FirstLevelCache = new FirstLevelCache();
        }

        private void CheckSession() {
            if (!Session.IsOpen) {
                throw new Exception("The associated entity manager is closed!");
            }
        }

        public T Find<T> (System.Type cls, Object primaryKey, long revision){
            ArgumentsTools.CheckNotNull(cls, "Entity class");
            ArgumentsTools.CheckNotNull(primaryKey, "Primary key");
            ArgumentsTools.CheckNotNull(revision, "Entity revision");
            ArgumentsTools.CheckPositive(revision, "Entity revision");
            CheckSession();

            String entityName = cls.FullName;

            if (!verCfg.EntCfg.IsVersioned(entityName)) {
                throw new NotAuditedException(entityName, entityName + " is not versioned!");
            }

            if (FirstLevelCache.Contains(entityName, revision, primaryKey)) {
                return (T) FirstLevelCache[entityName, revision, primaryKey];
            }

            Object result;
            try {
                // The result is put into the cache by the entity instantiator called from the query
                result = CreateQuery().ForEntitiesAtRevision(cls, revision)
                    .Add(AuditEntity.Id().Eq(primaryKey)).GetSingleResult();
            } catch (NonUniqueResultException e) {
                throw new AuditException(e);
            }
            catch (HibernateException e)
            {//ORIG: NoResultException e
                result = null;
            }

            return (T) result;
        }

        public IList GetRevisions(System.Type cls, Object primaryKey)
        {
            // todo: if a class is not versioned from the beginning, there's a missing ADD rev - what then?
            ArgumentsTools.CheckNotNull(cls, "Entity class");
            ArgumentsTools.CheckNotNull(primaryKey, "Primary key");
            CheckSession();

            String entityName = cls.FullName;

            if (!verCfg.EntCfg.IsVersioned(entityName)) {
                throw new NotAuditedException(entityName, entityName + " is not versioned!");
            }

            return CreateQuery().ForRevisionsOfEntity(cls, false, true)
                    .AddProjection(AuditEntity.RevisionNumber())
                    .Add(AuditEntity.Id().Eq(primaryKey))
                    .GetResultList();
        }

        public DateTime GetRevisionDate(long revision){
            ArgumentsTools.CheckNotNull(revision, "Entity revision");
            ArgumentsTools.CheckPositive(revision, "Entity revision");
            CheckSession();

            IQuery query = verCfg.RevisionInfoQueryCreator.getRevisionDateQuery(Session, revision);

            try {
                Object timestampObject = query.UniqueResult();
                if (timestampObject == null) {
                    throw new RevisionDoesNotExistException(revision);
                }

                // The timestamp object is either a date or a long
                return timestampObject is DateTime ? (DateTime) timestampObject : new DateTime((long) timestampObject);
            } catch (NonUniqueResultException e) {
                throw new AuditException(e);
            }
        }

        public long GetRevisionNumberForDate(DateTime date) {
            ArgumentsTools.CheckNotNull(date, "Date of revision");
            CheckSession();

            IQuery query = verCfg.RevisionInfoQueryCreator.getRevisionNumberForDateQuery(Session, date);

            try {
                object res = query.UniqueResult();
                if (res == null) {
                    throw new RevisionDoesNotExistException(date);
                }

                return (long)res;
            } catch (NonUniqueResultException e) {
                throw new AuditException(e);
            }
        }

        public T FindRevision<T>(System.Type revisionEntityClass, long revision){
            ArgumentsTools.CheckNotNull(revision, "Entity revision");
            ArgumentsTools.CheckPositive(revision, "Entity revision");
            CheckSession();

            IQuery query = verCfg.RevisionInfoQueryCreator.getRevisionQuery(Session, revision);

            try {
                object revisionData = query.UniqueResult();

                if (revisionData == null) {
                    throw new RevisionDoesNotExistException(revision);
                }

                return (T)revisionData;
            } catch (NonUniqueResultException e) {
                throw new AuditException(e);
            }
        }

        public T GetCurrentRevision<T>(System.Type revisionEntityClass, bool persist)
        {
		    if (!(Session is IEventSource)) {
                throw new NotSupportedException("The provided session is not an EventSource!");// ORIG IllegalArgumentException
		    }

		    // Obtaining the current audit sync
		    AuditSync auditSync = verCfg.AuditSyncManager.get((IEventSource) Session);

		    // And getting the current revision data
		    return (T) auditSync.GetCurrentRevisionData(Session, persist);
	    }

	    public AuditQueryCreator CreateQuery() {
            return new AuditQueryCreator(verCfg, this);
        }
    }
}
