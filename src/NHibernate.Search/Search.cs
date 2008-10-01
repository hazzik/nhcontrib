using NHibernate.Search.Impl;

namespace NHibernate.Search
{
    /// <summary>
    /// Helper class to get a FullTextSession out of a regular session.
    /// </summary>
    public static class Search
    {
        public static IFullTextSession GetFullTextSession(ISession session)
        {
            return session as FullTextSessionImpl ??
                new FullTextSessionImpl(session);
        }
    }
}
