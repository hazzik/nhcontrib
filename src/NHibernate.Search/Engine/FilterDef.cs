using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using NHibernate.Search.Attributes;

namespace NHibernate.Search.Engine
{
    /// <summary>
    /// A wrapper class which encapsualtes all required information to create a defined filter
    /// </summary>
    public class FilterDef
    {
        private System.Type impl;
        private MethodInfo factoryMethod;
        private MethodInfo keyMethod;
        private readonly Dictionary<string, MethodInfo> setters = new Dictionary<string, MethodInfo>();
        private FilterCacheModeType cacheMode=FilterCacheModeType.None;
        private readonly string name;

        public FilterDef(FullTextFilterDefAttribute def)
        {
            name = def.Name;
            impl = def.Impl;
            cacheMode = def.CacheMode;
        }

        #region Property methods

        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// Gets or sets the key method.
        /// </summary>
        /// <value>The key method.</value>
        public MethodInfo KeyMethod
        {
            get { return keyMethod; }
            set { keyMethod = value; }
        }

        /// <summary>
        /// Gets or sets the factory method.
        /// </summary>
        /// <value>The factory method.</value>
        public MethodInfo FactoryMethod
        {
            get { return factoryMethod; }
            set { factoryMethod = value; }
        }

        /// <summary>
        /// Gets or sets the impl.
        /// </summary>
        /// <value>The impl.</value>
        public System.Type Impl
        {
            get { return impl; }
            set { impl = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="FilterDef"/> is cache.
        /// </summary>
        /// <value><c>true</c> if cache; otherwise, <c>false</c>.</value>
        public FilterCacheModeType CacheMode
        {
            get { return cacheMode; }
            set { cacheMode = value; }
        }

        #endregion

        #region Public methods

        public void Invoke(string parameterName, object filter, object parameterValue)
        {
            MethodInfo method = setters[parameterName];
            if (method == null)
            {   throw new NotSupportedException(
                    string.Format(CultureInfo.InvariantCulture, "No setter {0} found in {1}", parameterName,
                                  impl != null ? impl.Name : "<impl>"));
            }

            method.Invoke(filter, new object[] { parameterValue });
        }

        public void AddSetter(string name, MethodInfo method)
        {
            setters.Add(name, method);
        }

        #endregion
    }
}