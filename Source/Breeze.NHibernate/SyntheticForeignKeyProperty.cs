using System;
using Breeze.NHibernate.Metadata;

namespace Breeze.NHibernate
{
    /// <summary>
    /// Represents a foreign key property for a many to one relation.
    /// </summary>W
    public class SyntheticForeignKeyProperty
    {
        /// <summary>
        /// The name of the synthetic property. In general it has a postfix with Id or Code
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Whether the many to one relation is nullable.
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Whether the synthetic property represents a part of the entity key.
        /// </summary>
        public bool IsPartOfKey { get; set; }

        /// <summary>
        /// The entity type of many to one relation.
        /// </summary>
        public Type EntityType { get; set; }

        /// <summary>
        /// Whether the property is defined in one of the derived classes.
        /// </summary>
        public bool Derived { get; set; }

        /// <summary>
        /// Whether the entity of many to one relation has a composite key.
        /// </summary>
        public bool HasCompositeKey { get; set; }

        /// <summary>
        /// The property name of the many to one relation.
        /// </summary>
        public string AssociationPropertyName { get; set; }

        /// <summary>
        /// Identifier data type of the many to one relation entity.
        /// </summary>
        public DataType IdentifierDataType { get; set; }

        /// <summary>
        /// Identifier type of the many to one relation entity.
        /// </summary>
        public Type IdentifierType { get; set; }

        /// <summary>
        /// Identifier length in case of string of the many to one relation entity.
        /// </summary>
        public int? IdentifierLength { get; set; }

        /// <summary>
        /// Identifier name of the many to one relation entity.
        /// </summary>
        public string IdentifierPropertyName { get; set; }

        /// <summary>
        /// A function that returns the identifier property value.
        /// </summary>
        public Func<object, object> GetIdentifierFunction { get; set; }

        /// <summary>
        /// A function that returns the association property value.
        /// </summary>
        public Func<object, object> GetAssociationFunction { get; set; }
    }
}
