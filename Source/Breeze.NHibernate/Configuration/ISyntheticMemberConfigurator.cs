using System;

namespace Breeze.NHibernate.Configuration
{
    /// <summary>
    /// A fluent configurator used to configure the <see cref="SyntheticMemberConfiguration"/>.
    /// </summary>
    public interface ISyntheticMemberConfigurator
    {
        /// <summary>
        /// Ignores the member from the metadata and serialization.
        /// </summary>
        ISyntheticMemberConfigurator Ignore();

        /// <summary>
        /// Includes the member to the metadata and serialization.
        /// </summary>
        ISyntheticMemberConfigurator Include();

        /// <summary>
        /// Set in the metadata whether the member is nullable.
        /// </summary>
        ISyntheticMemberConfigurator IsNullable(bool value);

        /// <summary>
        /// Set the maximum string length allowed, which will be used by <see cref="BreezeMetadataBuilder"/>.
        /// </summary>
        ISyntheticMemberConfigurator MaxLength(int? value);

        /// <summary>
        /// Set custom data, which will be used by <see cref="BreezeMetadataBuilder"/>.
        /// </summary>
        ISyntheticMemberConfigurator Custom(object value);

        /// <summary>
        /// Set whether the member should be serialized.
        /// </summary>
        ISyntheticMemberConfigurator Serialize(bool value);

        /// <summary>
        /// Set a delegate that will be used to serialize the member.
        /// </summary>
        /// <param name="serializeFunction">The serialization delegate.</param>
        ISyntheticMemberConfigurator Serialize(SerializeSyntheticMemberDelegate serializeFunction);

        /// <summary>
        /// Set the default value that will be put in the metadata.
        /// </summary>
        ISyntheticMemberConfigurator DefaultValue(object value);

        /// <summary>
        /// Set whether the member should be serialized by using a predicate.
        /// </summary>
        /// <param name="predicate">The predicate that returns whether the member should be serialized.</param>
        ISyntheticMemberConfigurator ShouldSerialize(Predicate<object> predicate);
    }

    /// <summary>
    /// A fluent configurator used to configure the <see cref="SyntheticMemberConfiguration"/>.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <typeparam name="TType">The synthetic member type.</typeparam>
    public interface ISyntheticMemberConfigurator<TModel, TType>
    {
        /// <summary>
        /// Ignores the member from the metadata and serialization.
        /// </summary>
        ISyntheticMemberConfigurator<TModel, TType> Ignore();

        /// <summary>
        /// Includes the member to the metadata and serialization.
        /// </summary>
        ISyntheticMemberConfigurator<TModel, TType> Include();

        /// <summary>
        /// Set in the metadata whether the member is nullable.
        /// </summary>
        ISyntheticMemberConfigurator<TModel, TType> IsNullable(bool value);

        /// <summary>
        /// Set the maximum string length allowed, which will be used by <see cref="BreezeMetadataBuilder"/>.
        /// </summary>
        ISyntheticMemberConfigurator<TModel, TType> MaxLength(int? value);

        /// <summary>
        /// Set custom data, which will be used by <see cref="BreezeMetadataBuilder"/>.
        /// </summary>
        ISyntheticMemberConfigurator<TModel, TType> Custom(object value);

        /// <summary>
        /// Set whether the member should be serialized.
        /// </summary>
        ISyntheticMemberConfigurator<TModel, TType> Serialize(bool value);

        /// <summary>
        /// Set a delegate that will be used to serialize the member.
        /// </summary>
        /// <param name="serializeFunction">The serialization delegate.</param>
        ISyntheticMemberConfigurator<TModel, TType> Serialize(Func<TModel, TType> serializeFunction);

        /// <summary>
        /// Set a delegate that will be used to serialize the member.
        /// </summary>
        /// <typeparam name="TResult">The result type</typeparam>
        /// <param name="serializeFunction">The serialization delegate.</param>
        /// <returns></returns>
        ISyntheticMemberConfigurator<TModel, TType> Serialize<TResult>(Func<TModel, TResult> serializeFunction);

        /// <summary>
        /// Set the default value that will be put in the metadata.
        /// </summary>
        ISyntheticMemberConfigurator<TModel, TType> DefaultValue(TType value);

        /// <summary>
        /// Set whether the member should be serialized by using a predicate.
        /// </summary>
        /// <param name="predicate">The predicate that returns whether the member should be serialized.</param>
        ISyntheticMemberConfigurator<TModel, TType> ShouldSerialize(Predicate<TModel> predicate);
    }
}
