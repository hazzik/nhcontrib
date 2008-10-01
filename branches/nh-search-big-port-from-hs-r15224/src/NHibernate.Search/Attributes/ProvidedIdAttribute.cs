using System;
using NHibernate.Search.Bridge.Builtin;

namespace NHibernate.Search.Attributes
{
    /// <summary>
    /// Objects whose identifier is provided externally and not part of the object state
    /// should be marked with this annotation
    /// 
    /// This annotation should not be used in conjunction with <seealso cref="DocumentIdAttribute"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ProvidedIdAttribute : System.Attribute
    {
        string name= "providedId";

        private System.Type bridge = typeof (StringBridge);

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public System.Type Bridge
        {
            get { return bridge; }
            set { bridge = value; }
        }
    }
}