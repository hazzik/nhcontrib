namespace NHibernate.Search.Backend
{
    /// <summary>
    ///	 Pile work operations
    ///  No thread safety has to be implemented, the queue being thread scoped already
    ///  The implementation must be "stateless" wrt the queue through (ie not store the queue state)
    /// </summary>
    public interface IQueueingProcessor
    {
        /// <summary>
        /// Execute works
        /// </summary>
        /// <param name="workQueue"></param>
        void PerformWorks(WorkQueue workQueue);

        /// <summary>
        /// Rollback works
        /// </summary>
        /// <param name="workQueue"></param>
        void CancelWorks(WorkQueue workQueue);

        /// <summary>
        /// Add a work
        /// </summary>
        /// <param name="work"></param>
        /// <param name="workQueue"></param>
        void Add(Work work, WorkQueue workQueue);

        /// <summary>
        /// prepare resources for a later performWorks call
        /// </summary>
        /// <param name="workQueue"></param>
        void PrepareWorks(WorkQueue workQueue);

        /// <summary>
        /// clean resources
        /// This method should log errors rather than raise an exception
        /// </summary>
        void Close();
    }
}