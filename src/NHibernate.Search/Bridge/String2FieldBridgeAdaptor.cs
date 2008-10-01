using System;
using Lucene.Net.Documents;
using NHibernate.Util;

namespace NHibernate.Search.Bridge
{
    /// <summary>
    /// Bridge to use a StringBridge as a FieldBridge
    /// </summary>
    public class String2FieldBridgeAdaptor : IFieldBridge
    {
        private readonly IStringBridge stringBridge;

        public String2FieldBridgeAdaptor(IStringBridge stringBridge)
        {
            this.stringBridge = stringBridge;
        }

        public void Set(string name, object value, Document document, LuceneOptions luceneOptions)
        {
            String indexedString = stringBridge.ObjectToString(value);
            //Do not add fields on empty strings, seems a sensible default in most situations
            //TODO if Store, probably also save empty ones
            if (StringHelper.IsNotEmpty(indexedString))
            {
                Field field = new Field(name, indexedString, luceneOptions.Store, luceneOptions.Index);
                field.SetBoost(luceneOptions.Boost);
                document.Add(field);
            }
        }
    }
}