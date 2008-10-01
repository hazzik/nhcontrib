using NHibernate.Util;

namespace NHibernate.Search.Bridge.Builtin
{
    /// <summary>
    /// Convert a Class back and forth
    /// </summary>
    public class ClassBridge : ITwoWayStringBridge 
    {
        public string ObjectToString(object obj)
        {
            if (obj == null) return null;

            System.Type type = ((System.Type) obj);
            // using this way to avoid getting locked for a specific version
            return type.FullName + ", " + type.Assembly.GetName().Name;
        }

        public object StringToObject(string stringValue)
        {
            if (string.IsNullOrEmpty(stringValue))
                return null;
            return ReflectHelper.ClassForName(stringValue);
        }
    }
}