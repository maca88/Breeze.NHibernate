using System.Collections.Generic;
using NHibernate;

namespace Breeze.NHibernate
{
    /// <summary>
    /// Initializes NHibernate proxies and collections by using <see cref="NHibernateUtil.Initialize"/>.
    /// </summary>
    public interface IProxyInitializer
    {
        /// <summary>
        /// Initializes NHibernate proxies or collection for the given expand paths.
        /// </summary>
        /// <param name="value">The entity or collection to expand.</param>
        /// <param name="expandPaths">The expand paths to initialize.</param>
        void Initialize(object value, IEnumerable<string> expandPaths);
    }
}
