

namespace Breeze.NHibernate.Metadata
{
    public class DataProperty : BaseProperty
    {
        private static readonly object Object = new object();

        /// <summary>
        /// If present, the complexType name should be omitted.
        /// </summary>
        public DataType? DataType
        {
            get => Get<DataType?>(nameof(DataType));
            set => Set(nameof(DataType), value);
        }

        /// <summary>
        /// If present, this must be the fully qualified name of one of the 'complexTypes' defined within this document, and the 'dataType' property may be omitted
        /// </summary>
        public string ComplexTypeName
        {
            get => Get<string>(nameof(ComplexTypeName));
            set => Set(nameof(ComplexTypeName), value);
        }

        /// <summary>
        /// Whether a null can be assigned to this property.
        /// </summary>
        public bool IsNullable
        {
            get => Get(nameof(IsNullable), true);
            set => Set(nameof(IsNullable), value, true);
        }

        /// <summary>
        /// The default value for this property if nothing is assigned to it.
        /// </summary>
        public object DefaultValue
        {
            get => Get<object>(nameof(DefaultValue));
            set => Set(nameof(DefaultValue), value, Object);
        }

        /// <summary>
        /// Whether this property is part of the key for this entity type.
        /// </summary>
        public bool IsPartOfKey
        {
            get => Get<bool>(nameof(IsPartOfKey));
            set => Set(nameof(IsPartOfKey), value);
        }

        /// <summary>
        /// This determines whether this property is used for concurrency purposes.
        /// </summary>
        public ConcurrencyMode? ConcurrencyMode
        {
            get => Get<ConcurrencyMode?>(nameof(ConcurrencyMode));
            set => Set(nameof(ConcurrencyMode), value);
        }

        /// <summary>
        /// Only applicable to 'String' properties. This is the maximum string length allowed.
        /// </summary>
        public int? MaxLength
        {
            get => Get<int?>(nameof(MaxLength));
            set => Set(nameof(MaxLength), value);
        }

        public bool IsSettable
        {
            get => Get<bool>(nameof(IsSettable));
            set => Set(nameof(IsSettable), value);
        }

        public bool IsUnmapped
        {
            get => Get<bool>(nameof(IsUnmapped));
            set => Set(nameof(IsUnmapped), value);
        }
    }
}
