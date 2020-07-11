using System;

namespace Breeze.NHibernate
{
    /// <summary>
    /// Provides metadata for client model types.
    /// </summary>
    public interface IClientModelMetadataProvider
    {
        /// <summary>
        /// Gets the metadata for the given type.
        /// </summary>
        /// <param name="clientType">The client model type.</param>
        /// <returns>The client model metadata.</returns>
        ClientModelMetadata GetMetadata(Type clientType);

        /// <summary>
        /// Check whether the given type is a client model.
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>Whether the given type is a client model</returns>
        bool IsClientModel(Type type);
    }
}
