using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Breeze.NHibernate.Internal;
using Breeze.NHibernate.Metadata;
using NHibernate;
using NHibernate.Type;

namespace Breeze.NHibernate
{
    /// <summary>
    /// The default implementation of <see cref="IClientModelMetadataProvider"/>, which generates metadata for types implementing <see cref="IClientModel"/>
    /// interface.
    /// </summary>
    public class ClientModelMetadataProvider : IClientModelMetadataProvider
    {
        private static readonly IReadOnlyList<string> IdentifierPropertyNames = new List<string>(1) { nameof(IClientModel.Id) };
        private static readonly IReadOnlyList<DataType> IdentifierPropertyDataTypes = new List<DataType>(1) { DataType.Int64 };
        private static readonly IReadOnlyList<Type> IdentifierPropertyTypes = new List<Type>(1) { typeof(long) };
        private static readonly IReadOnlyList<int?> IdentifierPropertyTypeLengths = new List<int?>(1) { null };
        private static readonly IReadOnlyList<Func<object,object>> IdentifierPropertyGetters;

        private readonly IEntityMetadataProvider _entityMetadataProvider;
        private readonly ISyntheticPropertyNameConvention _syntheticPropertyNameConvention;
        private readonly ConcurrentDictionary<Type, ClientModelMetadata> _cachedMetadata = new ConcurrentDictionary<Type, ClientModelMetadata>();

        static ClientModelMetadataProvider()
        {
            var parameter = Expression.Parameter(typeof(object));
            var function = Expression.Lambda<Func<object, object>>(
                Expression.Convert(
                    Expression.Property(
                        Expression.Convert(parameter, typeof(IClientModel)),
                        typeof(IClientModel).GetProperty(nameof(IClientModel.Id))
                    ),
                    typeof(object)),
                parameter
            ).Compile();

            IdentifierPropertyGetters = new [] {function};
        }

        public ClientModelMetadataProvider(
            IEntityMetadataProvider entityMetadataProvider,
            ISyntheticPropertyNameConvention syntheticPropertyNameConvention)
        {
            _entityMetadataProvider = entityMetadataProvider;
            _syntheticPropertyNameConvention = syntheticPropertyNameConvention;
        }

        /// <inheritdoc />
        public ClientModelMetadata GetMetadata(Type clientType)
        {
            return _cachedMetadata.GetOrAdd(clientType, Create);
        }

        /// <inheritdoc />
        public bool IsClientModel(Type type)
        {
            return typeof(IClientModel).IsAssignableFrom(type);
        }

        private ClientModelMetadata Create(Type clientType)
        {
            if (!IsClientModel(clientType))
            {
                return null;
            }

            var properties = clientType.GetProperties().ToDictionary(o => o.Name);
            var syntheticProperties = new List<SyntheticForeignKeyProperty>();
            var associations = new Dictionary<string, Association>();
            var clientProperties = new List<ClientModelProperty>(properties.Count);

            foreach (var property in clientType.GetProperties())
            {
                var propertyType = property.PropertyType;
                if (property.GetCustomAttribute<ComplexTypeAttribute>() != null)
                {
                    clientProperties.Add(new ClientModelProperty(
                        property.Name,
                        propertyType,
                        true,
                        null,
                        true,
                        false,
                        false,
                        false));
                    continue;
                }

                // Add a one to many relation
                if (propertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(propertyType))
                {
                    clientProperties.Add(CreateCollectionNavigationProperty(clientType, property, propertyType.GetGenericArguments()[0], associations));
                    continue;
                }

                // Add a many to one relation
                var relatedEntityMetadata = _entityMetadataProvider.IsEntityType(propertyType)
                    ? _entityMetadataProvider.GetMetadata(propertyType)
                    : null;
                if (relatedEntityMetadata != null || IsClientModel(propertyType))
                {
                    clientProperties.Add(CreateEntityNavigationProperty(
                        clientType,
                        property,
                        properties,
                        relatedEntityMetadata,
                        syntheticProperties,
                        associations));
                    continue;
                }

                if (BreezeHelper.TryGetDataType(NHibernateUtil.GuessType(propertyType), out var dataType))
                {
                    clientProperties.Add(new ClientModelProperty(
                        property.Name,
                        propertyType,
                        false,
                        dataType,
                        !propertyType.IsValueType,
                        property.Name == nameof(IClientModel.Id),
                        false,
                        false));
                }
            }

            return new ClientModelMetadata(
                clientType,
                null, // TODO: base type
                AutoGeneratedKeyType.KeyGenerator,
                clientProperties,
                syntheticProperties.ToDictionary(o => o.Name),
                new List<Type>(), // TODO: base type
                new List<string>(), // TODO: base type
                IdentifierPropertyNames,
                IdentifierPropertyGetters,
                IdentifierPropertyTypes,
                IdentifierPropertyDataTypes,
                IdentifierPropertyTypeLengths,
                new HashSet<string>(associations.Values.Where(o => o.IsScalar).SelectMany(o => o.ForeignKeyPropertyNames)),
                associations
            );
        }

        private ClientModelProperty CreateCollectionNavigationProperty(
            Type clientType,
            PropertyInfo property,
            Type elementEntityType,
            Dictionary<string, Association> associations)
        {
            var relatedEntityMetadata = _entityMetadataProvider.IsEntityType(elementEntityType)
                ? _entityMetadataProvider.GetMetadata(elementEntityType)
                : null;

            // We need to find the related property on the other side of the relation
            var invPropNameAttr = property.GetCustomAttribute<InversePropertyAttribute>();
            var invPropName = invPropNameAttr?.PropertyName;
            var invProperties = string.IsNullOrEmpty(invPropName)
                ? elementEntityType.GetProperties().Where(o => o.PropertyType == clientType).ToList()
                : elementEntityType.GetProperties().Where(o => o.Name == invPropName).ToList();
            if (!invProperties.Any() && !string.IsNullOrEmpty(invPropName))
            {
                throw new InvalidOperationException(
                    $"Inverse property name '{invPropName}' was not found in type '{elementEntityType.FullName}'.");
            }

            if (invProperties.Count > 1)
            {
                throw new InvalidOperationException(
                    $"More that one inverse property was found, use InversePropertyAttribute for property {property.Name} of type '{elementEntityType.FullName}'.");
            }

            var fkPropertyNames = new List<string>(1);
            // Add synthetic property only for non entities in order to prevent having duplicate property names and
            // keep entity mappings clear
            if (relatedEntityMetadata == null)
            {
                if (!invProperties.Any())
                {
                    fkPropertyNames.Add(_syntheticPropertyNameConvention.GetName(clientType.Name, nameof(IClientModel.Id)));
                }
                else
                {
                    var invProperty = invProperties.First();
                    fkPropertyNames.Add(_syntheticPropertyNameConvention.GetName(invProperty.Name, nameof(IClientModel.Id)));
                }
            }

            associations.Add(property.Name, new Association(elementEntityType, fkPropertyNames, ForeignKeyDirection.ForeignKeyToParent, false));

            return new ClientModelProperty(
                property.Name,
                elementEntityType,
                false,
                null,
                true,
                false,
                true,
                false
            );
        }

        private ClientModelProperty CreateEntityNavigationProperty(
            Type type,
            PropertyInfo property,
            Dictionary<string, PropertyInfo> allProperties,
            EntityMetadata relatedEntityMetadata,
            List<SyntheticForeignKeyProperty> syntheticProperties,
            Dictionary<string, Association> associations)
        {
            IReadOnlyList<string> relatedPkPropertyNames;
            IReadOnlyList<DataType> relatedPkPropertyDataTypes;
            IReadOnlyList<Type> relatedPkPropertyTypes;
            IReadOnlyList<int?> relatedPkPropertyTypeLengths;
            IReadOnlyList<Func<object,object>> getRelatedPkPropertyFunctions;
            Type relatedEntityType;
            bool hasCompositeKey;
            if (relatedEntityMetadata != null)
            {
                relatedPkPropertyNames = relatedEntityMetadata.IdentifierPropertyNames;
                relatedPkPropertyTypes = relatedEntityMetadata.IdentifierPropertyTypes;
                relatedPkPropertyDataTypes = relatedEntityMetadata.IdentifierPropertyDataTypes;
                relatedPkPropertyTypeLengths = relatedEntityMetadata.IdentifierPropertyTypeLengths;
                getRelatedPkPropertyFunctions = relatedEntityMetadata.IdentifierPropertyGetters;
                relatedEntityType = relatedEntityMetadata.Type;
                hasCompositeKey = relatedEntityMetadata.HasCompositeKey;
            }
            else
            {
                relatedPkPropertyNames = IdentifierPropertyNames;
                relatedPkPropertyTypes = IdentifierPropertyTypes;
                relatedPkPropertyDataTypes = IdentifierPropertyDataTypes;
                relatedPkPropertyTypeLengths = IdentifierPropertyTypeLengths;
                getRelatedPkPropertyFunctions = IdentifierPropertyGetters;
                relatedEntityType = property.PropertyType;
                hasCompositeKey = false;
            }

            var fkPropertyNames = new List<string>(relatedPkPropertyNames.Count);
            for (var i = 0; i < relatedPkPropertyNames.Count; i++)
            {
                var syntheticPropName = _syntheticPropertyNameConvention.GetName(property.Name, relatedPkPropertyNames[i]);
                if (!allProperties.ContainsKey(syntheticPropName))
                {
                    syntheticProperties.Add(new SyntheticForeignKeyProperty
                    {
                        Name = syntheticPropName,
                        HasCompositeKey = hasCompositeKey,
                        EntityType = relatedEntityType,
                        AssociationPropertyName = property.Name,
                        GetAssociationFunction = CreateGetPropertyValueFunction(type, property),
                        IsNullable = true,
                        IsPartOfKey = false,
                        IdentifierPropertyName = relatedPkPropertyNames[i],
                        GetIdentifierFunction = getRelatedPkPropertyFunctions[i],
                        IdentifierType = relatedPkPropertyTypes[i],
                        IdentifierDataType = relatedPkPropertyDataTypes[i],
                        IdentifierLength = relatedPkPropertyTypeLengths[i]
                    });
                }

                fkPropertyNames.Add(syntheticPropName);
            }

            associations.Add(property.Name, new Association(relatedEntityType, fkPropertyNames, ForeignKeyDirection.ForeignKeyFromParent, true));

            return new ClientModelProperty(
                property.Name,
                property.PropertyType,
                false,
                null,
                true,
                false,
                false,
                true
            );
        }

        private static Func<object, object> CreateGetPropertyValueFunction(Type type, PropertyInfo property)
        {
            var parameter = Expression.Parameter(typeof(object));

            return Expression.Lambda<Func<object, object>>(
                Expression.Convert(
                    Expression.Property(Expression.Convert(parameter, type), property),
                    typeof(object)
                ),
                parameter
            ).Compile();
        }
    }
}
