using System;
using System.Collections.Generic;

namespace Breeze.NHibernate
{
    /// <summary>
    /// A context used when saving entity changes.
    /// </summary>
    public class SaveChangesContext
    {
        internal SaveChangesContext(Dictionary<Type, List<EntityInfo>> saveMap, SaveOptions saveOptions)
        {
            SaveMap = saveMap;
            SaveOptions = saveOptions;
        }

        /// <summary>
        /// The save map sent by the client.
        /// </summary>
        public Dictionary<Type, List<EntityInfo>> SaveMap { get; }

        /// <summary>
        /// The save options sent by the client.
        /// </summary>
        public SaveOptions SaveOptions { get; }

        /// <summary>
        /// A storage for custom data that will be used in the saving process.
        /// </summary>
        public Dictionary<string, object> Data { get; } = new Dictionary<string, object>();

        /// <summary>
        /// List of entities that were added/updated/deleted only on the server. The entities in this list
        /// will be included in <see cref="SaveResult.Entities"/> and <see cref="SaveResult.DeletedKeys"/>.
        /// </summary>
        internal Dictionary<object, EntityState> AdditionalEntities { get; private set; }

        /// <summary>
        /// Adds and entity that was added/updated/deleted only on the server in <see cref="SaveResult.Entities"/> or
        /// <see cref="SaveResult.DeletedKeys"/>, depending of <see cref="EntityState"/>.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <param name="entityState">The entity state.</param>
        public void AddAdditionalEntity(object entity, EntityState entityState)
        {
            if (AdditionalEntities == null)
            {
                AdditionalEntities = new Dictionary<object, EntityState>();
            }

            AdditionalEntities.Add(entity, entityState);
        }

        /// <summary>
        /// Adds entities that were added/updated/deleted only on the server in <see cref="SaveResult.Entities"/> or
        /// <see cref="SaveResult.DeletedKeys"/>, depending of <see cref="EntityState"/>.
        /// </summary>
        /// <param name="entities">The entities to add.</param>
        /// <param name="entityState">The entity state.</param>
        public void AddAdditionalEntities(IEnumerable<object> entities, EntityState entityState)
        {
            if (AdditionalEntities == null)
            {
                AdditionalEntities = new Dictionary<object, EntityState>();
            }

            foreach (var entity in entities)
            {
                AdditionalEntities.Add(entity, entityState);
            }
        }
    }
}
