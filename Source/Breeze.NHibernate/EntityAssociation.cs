using System;
using System.Collections.Generic;
using NHibernate.Engine;
using NHibernate.Type;

namespace Breeze.NHibernate
{
    /// <summary>
    /// Represents an entity association (many-to-one or one-to-one).
    /// </summary>
    public class EntityAssociation : Association
    {
        private readonly Action<object, object> _addFunction;

        private readonly Action<object, object> _removeFunction;

        public EntityAssociation(
            Type entityType,
            bool isOneToOne,
            IReadOnlyList<string> foreignKeyPropertyNames,
            ForeignKeyDirection foreignKeyDirection,
            IType identifierType,
            bool isChild,
            string inverseAssociationPropertyName,
            CascadeStyle inverseAssociationPropertyCascadeStyle,
            string associationPropertyName,
            CascadeStyle associationPropertyCascadeStyle,
            Action<object, object> addFunction,
            Action<object, object> removeFunction)
        : base(entityType, foreignKeyPropertyNames, foreignKeyDirection, true)
        {
            IsOneToOne = isOneToOne;
            IdentifierType = identifierType;
            IsChild = isChild;
            InverseAssociationPropertyName = inverseAssociationPropertyName;
            InverseAssociationPropertyCascadeStyle = inverseAssociationPropertyCascadeStyle;
            AssociationPropertyName = associationPropertyName;
            AssociationPropertyCascadeStyle = associationPropertyCascadeStyle;
            _addFunction = addFunction;
            _removeFunction = removeFunction;
        }

        /// <summary>
        /// Whether it is an one-to-one association.
        /// </summary>
        public bool IsOneToOne { get; }

        /// <summary>
        /// Identifier type of the associated entity.
        /// </summary>
        public IType IdentifierType { get; }

        /// <summary>
        /// Whether the association is treated as a child association.
        /// </summary>
        public bool IsChild { get; }

        /// <summary>
        /// The inverse property name of the associated entity (in a many-to-one relation is the name of the collection).
        /// </summary>
        public string InverseAssociationPropertyName { get; }

        /// <summary>
        /// The cascade style for the inverse property of the associated entity (in a many-to-one relation is the cascade style of the collection).
        /// </summary>
        public CascadeStyle InverseAssociationPropertyCascadeStyle { get; }

        /// <summary>
        /// The association property name.
        /// </summary>
        public string AssociationPropertyName { get; }

        /// <summary>
        /// The cascade style for the association property.
        /// </summary>
        public CascadeStyle AssociationPropertyCascadeStyle { get; }

        /// <summary>
        /// Removes a child from its parent.
        /// </summary>
        /// <param name="child">The child to remove.</param>
        /// <param name="parent">The parent to remove from.</param>
        public void RemoveFromParent(object child, object parent)
        {
            _removeFunction?.Invoke(child, parent);
        }

        /// <summary>
        /// Adds a child to a parent.
        /// </summary>
        /// <param name="child">The child to add.</param>
        /// <param name="parent">The parent to add it.</param>
        public void AddToParent(object child, object parent)
        {
            _addFunction?.Invoke(child, parent);
        }
    }
}
