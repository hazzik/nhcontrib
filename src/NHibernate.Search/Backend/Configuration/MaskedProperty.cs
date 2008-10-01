using System;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Iesi.Collections.Generic;
using log4net;
using System.Collections.Generic;

namespace NHibernate.Search.Backend.Configuration
{
    /// <summary>
    /// A wrapper to Properties, to restrict the availability of
    /// values to only those which have a key beginning with some
    /// masking String.
    /// Supported methods to enumerate the list of properties are:
    ///   - propertyNames()
    ///   - Keys()
    ///   - keys()
    /// Other methods including methods returning Entries and values are not supported
    /// </summary>
    public class MaskedProperty
    {
        private const long serialVersionUID = -593307257383085113L;

        [NonSerialized] private readonly ILog log = LogManager.GetLogger(typeof (MaskedProperty));
        private readonly NameValueCollection masked;
        private readonly NameValueCollection fallBack;
        private readonly String radix;
        [NonSerialized] private readonly ISet<string> propertyNames = new HashedSet<string>();

        /// <summary>
        /// Provides a view to the provided Properties hiding
        /// all keys not starting with some [mask.].
        /// </summary>
        /// <param name="propsToMask"></param>
        /// <param name="mask"></param>
        public MaskedProperty(NameValueCollection propsToMask, String mask)
            : this(propsToMask, mask, null)
        {
        }

        /// <summary>
        /// Provides a view to the provided Properties hiding
        /// all keys not starting with some [mask.].
        /// If no value is found then a value is returned from propsFallBack,
        /// without masking.
        /// </summary>
        /// <param name="propsToMask"></param>
        /// <param name="mask"></param>
        /// <param name="propsFallBack"></param>
        public MaskedProperty(NameValueCollection propsToMask, String mask, NameValueCollection propsFallBack)
        {
            if (propsToMask == null)
                throw new ArgumentNullException("propsToMask");
            if (mask == null)
                throw new ArgumentNullException("mask");

           masked = propsToMask;
           radix = mask + ".";
           fallBack = propsFallBack;
        }


        public string this[string key]
        {
            get
            {
                string compositeKey = radix + key;
                string value = masked[compositeKey];
                if (value != null)
                {
                    log.DebugFormat("found a match for key: [{0}] value: {1}", compositeKey, value);
                    return value;
                }
                if (fallBack != null)
                {
                    return fallBack[key];
                }
                return null;
            }
        }

        public bool ContainsKey(string key)
        {
            return this[key] != null;
        }

        public string this[string key, string defaultValue]
        {
            get
            {
                String val = this[key];
                return val ?? defaultValue;
            }
        }

        public IEnumerable<string> PropertyNames()
        {
            InitPropertyNames();
            return propertyNames;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void InitPropertyNames()
        {
            if (propertyNames != null) return;
            List<string> maskedProperties = new List<string>();
            foreach (string key in masked)
            {
                if (key.StartsWith(radix))
                {
                    maskedProperties.Add(key.Substring(radix.Length, key.Length));
                }
            }
            if (fallBack != null)
            {
                foreach (string key in fallBack)
                {
                    maskedProperties.Add(key);
                }
            }
            propertyNames.AddAll(maskedProperties);
        }



        public bool Contains(string value)
        {
            InitPropertyNames();
            return propertyNames.Contains(value);
        }

        public bool IsEmpty()
        {
            InitPropertyNames();
            return propertyNames.Count == 0;
        }


        public ISet<string> Keys
        {
            get
            {
                InitPropertyNames();
                return propertyNames;
            }
        }


        public int Count
        {
            get
            {
                InitPropertyNames();
                return propertyNames.Count;
            }
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = ((fallBack == null) ? 0 : fallBack.GetHashCode());
            result = prime * result + masked.GetHashCode();
            result = prime * result + radix.GetHashCode();
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (GetType() != obj.GetType())
                return false;
            MaskedProperty other = (MaskedProperty)obj;
            if (fallBack == null)
            {
                if (other.fallBack != null)
                    return false;
            }
            else if (!fallBack.Equals(other.fallBack))
                return false;
            if (!masked.Equals(other.masked))
                return false;
            if (!radix.Equals(other.radix))
                return false;
            return true;
        }

        public NameValueCollection ToProperties()
        {
            InitPropertyNames();
            NameValueCollection properties = new NameValueCollection();
            foreach (string name in propertyNames)
            {
                properties.Add(name, this[name]);
            }
            return properties;
        }
    }

}