using System.Collections.Generic;

namespace NHibernate.Search.Attributes
{
    public interface IBridgeAttribute
    {
        System.Type Impl { get; }
        IDictionary<string, object> Parameters{ get;}
    }
}