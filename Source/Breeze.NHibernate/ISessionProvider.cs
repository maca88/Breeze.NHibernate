using System;
using NHibernate;

namespace Breeze.NHibernate
{
    /// <summary>
    /// A provider for NHibernate <see cref="ISession"/>.
    /// </summary>
    public interface ISessionProvider
    {
        /// <summary>
        /// Gets the session related to the given type.
        /// </summary>
        /// <param name="modelType">The type to get the session from.</param>
        /// <returns>The session that is related to the given type or <see langword="null"/> if not related to any session.</returns>
        ISession Get(Type modelType);
    }
}
