﻿
namespace Breeze.NHibernate
{
    /// <summary>
    /// Represents an entity key.
    /// </summary>
    public class EntityKey
    {
        /// <summary>
        /// Constructs an instance of <see cref="EntityKey"/>.
        /// </summary>
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
