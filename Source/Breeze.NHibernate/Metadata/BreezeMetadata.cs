using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Breeze.NHibernate.Metadata
{
    /// <summary>
    /// Metadata describing the entity models.
    /// </summary>
    public class BreezeMetadata : MetadataObject
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = {new StringEnumConverter()}
        };

        /// <summary>
        /// Array of entity type/complex type names to their metadata definitions. The key is a structural type name and the value is
        /// either an <see cref="EntityType"/> or a <see cref="ComplexType"/>.
        /// </summary>
        public List<StructuralType> StructuralTypes
        {
            get => Get<List<StructuralType>>(nameof(StructuralTypes));
            set => Set(nameof(StructuralTypes), value);
        }

        /// <summary>
        /// Map of resource names to entity type names.
        /// </summary>
        public Dictionary<string, string> ResourceEntityTypeMap
        {
            get => Get<Dictionary<string, string>>(nameof(ResourceEntityTypeMap));
            set => Set(nameof(ResourceEntityTypeMap), value);
        }

        /// <summary>
        /// The serialization version for this document.
        /// </summary>
        public string MetadataVersion
        {
            get => Get<string>(nameof(MetadataVersion));
            set => Set(nameof(MetadataVersion), value);
        }

        /// <summary>
        /// On deserialization, this must match the name of some 'namingConvention' already registered on the breeze client.
        /// </summary>
        public string NamingConvention
        {
            get => Get<string>(nameof(NamingConvention));
            set => Set(nameof(NamingConvention), value);
        }

        /// <summary>
        /// On deserialization, this must match the name of some 'localQueryComparisonOptions' already registered on the breeze client.
        /// </summary>
        public string LocalQueryComparisonOptions
        {
            get => Get<string>(nameof(LocalQueryComparisonOptions));
            set => Set(nameof(LocalQueryComparisonOptions), value);
        }

        /// <summary>
        /// List of enum types.
        /// </summary>
        public List<EnumType> EnumTypes
        {
            get => Get<List<EnumType>>(nameof(EnumTypes));
            set => Set(nameof(EnumTypes), value);
        }

        /// <summary>
        /// 
        /// </summary>
        public List<DataService> DataServices
        {
            get => Get<List<DataService>>(nameof(DataServices));
            set => Set(nameof(DataServices), value);
        }

        /// <summary>
        /// Generates a json of the current object.
        /// </summary>
        /// <param name="formatting">The json formatting.</param>
        /// <returns>The generated json.</returns>
        public string ToJson(Formatting formatting = Formatting.Indented)
        {
            return JsonConvert.SerializeObject(this, formatting, JsonSerializerSettings);
        }
    }
}
