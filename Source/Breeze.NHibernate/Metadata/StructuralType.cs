using System;
using System.Collections.Generic;

namespace Breeze.NHibernate.Metadata
{
    public class StructuralType : MetadataObject
    {
        public StructuralType(Type type)
        {
            Type = type;
            ShortName = type.Name;
            Namespace = type.Namespace;
        }

        public Type Type { get; }

        /// <summary>
        /// Together the shortName and the namespace make up a fully qualified name. Within this metadata references to an entityType are all qualified references.
        /// </summary>
        public string ShortName
        {
            get => Get<string>(nameof(ShortName));
            set => Set(nameof(ShortName), value);
        }

        public string Namespace
        {
            get => Get<string>(nameof(Namespace));
            set => Set(nameof(Namespace), value);
        }

        public List<DataProperty> DataProperties
        {
            get => GetOrCreate(nameof(DataProperties), () => new List<DataProperty>());
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

        public object Custom
        {
            get => Get<object>(nameof(Custom));
            set => Set(nameof(Custom), value);
        }

        public bool IsAbstract
        {
            get => Get<bool>(nameof(IsAbstract));
            set => Set(nameof(IsAbstract), value);
        }

        public bool IsUnmapped
        {
            get => Get<bool>(nameof(IsUnmapped));
            set => Set(nameof(IsUnmapped), value);
        }

        public override string ToString()
        {
            return $"{ShortName}:#{Namespace}";
        }
    }
}
