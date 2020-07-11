
namespace Breeze.NHibernate
{
    /// <summary>
    /// Represents an entity key.
    /// </summary>
    public class EntityKey
    {
        public EntityKey(string entityTypeName, object keyValue)
        {
            EntityTypeName = entityTypeName;
            KeyValue = keyValue;
        }

        /// <summary>
        /// The entity type name
        /// </summary>
        public string EntityTypeName { get; }

        /// <summary>
        /// The entity key value.
        /// </summary>
        public object KeyValue { get; }
    }
}
