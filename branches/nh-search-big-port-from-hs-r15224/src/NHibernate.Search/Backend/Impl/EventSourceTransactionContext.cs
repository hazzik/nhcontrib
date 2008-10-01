using NHibernate.Event;
using NHibernate.Transaction;

namespace NHibernate.Search.Backend.Impl
{
    /// <summary>
    /// Implementation of the transactional context on top of an EventSource (Session)
    /// </summary>
    public class EventSourceTransactionContext : ITransactionContext
    {
        private readonly IEventSource eventSource;

        public EventSourceTransactionContext(IEventSource eventSource)
        {
            this.eventSource = eventSource;
        }

        public bool IsTransactionInProgress
        {
            get { return eventSource.TransactionInProgress; }
        }

        public object TransactionIdentifier
        {
            get { return eventSource.Transaction; }
        }

        public void RegisterSynchronization(ISynchronization synchronization)
        {
            eventSource.Transaction.RegisterSynchronization(synchronization);
        }
    }
}