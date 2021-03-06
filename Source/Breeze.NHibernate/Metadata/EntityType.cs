﻿using System;
using System.Collections.Generic;

namespace Breeze.NHibernate.Metadata
{
    /// <summary>
    /// Represents a breeze entity type.
    /// </summary>
    public class EntityType : StructuralType
    {
        /// <summary>
        /// Constructs an instance of <see cref="EntityType"/>.
        /// </summary>
        public EntityType(Type type) : base(type)
        {
        }

        /// <summary>
        /// The base type name.
        /// </summary>
        public string BaseTypeName
        {
            get => Get<string>(nameof(BaseTypeName));
            set => Set(nameof(BaseTypeName), value);
        }

        /// <summary>
        /// Defines the mechanism by which the key for entities of this type are determined on the server. 'None' means that the client sets the key.
        /// </summary>
        public AutoGeneratedKeyType AutoGeneratedKeyType
        {
            get => Get<AutoGeneratedKeyType>(nameof(AutoGeneratedKeyType));
            set => Set(nameof(AutoGeneratedKeyType), value);
        }

        /// <summary>
        /// The default name by which entities of this type will be queried. Multiple 'resourceNames' may query for the same entityType, but only one is the default.
        /// </summary>
        public string DefaultResourceName
        {
            get => Get<string>(nameof(DefaultResourceName));
            set => Set(nameof(DefaultResourceName), value);
        }

        /// <summary>
        /// A list of navigation properties.
        /// </summary>
        public List<NavigationProperty> NavigationProperties
        {
            get => GetOrCreate(nameof(NavigationProperties), () => new List<NavigationProperty>());
            set => Set(nameof(NavigationProperties), value);
        }
    }
}
