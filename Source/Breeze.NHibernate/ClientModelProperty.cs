using System;
using Breeze.NHibernate.Metadata;

namespace Breeze.NHibernate
{
    /// <summary>
    /// Represents a client model property.
    /// </summary>
    public class ClientModelProperty
    {
        /// <summary>
        /// Constructs an instance of <see cref="ClientModelProperty"/>.
        /// </summary>
        public ClientModelProperty(
            string name,
            Type type,
            bool isComplexType,
            DataType? dataType,
            bool isNullable,
            bool isPartOfKey,
            bool isCollectionType,
            bool isEntityType)
        {
            Name = name;
            Type = type;
            IsComplexType = isComplexType;
            DataType = dataType;
            IsNullable = isNullable;
            IsPartOfKey = isPartOfKey;
            IsCollectionType = isCollectionType;
            IsEntityType = isEntityType;
        }

        /// <summary>
        /// The property name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The property type.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Whether is a complex type.
        /// </summary>
        public bool IsComplexType { get; }

        /// <summary>
        /// The <see cref="DataType"/> of the property.
        /// </summary>
        public DataType? DataType { get; }

        /// <summary>
        /// Whether is nullable.
        /// </summary>
        public bool IsNullable { get; }

        /// <summary>
        /// Whether is part of the key.
        /// </summary>
        public bool IsPartOfKey { get; }

        /// <summary>
        /// Whether is a collection type.
        /// </summary>
        public bool IsCollectionType { get; }

        /// <summary>
        /// Whether is an entity type.
        /// </summary>
        public bool IsEntityType { get; }

        /// <summary>
        /// Whether is an association type.
        /// </summary>
        public bool IsAssociationType => IsEntityType || IsCollectionType;
    }
}
