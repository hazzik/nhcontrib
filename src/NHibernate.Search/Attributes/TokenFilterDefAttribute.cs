using System;

namespace NHibernate.Search.Attributes
{
    /// <summary>
    /// Define a TokenFilterFactory and its parameters
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = false)]
    public class TokenFilterDefAttribute : System.Attribute
    {
        private readonly System.Type factory;
        private readonly object[] parameters;

        /// <summary>
        /// Create a new instance of <seealso cref="TokenFilterDefAttribute"/>
        /// </summary>
        /// <param name="factory">Defines the TokenFilterFactory implementation used</param>
        /// <param name="parameters">optional parameters passed to the TokenizerFactory</param>
        public TokenFilterDefAttribute(System.Type factory, params object[] parameters)
        {
            this.factory = factory;
            this.parameters = parameters;
        }

        /// <summary>
        /// Defines the TokenFilterFactory implementation used
        /// </summary>
        public System.Type Factory
        {
            get { return factory; }
        }

        /// <summary>
        /// optional parameters passed to the TokenizerFactory
        /// </summary>
        public object[] Parameters
        {
            get { return parameters; }
        }
    }
}