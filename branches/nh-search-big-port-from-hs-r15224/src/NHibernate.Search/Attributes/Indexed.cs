using System;

namespace NHibernate.Search.Attributes
{
    /// <summary>
    /// Specifies that an entity is to be indexed by Lucene
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class IndexedAttribute : Attribute
    {
        private string index = string.Empty;

        public string Index
        {
            get { return index; }
            set { index = value; }
        }
    }
}