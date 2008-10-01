using System;

namespace NHibernate.Search.Bridge.Builtin
{
    public class UriBridge : ITwoWayStringBridge
    {
        public Object StringToObject(String stringValue)
        {
            if (string.IsNullOrEmpty(stringValue))
            {
                return null;
            }
            try
            {
                return new Uri(stringValue);
            }
            catch (Exception e)
            {
                throw new SearchException("Unable to build URI: " + stringValue, e);
            }
        }

        public String ObjectToString(Object @object)
        {
            return @object == null ? 
                null :
                @object.ToString();
        }
    }
}