using System;

namespace NHibernate.Search.Attributes
{
    /// <summary>
    /// Specifies a similarity implementation to use
    /// </summary>
    public class SimilarityAttribute : Attribute
    {
        private System.Type impl;

        public SimilarityAttribute(System.Type impl)
        {
            this.impl = impl;
        }

        public System.Type Impl
        {
            get { return impl; }
        }
    }
}