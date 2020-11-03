using System;
using System.Collections.Generic;
using System.Linq;
using Breeze.NHibernate.Internal;

namespace Breeze.NHibernate
{
    /// <summary>
    /// Contains information of a save operation.
    /// </summary>
    public class SaveWorkState
    {
        /// <summary>
        /// Constructs an instance of <see cref="SaveWorkState"/>.
        /// </summary>
        public SaveWorkState(Dictionary<Type, List<EntityInfo>> saveMap)
        {
            SaveMap = saveMap;
        }

        /// <summary>
        /// The save map to be saved.
        /// </summary>
        public Dictionary<Type, List<EntityInfo>> SaveMap { get; }

        /// <summary>
        /// The key mappings for entities with auto-generated primary key.
        /// </summary>
        public List<KeyMapping> KeyMappings { get; set; }

        /// <summary>
        /// Creates a <see cref="SaveResult"/>.
        /// </summary>
        /// <returns>The save result.</returns>
        public SaveResult ToSaveResult()
        {
            var entities = new List<object>();
            var deletedKeys = new List<EntityKey>();
            foreach (var entityInfo in SaveMap.SelectMany(o => o.Value))
            {
                var state = entityInfo.EntityState;
                if (state != EntityState.Detached)
                {
                    entities.Add(entityInfo.ClientEntity);
                }

                if (state == EntityState.Deleted || state == EntityState.Detached)
                {
                    deletedKeys.Add(new EntityKey(
                        BreezeHelper.GetBreezeTypeFullName(entityInfo.EntityType),
                        entityInfo.GetIdentifierValues()));
                }
            }

            return new SaveResult
            {
                Entities = entities,
                KeyMappings = KeyMappings,
                DeletedKeys = deletedKeys
            };
        }
    }
}
