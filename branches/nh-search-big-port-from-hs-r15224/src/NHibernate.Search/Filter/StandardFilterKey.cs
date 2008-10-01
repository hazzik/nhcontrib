using System;
using System.Collections;

namespace NHibernate.Search.Filter
{
    /// <summary>
    /// Implements a filter key using all injected parameters to compute
    /// equals and hashCode
    /// the order the parameters are added is significant
    /// </summary>
    public class StandardFilterKey : FilterKey
    {
        private readonly ArrayList parameters = new ArrayList();
        private bool implSet;

        #region Property methods

        public override System.Type Impl
        {
            get { return base.Impl; }
            set
            {
                base.Impl = value;
                // Add impl once and once only
                if (implSet)
                {
                    parameters[0] = value;
                }
                else
                {
                    implSet = true;
                    parameters.Insert(0, value);
                }
            }
        }

        #endregion

        #region Public methods

        public void AddParameter(object value)
        {
            parameters.Add(value);
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            int hash = 23;
            foreach (object param in parameters)
                hash = 31*hash + (param != null ? param.GetHashCode() : 0);

            return hash;
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>.</param>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
        /// </returns>
        /// <exception cref="T:System.NullReferenceException">
        /// The <paramref name="obj"/> parameter is null.
        /// </exception>
        public override bool Equals(object obj)
        {
            if (!(obj is StandardFilterKey))
                return false;
            StandardFilterKey that = (StandardFilterKey)obj;
            int size = parameters.Count;
            if (size != that.parameters.Count)
                return false;
            for (int index = 0; index < size; index++)
            {
                object paramThis = parameters[(index)];
                object paramThat = that.parameters[(index)];
                if (paramThis == null && paramThat != null)
                    return false;
                if (paramThis != null && !paramThis.Equals(paramThat))
                    return false;
            }
            return true;
        }

        #endregion
    }
}