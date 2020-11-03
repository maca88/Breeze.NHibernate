using System.Collections.Generic;

namespace Breeze.NHibernate.Metadata
{
    /// <summary>
    /// Represents a breeze navigation property.
    /// </summary>
    public class NavigationProperty : BaseProperty
    {
        /// <summary>
        /// The type of the entity or collection of entities returned by this property.
        /// </summary>
        public string EntityTypeName
        {
            get => Get<string>(nameof(EntityTypeName));
            set => Set(nameof(EntityTypeName), value);
        }

        /// <summary>
        /// Whether this property returns a single entity (true) or an array of entities (false).
        /// </summary>
        public bool IsScalar
        {
            get => Get(nameof(IsScalar), true);
            set => Set(nameof(IsScalar), value, true);
        }

        /// <summary>
        /// An arbitrary name that is used to link this navigation property to its inverse property. For bidirectional navigations this name will occur twice within this document, otherwise only once.
        /// </summary>
        public string AssociationName
        {
            get => Get<string>(nameof(AssociationName));
            set => Set(nameof(AssociationName), value);
        }

        /// <summary>
        /// An array of the names of the properties on this type that are the foreign key 'backing' for this navigation property.  This may only be set if 'isScalar' is true.
        /// </summary>
        public IReadOnlyCollection<string> ForeignKeyNames
        {
            get => Get<IReadOnlyCollection<string>>(nameof(ForeignKeyNames));
            set => Set(nameof(ForeignKeyNames), value);
        }

        /// <summary>
        /// Inverse foreign key names.
        /// </summary>
        public IReadOnlyCollection<string> InvForeignKeyNames
        {
            get => Get<IReadOnlyCollection<string>>(nameof(InvForeignKeyNames));
            set => Set(nameof(InvForeignKeyNames), value);
        }

        /// <summary>
        /// Same as ForeignKeyNames, but the names here are server side names as opposed to client side.  Only one or the other is needed.
        /// </summary>
        public IReadOnlyCollection<string> ForeignKeyNamesOnServer
        {
            get => Get<IReadOnlyCollection<string>>(nameof(ForeignKeyNamesOnServer));
            set => Set(nameof(ForeignKeyNamesOnServer), value);
        }

        /// <summary>
        /// Inverse foreign key names on the server.
        /// </summary>
        public IReadOnlyCollection<string> InvForeignKeyNamesOnServer
        {
            get => Get<IReadOnlyCollection<string>>(nameof(InvForeignKeyNamesOnServer));
            set => Set(nameof(InvForeignKeyNamesOnServer), value);
        }

        /// <summary>
        /// Whether has orphan delete set.
        /// </summary>
        public bool HasOrphanDelete
        {
            get => Get<bool>(nameof(HasOrphanDelete));
            set => Set(nameof(HasOrphanDelete), value);
        }
    }
}
