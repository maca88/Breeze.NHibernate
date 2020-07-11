using System;

namespace Breeze.NHibernate.Configuration
{
    /// <summary>
    /// A fluent configurator used to configure the <see cref="MemberConfiguration"/>.
    /// </summary>
    public interface IMemberConfigurator
    {
        /// <summary>
        /// Ignores the member from the metadata and serialization.
        /// </summary>
        IMemberConfigurator Ignore();

        /// <summary>
        /// Includes the member to the metadata and serialization.
        /// </summary>
        IMemberConfigurator Include();

        /// <summary>
        /// Set in the metadata whether the member is nullable.
        /// </summary>
        IMemberConfigurator IsNullable(bool value);

        /// <summary>
        /// Set the maximum string length allowed, which will be used by <see cref="BreezeMetadataBuilder"/>.
        /// </summary>
        IMemberConfigurator MaxLength(int? value);

        /// <summary>
        /// Set custom data, which will be used by <see cref="BreezeMetadataBuilder"/>.
        /// </summary>
        IMemberConfigurator Custom(object value);

        /// <summary>
        /// Set whether the member should be serialized.
        /// </summary>
        IMemberConfigurator Serialize(bool value);

        /// <summary>
        /// Set a delegate that will be used to serialize the member.
        /// </summary>
        /// <param name="serializeFunction">The serialization delegate.</param>
        IMemberConfigurator Serialize(SerializeMemberDelegate serializeFunction);

        /// <summary>
        /// Set whether the member should be deserialized.
        /// </summary>
        IMemberConfigurator Deserialize(bool value);

        /// <summary>
        /// Set a delegate that will be used to deserialize the member.
        /// </summary>
        /// <param name="deserializeFunction">The deserialization delegate.</param>
        IMemberConfigurator Deserialize(DeserializeMemberDelegate deserializeFunction);

        /// <summary>
        /// Set the default value that will be put in the metadata.
        /// </summary>
        IMemberConfigurator DefaultValue(object value);

        /// <summary>
        /// Set whether the member should be serialized by using a predicate.
        /// </summary>
        /// <param name="predicate">The predicate that returns whether the member should be serialized.</param>
        IMemberConfigurator ShouldSerialize(Predicate<object> predicate);

        /// <summary>
        /// Set whether the member should be deserialized by using a predicate.
        /// </summary>
        /// <param name="predicate">The predicate that returns whether the member should be deserialized.</param>
        IMemberConfigurator ShouldDeserialize(Predicate<object> predicate);
    }

    /// <summary>
    /// A fluent configurator used to configure the <see cref="MemberConfiguration"/>.
    /// </summary>
    /// <typeparam name="TModel">The model type</typeparam>
    /// <typeparam name="TType">The member type.</typeparam>
    public interface IMemberConfigurator<TModel, TType>
    {
        /// <summary>
        /// Ignores the member from the metadata and serialization.
        /// </summary>
        IMemberConfigurator<TModel, TType> Ignore();

        /// <summary>
        /// Includes the member to the metadata and serialization.
        /// </summary>
        IMemberConfigurator<TModel, TType> Include();

        /// <summary>
        /// Set in the metadata whether the member is nullable.
        /// </summary>
        IMemberConfigurator<TModel, TType> IsNullable(bool value);

        /// <summary>
        /// Set the maximum string length allowed, which will be used by <see cref="BreezeMetadataBuilder"/>.
        /// </summary>
        IMemberConfigurator<TModel, TType> MaxLength(int? value);

        /// <summary>
        /// Set custom data, which will be used by <see cref="BreezeMetadataBuilder"/>.
        /// </summary>
        IMemberConfigurator<TModel, TType> Custom(object value);

        /// <summary>
        /// Set whether the member should be serialized.
        /// </summary>
        IMemberConfigurator<TModel, TType> Serialize(bool value);

        /// <summary>
        /// Set a delegate that will be used to serialize the member.
        /// </summary>
        /// <param name="serializeFunction">The serialization delegate.</param>
        IMemberConfigurator<TModel, TType> Serialize(Func<TModel, TType> serializeFunction);

        /// <summary>
        /// Set a delegate that will be used to serialize the member.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="serializeFunction">The serialization delegate.</param>
        IMemberConfigurator<TModel, TType> Serialize<TResult>(Func<TModel, TResult> serializeFunction);

        /// <summary>
        /// Set whether the member should be deserialized.
        /// </summary>
        IMemberConfigurator<TModel, TType> Deserialize(bool value);

        /// <summary>
        /// Set a delegate that will be used to deserialize the member.
        /// </summary>
        /// <param name="deserializeFunction">The deserialization delegate.</param>
        IMemberConfigurator<TModel, TType> Deserialize(Func<TType, TType> deserializeFunction);

        /// <summary>
        /// Set the default value that will be put in the metadata.
        /// </summary>
        IMemberConfigurator<TModel, TType> DefaultValue(TType value);

        /// <summary>
        /// Set whether the member should be serialized by using a predicate.
        /// </summary>
        /// <param name="predicate">The predicate that returns whether the member should be serialized.</param>
        IMemberConfigurator<TModel, TType> ShouldSerialize(Predicate<TModel> predicate);

        /// <summary>
        /// Set whether the member should be deserialized by using a predicate.
        /// </summary>
        /// <param name="predicate">The predicate that returns whether the member should be deserialized.</param>
        IMemberConfigurator<TModel, TType> ShouldDeserialize(Predicate<TModel> predicate);
    }
}
