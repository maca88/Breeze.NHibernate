﻿using Newtonsoft.Json.Linq;

namespace Breeze.NHibernate
{
    /// <summary>
    /// The entity aspect send by the client.
    /// </summary>
    public class EntityAspect
    {
        /// <summary>
        /// The entity type name.
        /// </summary>
        public string EntityTypeName { get; set; }

        /// <summary>
        /// The default resource name.
        /// </summary>
        public string DefaultResourceName { get; set; }

        /// <summary>
        /// The entity state.
        /// </summary>
        public EntityState EntityState { get; set; }

        /// <summary>
        /// The original properties values.
        /// </summary>
        public JObject OriginalValuesMap { get; set; }

        /// <summary>
        /// The entity auto-generated property.
        /// </summary>
        public AutoGeneratedKey AutoGeneratedKey { get; set; }
    }
}
