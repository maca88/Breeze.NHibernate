using System;

namespace Breeze.NHibernate.Configuration
{
    /// <inheritdoc />
    internal class SyntheticMemberConfigurator : ISyntheticMemberConfigurator
    {
        public SyntheticMemberConfigurator(SyntheticMemberConfiguration configuration)
        {
            Configuration = configuration;
        }

        internal SyntheticMemberConfiguration Configuration { get; }

        /// <inheritdoc />
        public ISyntheticMemberConfigurator Ignore()
        {
            Configuration.Ignored = true;
            return this;
        }

        /// <inheritdoc />
        public ISyntheticMemberConfigurator Include()
        {
            Configuration.Ignored = false;
            return this;
        }

        /// <inheritdoc />
        public ISyntheticMemberConfigurator IsNullable(bool value)
        {
            Configuration.IsNullable = value;
            return this;
        }

        /// <inheritdoc />
        public ISyntheticMemberConfigurator MaxLength(int? value)
        {
            Configuration.MaxLength = value;
            return this;
        }

        /// <inheritdoc />
        public ISyntheticMemberConfigurator Custom(object value)
        {
            Configuration.Custom = value;
            return this;
        }

        /// <inheritdoc />
        public ISyntheticMemberConfigurator Serialize(bool value)
        {
            Configuration.Serialize = value;
            return this;
        }

        /// <inheritdoc />
        public ISyntheticMemberConfigurator DefaultValue(object value)
        {
            Configuration.DefaultValue = value;
            Configuration.HasDefaultValue = true;
            return this;
        }

        /// <inheritdoc />
        public ISyntheticMemberConfigurator Serialize(SerializeSyntheticMemberDelegate serializeFunction)
        {
            Configuration.SerializeFunction = serializeFunction;
            return this;
        }

        /// <inheritdoc />
        public ISyntheticMemberConfigurator ShouldSerialize(Predicate<object> predicate)
        {
            Configuration.ShouldSerializePredicate = predicate;
            return this;
        }
    }

    internal class SyntheticMemberConfigurator<TModel, TType> : ISyntheticMemberConfigurator<TModel, TType>
    {
        public SyntheticMemberConfigurator(SyntheticMemberConfiguration configuration)
        {
            Configuration = configuration;
        }

        internal SyntheticMemberConfiguration Configuration { get; }

        /// <inheritdoc />
        public ISyntheticMemberConfigurator<TModel, TType> Ignore()
        {
            Configuration.Ignored = true;
            return this;
        }

        /// <inheritdoc />
        public ISyntheticMemberConfigurator<TModel, TType> Include()
        {
            Configuration.Ignored = false;
            return this;
        }

        /// <inheritdoc />
        public ISyntheticMemberConfigurator<TModel, TType> IsNullable(bool value)
        {
            Configuration.IsNullable = value;
            return this;
        }

        /// <inheritdoc />
        public ISyntheticMemberConfigurator<TModel, TType> MaxLength(int? value)
        {
            Configuration.MaxLength = value;
            return this;
        }

        /// <inheritdoc />
        public ISyntheticMemberConfigurator<TModel, TType> Custom(object value)
        {
            Configuration.Custom = value;
            return this;
        }

        /// <inheritdoc />
        public ISyntheticMemberConfigurator<TModel, TType> Serialize(bool value)
        {
            Configuration.Serialize = value;
            return this;
        }

        /// <inheritdoc />
        public ISyntheticMemberConfigurator<TModel, TType> DefaultValue(TType value)
        {
            Configuration.DefaultValue = value;
            Configuration.HasDefaultValue = true;
            return this;
        }

        /// <inheritdoc />
        public ISyntheticMemberConfigurator<TModel, TType> Serialize(Func<TModel, TType> serializeFunction = null)
        {
            Configuration.SerializeFunction = serializeFunction != null
                ? (modelVal, memberName) => serializeFunction((TModel) modelVal)
                : (SerializeSyntheticMemberDelegate) null;

            return this;
        }

        /// <inheritdoc />
        public ISyntheticMemberConfigurator<TModel, TType> Serialize<TResult>(Func<TModel, TResult> serializeFunction = null)
        {
            Configuration.SerializeFunction = serializeFunction != null
                ? (modelVal, memberName) => serializeFunction((TModel) modelVal)
                : (SerializeSyntheticMemberDelegate) null;

            return this;
        }

        /// <inheritdoc />
        public ISyntheticMemberConfigurator<TModel, TType> ShouldSerialize(Predicate<TModel> predicate)
        {
            Configuration.ShouldSerializePredicate = predicate != null
                ? model => predicate((TModel)model)
                : (Predicate<object>)null;

            return this;
        }
    }
}
