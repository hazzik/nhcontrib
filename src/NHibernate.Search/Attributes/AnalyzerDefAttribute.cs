using System;

namespace NHibernate.Search.Attributes
{

    /// <summary>
    /// Reusable analyzer definition.
    /// An analyzer definition defines:
    ///  - one tokenizer
    ///  - optionally some filters
    /// 
    /// Filters are applied in the order they are defined
    /// Reuses the Solr Tokenizer and Filter architecture
    ///  </summary>
    /// <remarks>
    /// We allow multiple instances of this attribute rather than having a AnalyzerDefs as per Java
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true)]
    public class AnalyzerDefAttribute : System.Attribute
    {
        private string name;
        private TokenizerDef tokenizer;
        private TokenFilterDef[] filters;

        ///<summary>
        /// Reference name to be used on <seealso cref="AnalyzerAttribute"/>
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// Tokenizer used 
        /// </summary>
        public TokenizerDef Tokenizer
        {
            get
            {
                return tokenizer;
            }
        }

        /// <summary>
        /// Filters used. The filters are applied in the defined order 
        /// </summary>
        public TokenFilterDef[] Filters
        {
            get
            {
                return filters;
            }
        }

        //TODO: fix this
        public class TokenFilterDef
        {
        }

        //TODO: fix this
        public class TokenizerDef
        {
        }
    }
}

   