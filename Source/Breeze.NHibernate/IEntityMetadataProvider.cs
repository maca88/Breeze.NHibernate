using System;

namespace Breeze.NHibernate
{
    /// <summary>
    /// Provides metadata for entity types.
    /// </summary>
    public interface IEntityMetadataProvider
    {
        /// <summary>
        /// Gets the <see cref="EntityMetadata"/> for the given type.
        /// </summary>
        /// <param name="entityType">The entity type.</param>
        /// <returns>The entity metadata.</returns>
        /// <exception cref="InvalidOperationException">Thrown when <see cref="EntityMetadata"/> does not exist for the given entity type.</exception>
        EntityMetadata GetMetadata(Type entityType);

        /// <summary>
        /// Checks whether the given type is an entity type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>Whether the given type is an entity type.</returns>
        bool IsEntityType(Type type);
    }
}
