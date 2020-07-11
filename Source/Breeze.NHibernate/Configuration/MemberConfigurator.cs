using System;

namespace Breeze.NHibernate.Configuration
{
    /// <inheritdoc />
    internal class MemberConfigurator : IMemberConfigurator
    {
        public MemberConfigurator(MemberConfiguration configuration)
        {
            Configuration = configuration;
        }

        internal MemberConfiguration Configuration { get; }

        /// <inheritdoc />
        public IMemberConfigurator Ignore()
        {
            Configuration.Ignored = true;
            return this;
        }

        /// <inheritdoc />
        public IMemberConfigurator Include()
        {
            Configuration.Ignored = false;
            return this;
        }

        /// <inheritdoc />
        public IMemberConfigurator IsNullable(bool value)
        {
            Configuration.IsNullable = value;
            return this;
        }

        /// <inheritdoc />
        public IMemberConfigurator MaxLength(int? value)
        {
            Configuration.MaxLength = value;
            return this;
        }

        /// <inheritdoc />
        public IMemberConfigurator Custom(object value)
        {
            Configuration.Custom = value;
            return this;
        }

        /// <inheritdoc />
        public IMemberConfigurator Serialize(bool value)
        {
            Configuration.Serialize = value;
            return this;
        }

        /// <inheritdoc />
        public IMemberConfigurator Deserialize(bool value)
        {
            Configuration.Deserialize = value;
            return this;
        }

        /// <inheritdoc />
        public IMemberConfigurator DefaultValue(object value)
        {
            Configuration.DefaultValue = value;
            Configuration.HasDefaultValue = true;
            return this;
        }

        /// <inheritdoc />
        public IMemberConfigurator Serialize(SerializeMemberDelegate serializeFunction)
        {
            Configuration.SerializeFunction = serializeFunction;
            return this;
        }

        /// <inheritdoc />
        public IMemberConfigurator Deserialize(DeserializeMemberDelegate deserializeFunction)
        {
            Configuration.DeserializeFunction = deserializeFunction;
            return this;
        }

        /// <inheritdoc />
        public IMemberConfigurator ShouldSerialize(Predicate<object> predicate)
        {
            Configuration.ShouldSerializePredicate = predicate;
            return this;
        }

        /// <inheritdoc />
        public IMemberConfigurator ShouldDeserialize(Predicate<object> predicate)
        {
            Configuration.ShouldDeserializePredicate = predicate;
            return this;
        }
    }

    /// <inheritdoc />
    internal class MemberConfigurator<TModel, TType> : IMemberConfigurator<TModel, TType>
    {
        public MemberConfigurator(MemberConfiguration configuration)
        {
            Configuration = configuration;
        }

        internal MemberConfiguration Configuration { get; }

        /// <inheritdoc />
        public IMemberConfigurator<TModel, TType> Ignore()
        {
            Configuration.Ignored = true;
            return this;
        }

        /// <inheritdoc />
        public IMemberConfigurator<TModel, TType> Include()
        {
            Configuration.Ignored = false;
            return this;
        }

        /// <inheritdoc />
        public IMemberConfigurator<TModel, TType> IsNullable(bool value)
        {
            Configuration.IsNullable = value;
            return this;
        }

        /// <inheritdoc />
        public IMemberConfigurator<TModel, TType> MaxLength(int? value)
        {
            Configuration.MaxLength = value;
            return this;
        }

        /// <inheritdoc />
        public IMemberConfigurator<TModel, TType> Custom(object value)
        {
            Configuration.Custom = value;
            return this;
        }

        /// <inheritdoc />
        public IMemberConfigurator<TModel, TType> Serialize(bool value)
        {
            Configuration.Serialize = value;
            return this;
        }

        /// <inheritdoc />
        public IMemberConfigurator<TModel, TType> Deserialize(bool value)
        {
            Configuration.Deserialize = value;
            return this;
        }

        /// <inheritdoc />
        public IMemberConfigurator<TModel, TType> DefaultValue(TType value)
        {
            Configuration.DefaultValue = value;
            Configuration.HasDefaultValue = true;
            return this;
        }

        /// <inheritdoc />
        public IMemberConfigurator<TModel, TType> Serialize(Func<TModel, TType> serializeFunction = null)
        {
            Configuration.SerializeFunction = serializeFunction != null
                ? (modelVal, memberInfo) => serializeFunction((TModel) modelVal)
                : (SerializeMemberDelegate) null;

            return this;
        }

        /// <inheritdoc />
        public IMemberConfigurator<TModel, TType> Serialize<TResult>(Func<TModel, TResult> serializeFunction = null)
        {
            Configuration.SerializeFunction = serializeFunction != null
                ? (modelVal, memberInfo) => serializeFunction((TModel) modelVal)
                : (SerializeMemberDelegate) null;

            return this;
        }

        /// <inheritdoc />
        public IMemberConfigurator<TModel, TType> Deserialize(Func<TType, TType> deserializeFunction = null)
        {
            Configuration.DeserializeFunction = deserializeFunction != null
                ? (value, memberInfo) => deserializeFunction((TType) value)
                : (DeserializeMemberDelegate) null;

            return this;
        }

        /// <inheritdoc />
        public IMemberConfigurator<TModel, TType> ShouldSerialize(Predicate<TModel> predicate)
        {
            Configuration.ShouldSerializePredicate = predicate != null
                ? model => predicate((TModel) model)
                : (Predicate<object>) null;

            return this;
        }

        /// <inheritdoc />
        public IMemberConfigurator<TModel, TType> ShouldDeserialize(Predicate<TModel> predicate)
        {
            Configuration.ShouldDeserializePredicate = predicate != null
                ? model => predicate((TModel) model)
                : (Predicate<object>) null;

            return this;
        }
    }
}
