using System;
using System.IO;
using Lucene.Net.Analysis;

namespace NHibernate.Search.Util
{
    /// <summary>
    /// delegate to a named analyzer
    /// delegated Analyzers are lazily configured
    /// </summary>
    public class DelegateNamedAnalyzer : Analyzer
    {
        private String name;
        private Analyzer inner;

        public DelegateNamedAnalyzer(String name)
        {
            this.name = name;
        }

        public Analyzer Inner
        {
            set { inner = value; }
        }

        public string Name
        {
            get { return name; }
        }

        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            return inner.TokenStream(fieldName, reader);
        }

        public override int GetPositionIncrementGap(string fieldName)
        {
            return inner.GetPositionIncrementGap(fieldName);
        }
    }
}