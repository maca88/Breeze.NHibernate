using System;
using System.Reflection;
using Breeze.NHibernate.Extensions;
using Breeze.NHibernate.Metadata;
using Breeze.NHibernate.Serialization;

namespace Breeze.NHibernate.Configuration
{
    /// <summary>
    /// A delegate used to manipulate the member value when serializing using <see cref="BreezeContractResolver"/>.
    /// </summary>
    /// <param name="model">The model that is being serialized.</param>
    /// <param name="member">The member to serialize.</param>
    /// <returns>The member value that will be used in the serialization.</returns>
    public delegate object SerializeMemberDelegate(object model, MemberInfo member);

    /// <summary>
    /// A delegate used to manipulate the member value when deserializing using <see cref="BreezeContractResolver"/>.
    /// </summary>
    /// <param name="value">The retrieved member value on deserialization.</param>
    /// <param name="member">The member to serialize.</param>
    /// <returns>The value that be set for the given member.</returns>
    public delegate object DeserializeMemberDelegate(object value, MemberInfo member);

    /// <summary>
    /// The member configuration used by <see cref="BreezeMetadataBuilder"/> and <see cref="BreezeContractResolver"/>.
    /// </summary>
    public class MemberConfiguration
    {
        internal MemberConfiguration(MemberInfo memberInfo)
        {
            MemberInfo = memberInfo ?? throw new ArgumentNullException(nameof(memberInfo));
            MemberType = memberInfo.GetUnderlyingType();
            DeclaringType = memberInfo.DeclaringType;
        }

        /// <summary>
        /// The member info of the member.
        /// </summary>
        public MemberInfo MemberInfo { get; }

        /// <summary>
        /// The member name.
        /// </summary>
        public string MemberName => MemberInfo.Name;

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
        public bool? Ignored { get; internal set; }

        /// <summary>
        /// The default value that will be used when building <see cref="BreezeMetadata"/>.
        /// </summary>
        public object DefaultValue { get; internal set; }

        /// <summary>
        /// Whether the default value has been set.
        /// </summary>
        public bool HasDefaultValue { get; internal set; }

        /// <summary>
        /// Whether the member is nullable. When set, it will override the default value set by <see cref="BreezeMetadataBuilder"/>
        /// when building <see cref="BreezeMetadata"/>.
        /// </summary>
        public bool? IsNullable { get; internal set; }

        /// <summary>
        /// The maximum string length allowed, which will be used by <see cref="BreezeMetadataBuilder"/>.
        /// when building <see cref="BreezeMetadata"/>.
        /// </summary>
        public int? MaxLength { get; internal set; }

        /// <summary>
        /// Custom data that will be used when building <see cref="BreezeMetadata"/>.
        /// </summary>
        public object Custom { get; internal set; }

        #region Serialization properties

        /// <summary>
        /// Whether the member should be serialized when using <see cref="BreezeContractResolver"/>.
        /// </summary>
        public bool? Serialize { get; internal set; }

        /// <summary>
        /// Whether the member should be deserialized when using <see cref="BreezeContractResolver"/>.
        /// </summary>
        public bool? Deserialize { get; internal set; }

        /// <summary>
        /// A predicate that controls whether the member should be serialized when using <see cref="BreezeContractResolver"/>.
        /// </summary>
        public Predicate<object> ShouldSerializePredicate { get; internal set; }

        /// <summary>
        /// A predicate that controls whether the member should be deserialized when using <see cref="BreezeContractResolver"/>.
        /// </summary>
        public Predicate<object> ShouldDeserializePredicate { get; internal set; }

        /// <summary>
        /// A custom serialization function called when serializing member using <see cref="BreezeContractResolver"/>.
        /// </summary>
        public SerializeMemberDelegate SerializeFunction { get; internal set; }

        /// <summary>
        /// A custom deserialization function called when deserializing member using <see cref="BreezeContractResolver"/>.
        /// </summary>
        public DeserializeMemberDelegate DeserializeFunction { get; internal set; }

        #endregion

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return MemberInfo.GetHashCode();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (!(obj is MemberConfiguration other))
            {
                return false;
            }

            return ReferenceEquals(other.MemberInfo, MemberInfo);
        }

        internal void MergeWith(MemberConfiguration member)
        {
            DefaultValue = member.DefaultValue ?? DefaultValue;
            HasDefaultValue = member.HasDefaultValue || HasDefaultValue;
            SerializeFunction = member.SerializeFunction ?? SerializeFunction;
            ShouldSerializePredicate = member.ShouldSerializePredicate ?? ShouldSerializePredicate;
            ShouldDeserializePredicate = member.ShouldDeserializePredicate ?? ShouldDeserializePredicate;
            Ignored = member.Ignored ?? Ignored;
            Deserialize = member.Deserialize ?? Deserialize;
            Serialize = member.Serialize ?? Serialize;
            MaxLength = member.MaxLength ?? MaxLength;
            IsNullable = member.IsNullable ?? IsNullable;
            Custom = member.Custom ?? Custom;
        }
    }
}
