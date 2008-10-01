using System;
using System.Collections.Generic;
using System.Reflection;
using NHibernate.Search.Attributes;
using NHibernate.Search.Bridge.Builtin;

namespace NHibernate.Search.Bridge
{
    public class BridgeFactory
    {
        public static readonly ITwoWayFieldBridge BOOLEAN =
            new TwoWayString2FieldBridgeAdaptor(new ValueTypeBridge<bool>());

        private static readonly Dictionary<string, IFieldBridge> builtInBridges = new Dictionary<string, IFieldBridge>();
        public static readonly IFieldBridge DATE_DAY = new String2FieldBridgeAdaptor(DateBridge.DATE_DAY);
        public static readonly IFieldBridge DATE_HOUR = new String2FieldBridgeAdaptor(DateBridge.DATE_HOUR);

        public static readonly ITwoWayFieldBridge DATE_MILLISECOND =
            new TwoWayString2FieldBridgeAdaptor(DateBridge.DATE_MILLISECOND);

        public static readonly IFieldBridge DATE_MINUTE = new String2FieldBridgeAdaptor(DateBridge.DATE_MINUTE);
        public static readonly IFieldBridge DATE_MONTH = new String2FieldBridgeAdaptor(DateBridge.DATE_MONTH);
        public static readonly IFieldBridge DATE_SECOND = new String2FieldBridgeAdaptor(DateBridge.DATE_SECOND);
        public static readonly IFieldBridge DATE_YEAR = new String2FieldBridgeAdaptor(DateBridge.DATE_YEAR);

        public static readonly ITwoWayFieldBridge DOUBLE =
            new TwoWayString2FieldBridgeAdaptor(new ValueTypeBridge<double>());

        public static readonly ITwoWayFieldBridge FLOAT =
            new TwoWayString2FieldBridgeAdaptor(new ValueTypeBridge<float>());

        public static readonly ITwoWayFieldBridge INTEGER =
            new TwoWayString2FieldBridgeAdaptor(new ValueTypeBridge<int>());

        public static readonly ITwoWayFieldBridge LONG =
            new TwoWayString2FieldBridgeAdaptor(new ValueTypeBridge<long>());

        public static readonly ITwoWayFieldBridge SHORT =
            new TwoWayString2FieldBridgeAdaptor(new ValueTypeBridge<short>());

        public static readonly ITwoWayFieldBridge STRING = new TwoWayString2FieldBridgeAdaptor(new StringBridge());

        public static readonly ITwoWayFieldBridge CLAZZ = new TwoWayString2FieldBridgeAdaptor(new ClassBridge());
        public static readonly ITwoWayFieldBridge URI = new TwoWayString2FieldBridgeAdaptor(new UriBridge());

        static BridgeFactory()
        {
            builtInBridges.Add(typeof(Double).Name, DOUBLE);
            builtInBridges.Add(typeof(float).Name, FLOAT);
            builtInBridges.Add(typeof(short).Name, SHORT);
            builtInBridges.Add(typeof(int).Name, INTEGER);
            builtInBridges.Add(typeof(long).Name, LONG);
            builtInBridges.Add(typeof(String).Name, STRING);
            builtInBridges.Add(typeof(Boolean).Name, BOOLEAN);
            builtInBridges.Add(typeof(System.Type).Name, CLAZZ);
            builtInBridges.Add(typeof(Uri).Name, URI);

            builtInBridges.Add(typeof(DateTime).Name, DATE_MILLISECOND);
        }

        private BridgeFactory()
        {
        }

        /// <summary>
        /// This extracts and instantiates the implementation class from a ClassBridge
        /// annotation.
        /// </summary>
        /// <param name="cb"></param>
        /// <returns></returns>
        public static IFieldBridge ExtractType(IBridgeAttribute cb)
        {
            IFieldBridge bridge = null;

            if (cb != null)
            {
                bridge = DoExtractType(cb, null);
            }
            if (bridge == null) throw new HibernateException("Unable to guess IFieldBridge ");

            return bridge;
        }

        public static IFieldBridge GuessType(MemberInfo member)
        {
            IFieldBridge bridge = null;
            FieldBridgeAttribute bridgeAnn = AttributeUtil.GetFieldBridge(member);
            if (bridgeAnn != null)
            {
                bridge = DoExtractType(bridgeAnn, member);
            }
            else if (AttributeUtil.IsDateBridge(member))
            {
                Resolution resolution =
                    AttributeUtil.GetDateBridge(member).Resolution;
                bridge = GetDateField(resolution);
            }
            else
            {
                //find in built-ins
                System.Type returnType = GetMemberType(member);
                if (IsNullable(returnType))
                    returnType = returnType.GetGenericArguments()[0];
                builtInBridges.TryGetValue(returnType.Name, out bridge);
                if (bridge == null && returnType.IsEnum)
                    bridge = new TwoWayString2FieldBridgeAdaptor(
                        new EnumBridge(returnType)
                        );
            }
            //TODO add classname
            if (bridge == null) throw new HibernateException("Unable to guess IFieldBridge for " + member.Name);
            return bridge;
        }

        private static IFieldBridge DoExtractType(IBridgeAttribute bridgeAnn, MemberInfo member)
        {
            System.Type impl = bridgeAnn.Impl;
            IFieldBridge bridge = null;
            try
            {
                Object instance = Activator.CreateInstance(impl);
                if (impl is IFieldBridge)
                    bridge = (IFieldBridge)instance;
                else if (impl is ITwoWayStringBridge)
                    bridge = new TwoWayString2FieldBridgeAdaptor(
                        (ITwoWayStringBridge)instance);
                else if (impl is IStringBridge)
                    bridge = new String2FieldBridgeAdaptor((StringBridge)instance);
                if (bridgeAnn.Parameters.Count > 0 && impl is IParameterizedBridge)
                    ((IParameterizedBridge)instance).SetParameterValues(bridgeAnn.Parameters);
            }
            catch (Exception e)
            {
                throw new HibernateException("Unable to instaniate IFieldBridge for " + member.DeclaringType.FullName + "." + member.Name, e);
            }
            return bridge;
        }


        /// <summary>
        /// Takes in a fieldBridge and will return you a TwoWayFieldBridge instance.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static ITwoWayFieldBridge ExtractTwoWayType(System.Type type)
        {
            IFieldBridge fb = ExtractType(new FieldBridgeAttribute(type));
            if (fb is ITwoWayFieldBridge)
                return (ITwoWayFieldBridge) fb;
            throw new SearchException("FieldBridge passed in is not an instance of ITwoWayFieldBridge");
        }

        private static bool IsNullable(System.Type returnType)
        {
            return returnType.IsGenericType && typeof(Nullable<>) == returnType.GetGenericTypeDefinition();
        }

        private static System.Type GetMemberType(MemberInfo member)
        {
            PropertyInfo prop = member as PropertyInfo;
            if (prop != null)
                return prop.PropertyType;
            else
                return ((FieldInfo)member).FieldType;
        }

        public static IFieldBridge GetDateField(Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Year:
                    return DATE_YEAR;
                case Resolution.Month:
                    return DATE_MONTH;
                case Resolution.Day:
                    return DATE_DAY;
                case Resolution.Hour:
                    return DATE_HOUR;
                case Resolution.Minute:
                    return DATE_MINUTE;
                case Resolution.Second:
                    return DATE_SECOND;
                case Resolution.Millisecond:
                    return DATE_MILLISECOND;
                default:
                    throw new AssertionFailure("Unknown Resolution: " + resolution);
            }
        }
    }
}