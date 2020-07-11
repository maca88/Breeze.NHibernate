using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Breeze.NHibernate.Configuration;
using Breeze.NHibernate.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NHibernate;
using NHibernate.Intercept;
using NHibernate.Proxy;
using NHibernate.Type;

namespace Breeze.NHibernate.Serialization
{
    /// <summary>
    /// A configurable contract resolver that is able to serialize NHibernate entities.
    /// </summary>
    public class BreezeContractResolver : DefaultContractResolver
    {
        private readonly IBreezeConfigurator _breezeConfigurator;
        private readonly IEntityMetadataProvider _entityMetadataProvider;
        private readonly IClientModelMetadataProvider _clientModelMetadataProvider;
        private readonly ILazyLoadGuardProvider _lazyLoadGuardProvider;

        public BreezeContractResolver(
            IBreezeConfigurator breezeConfigurator,
            IEntityMetadataProvider entityMetadataProvider,
            IClientModelMetadataProvider clientModelMetadataProvider,
            ILazyLoadGuardProvider lazyLoadGuardProvider)
        {
            _breezeConfigurator = breezeConfigurator;
            _entityMetadataProvider = entityMetadataProvider;
            _clientModelMetadataProvider = clientModelMetadataProvider;
            _lazyLoadGuardProvider = lazyLoadGuardProvider;
        }

        /// <summary>
        /// Creates a <see cref="BreezeContractResolver"/> with the given naming strategy.
        /// </summary>
        /// <param name="namingStrategy">The naming strategy to use.</param>
        /// <returns>A new contract resolver.</returns>
        public BreezeContractResolver CreateWithNamingStrategy(NamingStrategy namingStrategy)
        {
            return new BreezeContractResolver(
                _breezeConfigurator,
                _entityMetadataProvider,
                _clientModelMetadataProvider,
                _lazyLoadGuardProvider)
            {
                NamingStrategy = namingStrategy
            };
        }

        /// <inheritdoc />
        public override JsonContract ResolveContract(Type type)
        {
            // Proxies for entity types that have one or more lazy fields/properties will implements IFieldInterceptorAccessor.
            if (typeof(IFieldInterceptorAccessor).IsAssignableFrom(type))
            {
                type = type.BaseType;
            }

            return base.ResolveContract(type);
        }

        protected override JsonContract CreateContract(Type objectType)
        {
            if (!typeof(INHibernateProxy).IsAssignableFrom(objectType))
            {
                return base.CreateContract(objectType);
            }

            // For proxies add a special converter that will unwrap the underlying entity when the proxy is initialized
            // or null will be serialized otherwise
            var contract = CreateObjectContract(objectType);
            contract.Converter = NHibernateProxyJsonConverter.Instance;
            var baseContract = (JsonObjectContract)base.ResolveContract(objectType.BaseType);
            foreach (var property in baseContract.Properties)
            {
                contract.Properties.Add(property);
            }

            return contract;
        }

        /// <inheritdoc />
        protected sealed override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            return _breezeConfigurator.GetModelConfiguration(objectType).Members.Values.Select(o => o.MemberInfo).ToList();
        }

        /// <inheritdoc />
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            if (typeof(INHibernateProxy).IsAssignableFrom(type))
            {
                return new List<JsonProperty>(); // They will be copied from the base type
            }

            var members = GetSerializableMembers(type);
            if (members == null)
            {
                throw new JsonSerializationException("Null collection of serializable members returned.");
            }

            var propertyMembers = new Dictionary<JsonProperty, MemberInfo>(members.Count);
            var modelConfiguration = _breezeConfigurator.GetModelConfiguration(type);
            var metadata = GetTypeMetadata(type);
            var properties = new JsonPropertyCollection(type);
            foreach (var member in members)
            {
                var property = CreateProperty(member, memberSerialization, metadata);
                propertyMembers.Add(property, member);
                properties.AddProperty(property);
            }

            foreach (var syntheticMember in modelConfiguration.SyntheticMembers.Values.Where(o => o.Added))
            {
                properties.AddProperty(CreateSyntheticProperty(syntheticMember));
            }

            if (metadata != null)
            {
                foreach (var syntheticProperty in metadata.SyntheticForeignKeyProperties.Values)
                {
                    properties.AddProperty(CreateSyntheticForeignKeyProperty(
                        type,
                        syntheticProperty,
                        modelConfiguration.GetSyntheticMember(syntheticProperty.Name),
                        modelConfiguration.GetMember(syntheticProperty.AssociationPropertyName)));
                }
            }

            ApplyModelConfiguration(type, properties, modelConfiguration, metadata, propertyMembers);

            return properties.OrderBy(p => p.Order ?? -1).ToList();
        }

        /// <summary>
        /// Creates a <see cref="JsonProperty"/> for the given <see cref="MemberInfo"/> and conditionally
        /// adds a guard for avoid serializing NHibernate proxies.
        /// </summary>
        /// <param name="memberSerialization">The member's parent <see cref="MemberSerialization"/>.</param>
        /// <param name="member">The member to create a <see cref="JsonProperty"/> for.</param>
        /// <param name="metadata">The metadata of the member type.</param>
        /// <returns>A created <see cref="JsonProperty"/> for the given <see cref="MemberInfo"/>.</returns>
        protected virtual JsonProperty CreateProperty(
            MemberInfo member,
            MemberSerialization memberSerialization,
            ModelMetadata metadata)
        {
            var property = CreateProperty(member, memberSerialization);
            TrySetupEntityProperty(property, member, metadata);

            return property;
        }

        /// <summary>
        /// Creates a <see cref="JsonProperty"/> for the given <see cref="SyntheticMemberConfiguration"/>.
        /// </summary>
        /// <param name="memberConfiguration">The synthetic member configuration to create a <see cref="JsonProperty"/> for.</param>
        /// <returns>A created <see cref="JsonProperty"/> for the given <see cref="SyntheticMemberConfiguration"/>.</returns>
        protected virtual JsonProperty CreateSyntheticProperty(SyntheticMemberConfiguration memberConfiguration)
        {
            var valueProvider = new SyntheticMemberValueProvider(memberConfiguration.MemberName, memberConfiguration.SerializeFunction);
            var property = new JsonProperty
            {
                PropertyName = ResolvePropertyName(memberConfiguration.MemberName),
                UnderlyingName = memberConfiguration.MemberName,
                PropertyType = memberConfiguration.MemberType,
                DeclaringType = memberConfiguration.DeclaringType,
                Readable = memberConfiguration.Serialize ?? true,
                Ignored = memberConfiguration.Ignored ?? false,
                Writable = false,
                ShouldSerialize = CreateShouldSerialize(memberConfiguration.DeclaringType, memberConfiguration.ShouldSerializePredicate, valueProvider.GetValue),
                ValueProvider = valueProvider
            };

            if (memberConfiguration.HasDefaultValue)
            {
                property.DefaultValue = memberConfiguration.DefaultValue;
            }

            return property;
        }

        /// <summary>
        /// Creates a <see cref="JsonProperty"/> for the given <see cref="SyntheticForeignKeyProperty"/>.
        /// </summary>
        /// <param name="type">The declared type of the synthetic member.</param>
        /// <param name="syntheticForeignKeyProperty">The synthetic foreign key member to create a <see cref="JsonProperty"/> for.</param>
        /// <param name="memberConfiguration">The synthetic foreign key member configuration</param>
        /// <param name="associationMemberConfiguration">The association member configuration.</param>
        /// <returns>A created <see cref="JsonProperty"/> for the given <see cref="SyntheticForeignKeyProperty"/>.</returns>
        protected virtual JsonProperty CreateSyntheticForeignKeyProperty(
            Type type,
            SyntheticForeignKeyProperty syntheticForeignKeyProperty,
            SyntheticMemberConfiguration memberConfiguration,
            MemberConfiguration associationMemberConfiguration)
        {
            var property = new JsonProperty
            {
                PropertyName = ResolvePropertyName(syntheticForeignKeyProperty.Name),
                UnderlyingName = syntheticForeignKeyProperty.Name,
                PropertyType = syntheticForeignKeyProperty.IdentifierType,
                Ignored = memberConfiguration?.Ignored == true || associationMemberConfiguration?.Ignored == true && memberConfiguration?.Ignored == null,
                DeclaringType = type,
                Readable = memberConfiguration?.Serialize ?? associationMemberConfiguration?.Serialize ?? true,
                Writable = false,
                ValueProvider = new SyntheticForeignKeyPropertyValueProvider(
                    syntheticForeignKeyProperty.GetAssociationFunction,
                    syntheticForeignKeyProperty.GetIdentifierFunction,
                    syntheticForeignKeyProperty.HasCompositeKey)
            };

            if (memberConfiguration?.HasDefaultValue == true)
            {
                property.DefaultValue = memberConfiguration.DefaultValue;
            }

            return property;
        }

        /// <summary>
        /// Configures the <see cref="JsonProperty"/> by the given <see cref="MemberConfiguration"/>.
        /// </summary>
        /// <param name="property">The property to configure.</param>
        /// <param name="memberConfiguration">The configuration to apply.</param>
        protected virtual void ConfigureProperty(JsonProperty property, MemberConfiguration memberConfiguration)
        {
            var serializePredicate = memberConfiguration.ShouldSerializePredicate;
            if (serializePredicate != null) // Can be the function defined in this.CreateProperty (check NHibernate initialized property) or a custom one
            {
                property.ShouldSerialize = MergePredicates(property.ShouldSerialize, serializePredicate);
            }

            var deserializePredicate = memberConfiguration.ShouldDeserializePredicate;
            if (deserializePredicate != null)
            {
                property.ShouldDeserialize = MergePredicates(property.ShouldDeserialize, deserializePredicate);
            }

            property.Ignored = memberConfiguration.Ignored.HasValue && memberConfiguration.Ignored.Value;
            property.Writable = memberConfiguration.Deserialize ?? property.Writable;
            property.Readable = memberConfiguration.Serialize ?? property.Readable;
            if (memberConfiguration.SerializeFunction != null || memberConfiguration.DeserializeFunction != null)
            {
                property.ValueProvider = new BreezeValueProvider(
                    property.ValueProvider,
                    memberConfiguration.MemberInfo,
                    memberConfiguration.SerializeFunction,
                    memberConfiguration.DeserializeFunction);
            }
        }

        private void ApplyModelConfiguration(
            Type type,
            JsonPropertyCollection properties,
            ModelConfiguration modelConfiguration,
            ModelMetadata metadata,
            Dictionary<JsonProperty, MemberInfo> propertyMembers)
        {
            foreach (var property in properties)
            {
                var memberConfiguration = modelConfiguration.GetMember(property.UnderlyingName);
                if (memberConfiguration != null)
                {
                    ConfigureProperty(property, memberConfiguration);
                }

                var isMappedProperty = metadata?.AllProperties.Contains(property.UnderlyingName);
                var syntheticConfiguration = modelConfiguration.GetSyntheticMember(property.UnderlyingName);
                if (syntheticConfiguration == null &&
                    isMappedProperty == false &&
                    memberConfiguration?.Serialize != true &&
                    memberConfiguration?.Deserialize != true)
                {
                    // Do not serialize a non mapped entity property by default (we do not want to expose data that the client will not use)
                    property.Ignored = memberConfiguration?.Ignored ?? true;
                }

                if (syntheticConfiguration?.SerializeFunction != null ||
                    memberConfiguration?.SerializeFunction != null ||
                    propertyMembers.TryGetValue(property, out var member) && member.IsProperty() && !member.IsAutoProperty(true))
                {
                    // Throw when a non auto or synthetic property lazy loads an uninitialized association (e.g. CustomerId => Organization.Customer.Id)
                    property.ValueProvider = CreateLazyLoadGuard(property.ValueProvider, type, property.UnderlyingName);
                }

                if (isMappedProperty == true)
                {
                    property.Writable = memberConfiguration?.Deserialize ?? true; // Non public mapped property setter shall be writable by default
                }
            }
        }

        private void TrySetupEntityProperty(
            JsonProperty jsonProperty,
            MemberInfo member,
            ModelMetadata metadata)
        {
            if (!(metadata is EntityMetadata entityMetadata))
            {
                if (IsAssociation(member, metadata, out var isScalar))
                {
                    SetupEntityProperty(jsonProperty, CreateShouldSerializeForAssociationProperty(member), isScalar);
                }

                return;
            }

            var persister = entityMetadata.EntityPersister;
            var propertyIndex = persister.EntityMetamodel.GetPropertyIndexOrNull(member.Name);
            if (!propertyIndex.HasValue)
            {
                // Check whether the property is a many-to-one key
                if (entityMetadata.ManyToOneIdentifierProperties.TryGetValue(member.Name, out var getter))
                {
                    SetupEntityProperty(jsonProperty, CreateShouldSerializeForAssociationProperty(getter), true);
                }

                return; // Skip non mapped properties
            }
                
            var propertyType = persister.PropertyTypes[propertyIndex.Value];
            var isLazy = persister.PropertyLaziness[propertyIndex.Value];
            if (!propertyType.IsCollectionType && !propertyType.IsAssociationType && !isLazy)
            {
                return; // Skip properties that are not collection, association and lazy
            } 

            var predicate = isLazy
                ? CreateShouldSerializeForLazyProperty(member.Name)
                : CreateShouldSerializeForAssociationProperty(propertyIndex.Value, persister.GetPropertyValue);

            SetupEntityProperty(jsonProperty, predicate, propertyType.IsAssociationType && !propertyType.IsCollectionType);
        }

        private bool IsAssociation(MemberInfo member, ModelMetadata metadata, out bool isScalar)
        {
            if (metadata == null)
            {
                return IsAssociation(member, _entityMetadataProvider, out isScalar, out _);
            }

            if (!metadata.Associations.TryGetValue(member.Name, out var association))
            {
                isScalar = false;
                return false;
            }

            isScalar = association.IsScalar;
            return true;
        }

        private Predicate<object> CreateShouldSerialize(Type memberType, Predicate<object> currentPredicate, Func<object, object> getValue)
        {
            return _entityMetadataProvider.IsEntityType(memberType)
                ? MergePredicates(currentPredicate, o => NHibernateUtil.IsInitialized(getValue(o)))
                : currentPredicate;
        }

        private Predicate<object> CreateShouldSerializeForAssociationProperty(MemberInfo member)
        {
            var valueProvider = CreateMemberValueProvider(member);
            if (member.IsProperty() && !member.IsAutoProperty(false))
            {
                // A non auto property may trigger a lazy load (e.g. CustomerId => Organization.Customer.Id)
                valueProvider = CreateLazyLoadGuard(valueProvider, member.ReflectedType, member.Name);
            }
            
            return model => NHibernateUtil.IsInitialized(valueProvider.GetValue(model));
        }

        private IValueProvider CreateLazyLoadGuard(IValueProvider valueProvider, Type reflectedType, string memberName)
        {
            var guardedGetValueFunction = _lazyLoadGuardProvider.AddGuard(valueProvider.GetValue, reflectedType, memberName);
            return new LazyLoadGuardDecorator(guardedGetValueFunction, valueProvider.SetValue);
        }

        private ModelMetadata GetTypeMetadata(Type type)
        {
            if (_entityMetadataProvider.IsEntityType(type))
            {
                return _entityMetadataProvider.GetMetadata(type);
            }

            if (_clientModelMetadataProvider.IsClientModel(type))
            {
                return _clientModelMetadataProvider.GetMetadata(type);
            }

            return null;
        }

        internal static bool IsAssociation(MemberInfo member, IEntityMetadataProvider classMetadataProvider, out bool isScalar, out Type memberType)
        {
            memberType = member.GetUnderlyingType();
            if (memberType == typeof(string))
            {
                isScalar = false;
                return false;
            }

            if (memberType.TryGetGenericType(typeof(IEnumerable<>), out var genericType))
            {
                isScalar = false;
                memberType = genericType.GetGenericArguments()[0];
            }
            else if (typeof(IEnumerable).IsAssignableFrom(memberType))
            {
                memberType = null;
                isScalar = false;
                return true; // To be safe in case the enumerable is an uninitialized NHibernate collection
            }
            else
            {
                isScalar = true;
            }

            return classMetadataProvider.IsEntityType(memberType);
        }

        private static void SetupEntityProperty(JsonProperty jsonProperty, Predicate<object> shouldSerializePredicate, bool isScalar)
        {
            jsonProperty.ShouldSerialize = MergePredicates(jsonProperty.ShouldSerialize, shouldSerializePredicate);
            if (isScalar)
            {
                // Unwrap NHibernate proxy
                jsonProperty.ValueProvider = new NHibernateProxyValueProvider(jsonProperty.ValueProvider);
            }
        }

        private static Predicate<object> CreateShouldSerializeForLazyProperty(string propertyName)
        {
            return entity => NHibernateUtil.IsPropertyInitialized(entity, propertyName);
        }

        private static Predicate<object> CreateShouldSerializeForAssociationProperty(
            int propertyIndex,
            Func<object, int, object> getPropertyValueFunction)
        {
            return entity => NHibernateUtil.IsInitialized(getPropertyValueFunction(entity, propertyIndex));
        }

        private static Predicate<object> CreateShouldSerializeForAssociationProperty(Func<object, object> getPropertyValueFunction)
        {
            return entity => NHibernateUtil.IsInitialized(getPropertyValueFunction(entity));
        }

        private static Predicate<object> MergePredicates(Predicate<object> currentPredicate, Predicate<object> newPredicate)
        {
            return currentPredicate != null
                ? o => currentPredicate(o) && newPredicate(o)
                : newPredicate;
        }
    }
}
