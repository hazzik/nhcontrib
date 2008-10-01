using System.Collections;
using NHibernate.Engine;
using NHibernate.Search.Engine;
using NHibernate.Search.Impl;
using NHibernate.Util;
using System.Collections.Generic;

namespace NHibernate.Search.Backend.Impl
{

    /// <summary>
    /// Queue works per transaction.
    /// If out of transaction, the work is executed right away
    /// When <code>hibernate.search.worker.type</code> is set to <code>async</code>
    /// the work is done in a separate thread (threads are pooled)
    /// </summary>
    public class TransactionalWorker : IWorker
    {
        //not a synchronized map since for a given transaction, we have not concurrent access
        private IQueueingProcessor queueingProcessor;
        protected WeakHashtable synchronizationPerTransaction = new WeakHashtable();

        #region IWorker Members

        public void PerformWork(Work work, ITransactionContext transactionContext)
        {
            if (transactionContext.IsTransactionInProgress)
            {
                object transaction = transactionContext.TransactionIdentifier;
                PostTransactionWorkQueueSynchronization txSync = (PostTransactionWorkQueueSynchronization)
                                                                 synchronizationPerTransaction[transaction];
                if (txSync == null || txSync.IsConsumed)
                {
                    txSync =
                        new PostTransactionWorkQueueSynchronization(queueingProcessor, synchronizationPerTransaction);
                    transactionContext.RegisterSynchronization(txSync);
                    synchronizationPerTransaction[transaction] = txSync;
                }
                txSync.Add(work);
            }
            else
            {
                WorkQueue queue = new WorkQueue(2); //one work can be split
                queueingProcessor.Add(work, queue);
                queueingProcessor.PrepareWorks(queue);
                queueingProcessor.PerformWorks(queue);
            }
        }

        #endregion

        public void Initialize(IDictionary<string,string>  props, ISearchFactoryImplementor searchFactory)
        {
            queueingProcessor = new BatchedQueueingProcessor(searchFactory, props);
        }

        public void Close()
        {
            queueingProcessor.Close();
        }

        public void FlushWorks(ITransactionContext transactionContext)
        {
            if (!transactionContext.IsTransactionInProgress) 
                return;

            object transaction = transactionContext.TransactionIdentifier;
            PostTransactionWorkQueueSynchronization txSync = (PostTransactionWorkQueueSynchronization)
                                                             synchronizationPerTransaction[transaction];
            if (txSync != null && !txSync.IsConsumed)
            {
                txSync.FlushWorks();
            }
        }
    }
}