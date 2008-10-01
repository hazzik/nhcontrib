using NHibernate.Transaction;
using NHibernate.Util;

namespace NHibernate.Search.Backend.Impl
{
    /// <summary>
    ///  Execute some work inside a transaction synchronization
    /// </summary>
    internal class PostTransactionWorkQueueSynchronization : ISynchronization
    {
        private bool isConsumed;
        private readonly WorkQueue queue = new WorkQueue();
        private readonly IQueueingProcessor queueingProcessor;
        private readonly WeakHashtable queuePerTransaction;
        
        /// <summary>
        /// in transaction work
        /// </summary>
        /// <param name="queueingProcessor"></param>
        /// <param name="queuePerTransaction"></param>
        public PostTransactionWorkQueueSynchronization(IQueueingProcessor queueingProcessor,
                                                       WeakHashtable queuePerTransaction)
        {
            this.queueingProcessor = queueingProcessor;
            this.queuePerTransaction = queuePerTransaction;
        }

        #region ISynchronization Members

        public void BeforeCompletion()
        {
            queueingProcessor.PrepareWorks(queue);
        }

        public void AfterCompletion(bool success)
        {
            try
            {
                if (success)
                    queueingProcessor.PerformWorks(queue);
                else
                    queueingProcessor.CancelWorks(queue);
            }
            finally
            {
                isConsumed = true;
                //clean the Synchronization per Transaction
                //not needed in a strict sense but a cleaner approach and faster than the GC
                if (queuePerTransaction != null) queuePerTransaction.Remove(this);
            }
        }

        #endregion

        public void Add(Work work)
        {
            queueingProcessor.Add(work, queue);
        }

        public bool IsConsumed
        {
            get { return isConsumed; }
        }

        public void FlushWorks()
        {
            WorkQueue subQueue = queue.SplitQueue();
            queueingProcessor.PrepareWorks(subQueue);
            queueingProcessor.PerformWorks(subQueue);
        }
    }
}