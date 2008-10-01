using System;
using System.Collections.Generic;

namespace NHibernate.Search.Query
{
    public class FullTextFilterImpl : IFullTextFilter
    {
        private readonly Dictionary<string, object> parameters = new Dictionary<string, object>();
        private string name;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public IFullTextFilter SetParameter(string paramName, object value)
        {
            parameters[paramName] = value;
        }

        public object GetParameter(string paramName)
        {
            object value;
            if (parameters.TryGetValue(paramName, out value) == false)
                return null;
            return value;
        }

        public Dictionary<string, object> Parameters
        {
            get { return parameters; }
        }
    }
}