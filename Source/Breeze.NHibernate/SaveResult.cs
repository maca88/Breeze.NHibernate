using System.Collections.Generic;

namespace Breeze.NHibernate
{
    /// <summary>
    /// Result of a save, which may have either Entities, KeyMappings and DeletedKeys, or EntityErrors
    /// </summary>
    public class SaveResult
    {
        /// <summary>
        /// The entities that were saved/updated/deleted.
        /// </summary>
        public List<object> Entities { get; set; }

        /// <summary>
        /// The key mappings for entities with auto-generated primary key.
        /// </summary>
        public List<KeyMapping> KeyMappings { get; set; }

        /// <summary>
        /// Deleted entity keys.
        /// </summary>
        public List<EntityKey> DeletedKeys { get; set; }
    }
}
