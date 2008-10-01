using System.Collections;
using NHibernate.Search.Engine;
using System.Collections.Generic;

namespace NHibernate.Search.Backend
{
    /// <summary>
    /// Perform work for a given session. This implementation has to be multi threaded
    /// </summary>
    public interface IWorker
    {
        /// <summary>
        /// Perform the work on the session
        /// </summary>
        /// <param name="work">The work.</param>
        /// <param name="transactionContext">The transaction context.</param>
        //Use of EventSource since it's the common subinterface for Session and SessionImplementor
        //the alternative would have been to do a subcasting or to retrieve 2 parameters :(
        void PerformWork(Work work, ITransactionContext transactionContext);

        /// <summary>
        /// Initialize the worker
        /// </summary>
        /// <param name="props"></param>
        /// <param name="searchFactory"></param>
        void Initialize(IDictionary<string,string>  props, ISearchFactoryImplementor searchFactory);

        /// <summary>
        /// clean resources
        /// This method can return exceptions
        /// </summary>
        void Close();

        /// <summary>
        /// flush any work queue
        /// </summary>
        /// <param name="transactionContext"></param>
        void FlushWorks(ITransactionContext transactionContext);
    }
}