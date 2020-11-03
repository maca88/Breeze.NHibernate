using System;
using System.Collections.Generic;

namespace Breeze.NHibernate.Metadata
{
    /// <summary>
    /// A structural type.
    /// </summary>
    public class StructuralType : MetadataObject
    {
        /// <summary>
        /// Constructs an instance of <see cref="StructuralType"/>.
        /// </summary>
        public StructuralType(Type type)
        {
            Type = type;
            ShortName = type.Name;
            Namespace = type.Namespace;
            DataProperties = new List<DataProperty>();
        }

        /// <summary>
        /// The structural type type.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Together the shortName and the namespace make up a fully qualified name. Within this metadata references to an entityType are all qualified references.
        /// </summary>
        public string ShortName
        {
            get => Get<string>(nameof(ShortName));
            set => Set(nameof(ShortName), value);
        }

        /// <summary>
        /// The type namespace.
        /// </summary>
        public string Namespace
        {
            get => Get<string>(nameof(Namespace));
            set => Set(nameof(Namespace), value);
        }

        /// <summary>
        /// A list of data properties.
        /// </summary>
        public List<DataProperty> DataProperties
        {
            get => Get<List<DataProperty>>(nameof(DataProperties));
            set => Set(nameof(DataProperties), value);
        }

        /// <summary>
        /// A list of the validators (validations) that will be associated with this structure
        /// </summary>
        public List<Validator> Validators
        {
            get => GetOrCreate(nameof(Validators), () => new List<Validator>());
            set => Set(nameof(Validators), value);
        }

        /// <summary>
        /// Custom data that will be included in the type metadata.
        /// </summary>
        public object Custom
        {
            get => Get<object>(nameof(Custom));
            set => Set(nameof(Custom), value);
        }

        /// <summary>
        /// Whether the type is abstract.
        /// </summary>
        public bool IsAbstract
        {
            get => Get<bool>(nameof(IsAbstract));
            set => Set(nameof(IsAbstract), value);
        }

        /// <summary>
        /// Whether the type is unmapped.
        /// </summary>
        public bool IsUnmapped
        {
            get => Get<bool>(nameof(IsUnmapped));
            set => Set(nameof(IsUnmapped), value);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{ShortName}:#{Namespace}";
        }
    }
}
