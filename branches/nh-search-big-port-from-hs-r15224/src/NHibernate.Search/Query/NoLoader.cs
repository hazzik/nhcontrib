using System.Collections;
using NHibernate.Search.Engine;

namespace NHibernate.Search.Query
{
    internal class NoLoader : ILoader
    {
        public void Init(ISession session, ISearchFactoryImplementor searchFactoryImplementor)
        {
            
        }

        public object Load(EntityInfo entityInfo)
        {
            throw new System.NotSupportedException("noLoader should not be used");
        }

        public IList Load(params EntityInfo[] entityInfos)
        {
            throw new System.NotSupportedException("noLoader should not be used");
        }
    }
}