using NHibernate.Transaction;

namespace NHibernate.Search.Backend
{
    /// <summary>
    /// Contract needed by Hibernate Search to bach changes per transactio
    /// </summary>
    public interface ITransactionContext
    {
        /// <returns> 
        /// A boolean whether a transaction is in progress or not. 
        /// </returns>
        bool IsTransactionInProgress { get; }

        /// <returns> a transaction object. </returns>
        object TransactionIdentifier { get; }

        ///	<summary>
        /// register the givne synchronization
        ///	</summary>
        /// <param name="synchronization"> synchronization to register</param>
        void RegisterSynchronization(ISynchronization synchronization);
    }
}