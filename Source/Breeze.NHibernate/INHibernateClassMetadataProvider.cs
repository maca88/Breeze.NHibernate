using System.Collections.Generic;
using NHibernate.Metadata;

namespace Breeze.NHibernate
{
    /// <summary>
    /// Provides methods to retrieve NHibernate class metadata.
    /// </summary>
    public interface INHibernateClassMetadataProvider
    {
        /// <summary>
        /// Gets the class metadata for the given entity name.
        /// </summary>
        /// <param name="entityName">The entity name.</param>
        /// <returns>The entity class metadata or <see langword="null"/> if not found.</returns>
        IClassMetadata Get(string entityName);

        /// <summary>
        /// Gets all class metadata.
        /// </summary>
        /// <returns>All class metadata.</returns>
        IEnumerable<IClassMetadata> GetAll();
    }
}
