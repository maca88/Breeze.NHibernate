using System;
using Breeze.NHibernate.Metadata;
using Breeze.NHibernate.Serialization;

namespace Breeze.NHibernate.Configuration
{
    /// <summary>
    /// A delegate used to serialize the synthetic member when serializing using <see cref="BreezeContractResolver"/>.
    /// </summary>
    /// <param name="model">The model that is being serialized.</param>
    /// <param name="memberName">The synthetic member name to serialize.</param>
    /// <returns>The synthetic member value that will be used in the serialization.</returns>
    public delegate object SerializeSyntheticMemberDelegate(object model, string memberName);

    /// <summary>
    /// The synthetic member configuration used by <see cref="BreezeMetadataBuilder"/> and <see cref="BreezeContractResolver"/>.
    /// </summary>
    public class SyntheticMemberConfiguration
    {
        internal SyntheticMemberConfiguration(
            string memberName,
            Type memberType,
            Type declaringType)
            : this(memberName, memberType, declaringType, null, false)
        {
        }

        internal SyntheticMemberConfiguration(
            string memberName,
            Type memberType,
            Type declaringType,
            SerializeSyntheticMemberDelegate serializeFunction)
            : this(memberName, memberType, declaringType, serializeFunction, true)
        {
        }

        internal SyntheticMemberConfiguration(
            string memberName,
            Type memberType,
            Type declaringType,
            SerializeSyntheticMemberDelegate serializeFunction,
            bool added)
        {
            MemberName = memberName;
            MemberType = memberType;
            DeclaringType = declaringType;
            SerializeFunction = serializeFunction;
            Added = added;
        }

        /// <summary>
        /// Whether it is an additional synthetic property, not related to a foreign key synthetic property.
        /// </summary>
        public bool Added { get; private set; }

        /// <summary>
        /// The member name.
        /// </summary>
        public string MemberName { get; }

        /// <summary>
        /// The member type.
        /// </summary>
        public Type MemberType { get; }

        /// <summary>
        /// The member declaring type.
        /// </summary>
        public Type DeclaringType { get; }

        /// <summary>
        /// Whether to ignore the member when building <see cref="BreezeMetadata"/> and serializing when using <see cref="BreezeContractResolver"/>.
        /// </summary>
        public bool? Ignored { get; set; }

        /// <summary>
        /// The default value that will be used when building <see cref="BreezeMetadata"/> and serializing when using <see cref="BreezeContractResolver"/>.
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// Whether the default value has been set.
        /// </summary>
        public bool HasDefaultValue { get; set; }

        /// <summary>
        /// Whether the member is nullable. When set, it will override the default value set by <see cref="BreezeMetadataBuilder"/>
        /// when building <see cref="BreezeMetadata"/>.
        /// </summary>
        public bool? IsNullable { get; set; }

        /// <summary>
        /// The maximum string length allowed, which will be used by <see cref="BreezeMetadataBuilder"/>
        /// when building <see cref="BreezeMetadata"/>.
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// The custom data, which will be used by <see cref="BreezeMetadataBuilder"/>
        /// when building <see cref="BreezeMetadata"/>.
        /// </summary>
        public object Custom { get; set; }

        #region Serialization properties

        /// <summary>
        /// Whether the member should be serialized when using <see cref="BreezeContractResolver"/>.
        /// </summary>
        public bool? Serialize { get; set; }

        /// <summary>
        /// The serialization function called when serializing the synthetic member using <see cref="BreezeContractResolver"/>.
        /// </summary>
        public SerializeSyntheticMemberDelegate SerializeFunction { get; set; }

        /// <summary>
        /// A predicate that controls whether the synthetic member should be serialized when using <see cref="BreezeContractResolver"/>.
        /// </summary>
        public Predicate<object> ShouldSerializePredicate { get; set; }

        #endregion

        internal void MergeWith(SyntheticMemberConfiguration member)
        {
            Added |= member.Added;
            DefaultValue = member.DefaultValue ?? DefaultValue;
            HasDefaultValue = member.HasDefaultValue || HasDefaultValue;
            SerializeFunction = member.SerializeFunction ?? SerializeFunction;
            ShouldSerializePredicate = member.ShouldSerializePredicate ?? ShouldSerializePredicate;
            Ignored = member.Ignored ?? Ignored;
            Serialize = member.Serialize ?? Serialize;
            MaxLength = member.MaxLength ?? MaxLength;
            IsNullable = member.IsNullable ?? IsNullable;
            Custom = member.Custom ?? Custom;
        }
    }
}
