using System;

namespace NHibernate.Search.Filter
{
    /// <summary>
    /// The key object must implement equals / hashcode so that 2 keys are equals if and only if
    /// the given Filter types are the same and the set of parameters are the same.
    ///
    /// The FilterKey creator (ie the @Key method) does not have to inject <code>impl</code>
    /// It will be done by Hibernate Search
    /// </summary>
    public abstract class FilterKey
    {
        private System.Type impl;

        /// <summary>
        /// Represent the @FullTextFilterDef.impl class
        /// </summary>
        public virtual System.Type Impl
        {
            get { return impl; }
            set { impl = value; }
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public abstract override int GetHashCode();

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
        public abstract override bool Equals(object obj);
    }
}