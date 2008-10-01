using System.Collections.Generic;

namespace NHibernate.Search.Bridge
{
    /// <summary>
    /// Allow parameter injection to a given bridge
    /// </summary>
    public interface IParameterizedBridge
    {
        void SetParameterValues(IDictionary<string, object> parameters);
    }
}