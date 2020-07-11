using System;
using System.Collections.Generic;
using NHibernate.Type;

namespace Breeze.NHibernate
{
    /// <summary>
    /// Represents an association between two types.
    /// </summary>
    public class Association
    {
        public Association(Type entityType, IReadOnlyList<string> foreignKeyPropertyNames, ForeignKeyDirection foreignKeyDirection, bool isScalar)
        {
            EntityType = entityType;
            ForeignKeyPropertyNames = foreignKeyPropertyNames;
            ForeignKeyDirection = foreignKeyDirection;
            IsScalar = isScalar;
        }

        /// <summary>
        /// The associated entity type.
        /// </summary>
        public Type EntityType { get; }

        /// <summary>
        /// Whether the association is scalar.
        /// </summary>
        public bool IsScalar { get; }

        /// <summary>
        /// Name of foreign key property names of the association.
        /// </summary>
        public IReadOnlyList<string> ForeignKeyPropertyNames { get; }

        /// <summary>
        /// The foreign key direction.
        /// </summary>
        public ForeignKeyDirection ForeignKeyDirection { get; }
    }
}
