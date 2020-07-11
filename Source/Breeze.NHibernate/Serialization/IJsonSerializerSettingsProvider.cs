using Newtonsoft.Json;

namespace Breeze.NHibernate.Serialization
{
    /// <summary>
    /// Provides <see cref="JsonSerializerSettings"/> for serialization of NHibernate entities.
    /// </summary>
    public interface IJsonSerializerSettingsProvider
    {
        /// <summary>
        /// Gets the default <see cref="JsonSerializerSettings"/> used for serializing query results.
        /// </summary>
        /// <returns>The default <see cref="JsonSerializerSettings"/>.</returns>
        JsonSerializerSettings GetDefault();

        /// <summary>
        /// Gets the <see cref="JsonSerializerSettings"/> used for deserializing <see cref="EntityAspect"/> when saving entity changes from the client.
        /// </summary>
        /// <returns>The <see cref="JsonSerializerSettings"/> for saving.</returns>
        JsonSerializerSettings GetForSave();
    }
}
