using System;

namespace NHibernate.Search.Attributes
{
    /// <summary>
    /// Defines an analyzer for a given entity, method or field.
    /// The order of precedence is
    /// - FieldAttribute
    /// - field / method
    /// - entity
    /// - default
    /// 
    /// Either describe an explicit implementation through the <code>impl</code> parameter
    /// or use an external [AnalyzerDef] definition through the <code>def</code> parameter
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class AnalyzerAttribute : Attribute
    {
        private readonly System.Type type;
        private readonly string definition;

        public AnalyzerAttribute(string definition)
        {
            this.definition = definition;
        }

        public string Definition
        {
            get { return definition; }
        }

        public AnalyzerAttribute(System.Type value)
        {
            type = value;
        }

        public System.Type Type
        {
            get { return type; }
        }
    }  
}