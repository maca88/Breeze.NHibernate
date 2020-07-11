using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Breeze.NHibernate.Configuration;
using Breeze.NHibernate.Metadata;
using NHibernate;
using NHibernate.Engine;
using NHibernate.Persister.Collection;
using NHibernate.Persister.Entity;
using NHibernate.Type;
using EntityType = Breeze.NHibernate.Metadata.EntityType;
using static Breeze.NHibernate.Internal.BreezeHelper;

namespace Breeze.NHibernate
{
    /// <summary>
    /// A builder for building <see cref="BreezeMetadata"/>.
    /// </summary>
    public class BreezeMetadataBuilder
    {
        private readonly HashSet<Type> _addedTypes = new HashSet<Type>();
        private readonly INHibernateClassMetadataProvider _classMetadataProvider;
        private readonly IEntityMetadataProvider _entityMetadataProvider;
        private readonly IClientModelMetadataProvider _clientModelMetadataProvider;
        private readonly IBreezeConfigurator _breezeConfigurator;
        private IPropertyValidatorsProvider _propertyValidatorsProvider;
        private Predicate<Type> _includePredicate;
        private Func<string, string> _pluralizeFunction;
        private Action<DataProperty, StructuralType> _dataPropertyCreatedCallback;
        private IEnumerable<Assembly> _clientModelAssemblies;
        private bool _orphanDeleteEnabled;
        private string _localQueryComparisonOptions;
        private Dictionary<Type, EntityMetadata> _entityMetadata;
        private Dictionary<Type, ClientModelMetadata> _clientModelMetadata;

        public BreezeMetadataBuilder(
            INHibernateClassMetadataProvider classMetadataProvider,
            IEntityMetadataProvider entityMetadataProvider,
            IBreezeConfigurator breezeConfigurator,
            IClientModelMetadataProvider clientModelMetadataProvider,
            IPropertyValidatorsProvider propertyValidatorsProvider)
        {
            _classMetadataProvider = classMetadataProvider;
            _entityMetadataProvider = entityMetadataProvider;
            _breezeConfigurator = breezeConfigurator;
            _clientModelMetadataProvider = clientModelMetadataProvider;
            _propertyValidatorsProvider = propertyValidatorsProvider;
        }

        /// <summary>
        /// Set a predicate that controls which entities to include in the metadata.
        /// </summary>
        /// <param name="predicate">The filter predicate.</param>
        public BreezeMetadataBuilder WithIncludeFilter(Predicate<Type> predicate)
        {
            _includePredicate = predicate;
            return this;
        }

        /// <summary>
        /// Set a function that will pluralize <see cref="EntityType.DefaultResourceName"/>.
        /// </summary>
        /// <param name="pluralizeFunction">The pluralize function.</param>
        public BreezeMetadataBuilder WithPluralizeFunction(Func<string, string> pluralizeFunction)
        {
            _pluralizeFunction = pluralizeFunction;
            return this;
        }

        /// <summary>
        /// Set a callback that will be called after every created <see cref="DataProperty"/>.
        /// </summary>
        /// <param name="createdCallback">The callback to call.</param>
        public BreezeMetadataBuilder WithDataPropertyCreatedCallback(Action<DataProperty, StructuralType> createdCallback)
        {
            _dataPropertyCreatedCallback = createdCallback;
            return this;
        }

        /// <summary>
        /// Set the property validator provider that will be used to fill <see cref="BaseProperty.Validators"/>.
        /// </summary>
        /// <param name="propertyValidatorsProvider">The property validator provider.</param>
        public BreezeMetadataBuilder WithPropertyValidatorsProvider(IPropertyValidatorsProvider propertyValidatorsProvider)
        {
            _propertyValidatorsProvider = propertyValidatorsProvider;
            return this;
        }

        /// <summary>
        /// Set the assemblies where <see cref="IClientModel"/> classes are located, which be included in the <see cref="BreezeMetadata"/>.
        /// </summary>
        /// <param name="assemblies">The client model assemblies to include.</param>
        public BreezeMetadataBuilder WithClientModelAssemblies(IEnumerable<Assembly> assemblies)
        {
            _clientModelAssemblies = assemblies;
            return this;
        }

        /// <summary>
        /// Set the local query comparison option needed by the client.
        /// </summary>
        /// <param name="value">The option to use.</param>
        public BreezeMetadataBuilder WithLocalQueryComparisonOptions(string value)
        {
            _localQueryComparisonOptions = value;
            return this;
        }

        /// <summary>
        /// Set a non-standard option that will set <see cref="NavigationProperty.HasOrphanDelete"/> when the cascade style
        /// of the association has <see cref="CascadeStyle.HasOrphanDelete"/> set to <see langword="true"/>.
        /// </summary>
        /// <param name="value">Whether to set <see cref="NavigationProperty.HasOrphanDelete"/>.</param>
        public BreezeMetadataBuilder WithOrphanDeleteEnabled(bool value = true)
        {
            _orphanDeleteEnabled = value;
            return this;
        }

        /// <summary>
        /// Builds <see cref="BreezeMetadata"/>.
        /// </summary>
        /// <returns>The breeze metadata.</returns>
        public BreezeMetadata Build()
        {
            var metadata = new BreezeMetadata
            {
                StructuralTypes = new List<StructuralType>(),
                ResourceEntityTypeMap = new Dictionary<string, string>(),
                EnumTypes = new List<EnumType>(),
                LocalQueryComparisonOptions = _localQueryComparisonOptions ?? "caseInsensitiveSQL"
            };

            _addedTypes.Clear();
            _entityMetadata = _classMetadataProvider.GetAll()
                .Select(o => o.MappedClass)
                .Where(o => o != null)
                .Where(o => _includePredicate?.Invoke(o) ?? true)
                .Distinct()
                .Select(o => new {MappedClass = o, Metadata = _entityMetadataProvider.GetMetadata(o)})
                .ToDictionary(o => o.MappedClass, o => o.Metadata);

            foreach (var entityMetadata in _entityMetadata.Values)
            {
                AddEntityType(entityMetadata, metadata);
            }

            if (_clientModelAssemblies == null)
            {
                return metadata;
            }

            // Add client models
            _clientModelMetadata = _clientModelAssemblies?.SelectMany(assembly => assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && _clientModelMetadataProvider.IsClientModel(t)))
                .Where(t => _includePredicate?.Invoke(t) ?? true)
                .Select(o => _clientModelMetadataProvider.GetMetadata(o))
                .ToDictionary(o => o.Type, o => o);

            foreach (var modelMetadata in _clientModelMetadata.Values)
            {
                AddClientModelType(modelMetadata, metadata);
            }

            return metadata;
        }

        private void AddEntityType(
            EntityMetadata entityMetadata,
            BreezeMetadata metadata)
        {
            var type = entityMetadata.Type;
            var modelConfiguration = _breezeConfigurator.GetModelConfiguration(type);
            var entityType = new EntityType(type)
            {
                BaseTypeName = entityMetadata.BaseType != null
                    ? GetBreezeTypeFullName(entityMetadata.BaseType)
                    : null,
                AutoGeneratedKeyType = modelConfiguration.AutoGeneratedKeyType ?? entityMetadata.AutoGeneratedKeyType,
                DefaultResourceName = modelConfiguration.ResourceName ?? Pluralize(type.Name),
                Custom = modelConfiguration.Custom
            };

            metadata.ResourceEntityTypeMap.Add(entityType.DefaultResourceName, GetBreezeTypeFullName(type));

            AddProperties(entityMetadata, entityType, modelConfiguration, metadata);

            metadata.StructuralTypes.Add(entityType);
        }

        private void AddClientModelType(ClientModelMetadata modelMetadata, BreezeMetadata metadata)
        {
            var modelConfiguration = _breezeConfigurator.GetModelConfiguration(modelMetadata.Type);
            var entityType = GetOrCreateEntityType(
                modelMetadata.Type,
                modelConfiguration,
                metadata);
            entityType.IsUnmapped = true;

            foreach (var property in modelMetadata.Properties)
            {
                var memberConfiguration = modelConfiguration.GetMember(property.Name);
                if (memberConfiguration?.Ignored == true)
                {
                    continue;
                }

                if (property.IsAssociationType)
                {
                    entityType.NavigationProperties.Add(CreateNavigationProperty(property, modelMetadata));
                    continue;
                }

                if (property.IsComplexType)
                {
                    AddComplexType(property.Type, metadata);
                }

                var dataProperty = CreateDataProperty(property, memberConfiguration, entityType);
                if (property.IsPartOfKey)
                {
                    entityType.DataProperties.Insert(0, dataProperty);
                }
                else
                {
                    entityType.DataProperties.Add(dataProperty);
                }
            }

            AddSyntheticMembers(entityType, modelConfiguration);
            AddSyntheticForeignKeyProperties(modelMetadata, entityType, modelConfiguration);
        }

        private void AddProperties(
            EntityMetadata entityMetadata,
            EntityType entityType,
            ModelConfiguration modelConfiguration,
            BreezeMetadata metadata)
        {
            var persister = entityMetadata.EntityPersister;
            AddDataProperties(
                persister.PropertyNames,
                persister.PropertyTypes,
                persister.PropertyNullability,
                entityMetadata.DerivedPropertyNames,
                persister.VersionProperty,
                false,
                entityType,
                modelConfiguration,
                metadata,
                persister.Factory);
            AddIdentifierProperties(persister, entityType, modelConfiguration, entityMetadata, metadata);
            AddSyntheticMembers(entityType, modelConfiguration);
            AddSyntheticForeignKeyProperties(entityMetadata, entityType, modelConfiguration);
            AddIncludedMembers(entityType, modelConfiguration, entityMetadata);

            // We do the association properties after the data properties, so we can do the foreign key lookups
            AddAssociationProperties(
                persister.PropertyNames,
                persister.PropertyTypes,
                false,
                i => persister.PropertyCascadeStyles[i],
                entityType,
                entityMetadata.DerivedPropertyNames,
                modelConfiguration,
                entityMetadata);
        }

        private void AddSyntheticMembers(
            EntityType entityType,
            ModelConfiguration modelConfiguration)
        {
            foreach (var memberConfiguration in modelConfiguration.SyntheticMembers.Values.Where(o => o.Added))
            {
                var dataProperty = CreateDataProperty(
                    memberConfiguration.MemberName,
                    GetDataType(NHibernateUtil.GuessType(memberConfiguration.MemberType)),
                    false,
                    !memberConfiguration.MemberType.IsValueType,
                    false,
                    memberConfiguration,
                    entityType,
                    null);

                dataProperty.IsUnmapped = true;
                entityType.DataProperties.Add(dataProperty);
            }
        }

        private void AddIncludedMembers(
            EntityType entityType,
            ModelConfiguration modelConfiguration,
            ModelMetadata metadata)
        {
            foreach (var memberConfiguration in modelConfiguration.Members.Values)
            {
                if (
                    memberConfiguration.Ignored != false ||
                    metadata.AllProperties.Contains(memberConfiguration.MemberName))
                {
                    continue;
                }

                var dataProperty = CreateDataProperty(
                    memberConfiguration.MemberName,
                    GetDataType(NHibernateUtil.GuessType(memberConfiguration.MemberType)),
                    false,
                    !memberConfiguration.MemberType.IsValueType,
                    false,
                    memberConfiguration,
                    entityType,
                    null);

                dataProperty.IsUnmapped = true;
                entityType.DataProperties.Add(dataProperty);
            }
        }

        private void AddSyntheticForeignKeyProperties(
            ModelMetadata modelMetadata,
            EntityType entityType,
            ModelConfiguration modelConfiguration)
        {
            foreach (var syntheticProperty in modelMetadata.SyntheticForeignKeyProperties.Values.Where(o => !o.Derived))
            {
                var memberConfiguration = modelConfiguration.GetSyntheticMember(syntheticProperty.Name);
                var associationConfiguration = modelConfiguration.GetMember(syntheticProperty.AssociationPropertyName);
                if (memberConfiguration?.Ignored == true || associationConfiguration?.Ignored == true && memberConfiguration?.Ignored == null)
                {
                    continue;
                }

                var dataProperty = CreateDataProperty(
                    syntheticProperty.Name,
                    syntheticProperty.IdentifierDataType,
                    syntheticProperty.IsPartOfKey,
                    associationConfiguration?.IsNullable ?? syntheticProperty.IsNullable,
                    false,
                    memberConfiguration,
                    entityType,
                    associationConfiguration?.MaxLength ?? syntheticProperty.IdentifierLength);

                dataProperty.IsUnmapped = true;
                entityType.DataProperties.Add(dataProperty);
            }
        }

        private void AddIdentifierProperties(
            AbstractEntityPersister persister,
            EntityType entityType,
            ModelConfiguration modelConfiguration,
            EntityMetadata entityMetadata,
            BreezeMetadata metadata)
        {
            // Add the identifier properties only for the root class
            if (persister.IsInherited)
            {
                return;
            }

            if (persister.HasIdentifierProperty)
            {
                var memberConfiguration = modelConfiguration.GetMember(persister.IdentifierPropertyName);
                var dataProperty = CreateDataProperty(
                    persister.IdentifierPropertyName,
                    persister.IdentifierType,
                    true,
                    false,
                    false,
                    memberConfiguration,
                    entityType,
                    persister.Factory);
                entityType.DataProperties.Insert(0, dataProperty);
            }
            else if (persister.IdentifierType is IAbstractComponentType componentType)
            {
                modelConfiguration = _breezeConfigurator.GetModelConfiguration(componentType.ReturnedClass);
                AddDataProperties(
                    componentType.PropertyNames,
                    componentType.Subtypes,
                    componentType.PropertyNullability,
                    null,
                    null,
                    true,
                    entityType,
                    modelConfiguration,
                    metadata,
                    persister.Factory);
                AddAssociationProperties(
                    componentType.PropertyNames,
                    componentType.Subtypes,
                    true,
                    i => componentType.GetCascadeStyle(i),
                    entityType,
                    null,
                    modelConfiguration,
                    entityMetadata);
            }
        }

        private void AddDataProperties(
            string[] propertyNames,
            IType[] propertyTypes,
            bool[] propertyNullability,
            IReadOnlyCollection<string> superProperties,
            int? versionPropertyIndex,
            bool partsOfKey,
            StructuralType structuralType,
            ModelConfiguration modelConfiguration,
            BreezeMetadata metadata,
            ISessionFactoryImplementor sessionFactory)
        {
            for (var i = 0; i < propertyNames.Length; i++)
            {
                var propertyType = propertyTypes[i];
                var propertyName = propertyNames[i];
                var memberConfiguration = modelConfiguration.GetMember(propertyNames[i]);
                if (
                    memberConfiguration?.Ignored == true ||
                    superProperties?.Contains(propertyName) == true ||
                    propertyType.IsAssociationType)
                {
                    continue;
                }

                var dataProperty = CreateDataProperty(
                    propertyName,
                    propertyType,
                    partsOfKey,
                    propertyNullability[i],
                    i == versionPropertyIndex,
                    memberConfiguration,
                    structuralType,
                    sessionFactory);
                if (propertyType is IAbstractComponentType componentType)
                {
                    AddComplexType(
                        componentType,
                        _breezeConfigurator.GetModelConfiguration(propertyType.ReturnedClass),
                        metadata,
                        partsOfKey,
                        sessionFactory);
                }

                if (partsOfKey)
                {
                    structuralType.DataProperties.Insert(0, dataProperty);
                }
                else
                {
                    structuralType.DataProperties.Add(dataProperty);
                }

                // Map enum type
                if (propertyType is AbstractEnumType nhEnumType && _addedTypes.Add(nhEnumType.ReturnedClass))
                {
                    var enumType = nhEnumType.ReturnedClass;
                    metadata.EnumTypes.Add(new EnumType
                    {
                        Namespace = enumType.Namespace,
                        ShortName = enumType.Name,
                        Values = Enum.GetNames(enumType)
                    });
                }
            }
        }

        private void AddAssociationProperties(
            string[] propertyNames,
            IType[] propertyTypes,
            bool partsOfKey,
            Func<int, CascadeStyle> getCascadeStyle,
            EntityType entityType,
            IReadOnlyCollection<string> superProperties,
            ModelConfiguration modelConfiguration,
            EntityMetadata entityMetadata)
        {
            for (var i = 0; i < propertyNames.Length; i++)
            {
                var propertyType = propertyTypes[i] as IAssociationType;
                var propertyName = propertyNames[i];
                var memberConfiguration = modelConfiguration.GetMember(propertyNames[i]);
                if (memberConfiguration?.Ignored == true ||
                    superProperties?.Contains(propertyName) == true ||
                    propertyType == null)
                {
                    continue;
                }

                var navigationProperty = CreateNavigationProperty(propertyType, propertyName, i, partsOfKey, getCascadeStyle, entityType, entityMetadata);
                if (navigationProperty != null)
                {
                    entityType.NavigationProperties.Add(navigationProperty);
                }
            }
        }

        private NavigationProperty CreateNavigationProperty(
            IAssociationType associationType,
            string propertyName,
            int propertyIndex,
            bool partOfKey,
            Func<int, CascadeStyle> getCascadeStyle,
            StructuralType structuralType,
            EntityMetadata entityMetadata)
        {
            var navigationProperty = new NavigationProperty
            {
                NameOnServer = propertyName,
                IsScalar = !associationType.IsCollectionType
            };

            var joinable = associationType.GetAssociatedJoinable(entityMetadata.EntityPersister.Factory);
            if (associationType.IsCollectionType)
            {
                var collectionPersister = (IQueryableCollection) joinable;
                var elementPersister = collectionPersister.ElementPersister;
                if (!_entityMetadata.ContainsKey(elementPersister.MappedClass) ||
                    !entityMetadata.Associations.TryGetValue(propertyName, out var association))
                {
                    // Element is excluded from metadata, exclude also the relation
                    return null;
                }

                navigationProperty.AssociationName = GetAssociationName(structuralType.ShortName, elementPersister.MappedClass.Name, association.ForeignKeyPropertyNames);
                navigationProperty.EntityTypeName = GetBreezeTypeFullName(elementPersister.MappedClass);
                navigationProperty.InvForeignKeyNamesOnServer = association.ForeignKeyPropertyNames;
            }
            else
            {
                // Many to one
                var relatedPersister = (IEntityPersister) joinable;
                if (!_entityMetadata.ContainsKey(relatedPersister.MappedClass) ||
                    !entityMetadata.Associations.TryGetValue(propertyName, out var association))
                {
                    // Element is excluded from metadata, exclude also the relation
                    return null;
                }

                navigationProperty.AssociationName = GetAssociationName(structuralType.ShortName, relatedPersister.MappedClass.Name, association.ForeignKeyPropertyNames);
                navigationProperty.EntityTypeName = GetBreezeTypeFullName(relatedPersister.MappedClass);
                if (association.ForeignKeyDirection == ForeignKeyDirection.ForeignKeyFromParent)
                {
                    navigationProperty.ForeignKeyNamesOnServer = association.ForeignKeyPropertyNames;
                }
                else
                {
                    navigationProperty.InvForeignKeyNamesOnServer = association.ForeignKeyPropertyNames;
                }

                if (partOfKey)
                {
                    // NH does not set FK properties as keys when the entity relation is set as key, we have to do it manually
                    var keyProperties = new HashSet<string>(association.ForeignKeyPropertyNames);
                    foreach (var dataProperty in structuralType.DataProperties)
                    {
                        if (keyProperties.Contains(dataProperty.NameOnServer))
                        {
                            dataProperty.IsPartOfKey = true;
                        }
                    }
                }
            }

            if (_orphanDeleteEnabled)
            {
                navigationProperty.HasOrphanDelete = getCascadeStyle(propertyIndex).HasOrphanDelete;
            }

            return navigationProperty;
        }

        private NavigationProperty CreateNavigationProperty(
            ClientModelProperty property,
            ClientModelMetadata modelMetadata)
        {
            var fkPropertyNames = modelMetadata.Associations[property.Name].ForeignKeyPropertyNames;
            return new NavigationProperty
            {
                IsScalar = property.IsEntityType,
                EntityTypeName = GetBreezeTypeFullName(property.Type),
                NameOnServer = property.Name,
                AssociationName = GetAssociationName(modelMetadata.Type.Name, property.Type.Name, fkPropertyNames),
                InvForeignKeyNamesOnServer = property.IsEntityType ? null : fkPropertyNames,
                ForeignKeyNamesOnServer = property.IsEntityType ? fkPropertyNames : null,
            };
        }

        private void AddComplexType(
            IAbstractComponentType componentType,
            ModelConfiguration modelConfiguration,
            BreezeMetadata metadata,
            bool partOfKey,
            ISessionFactoryImplementor sessionFactory)
        {
            var type = componentType.ReturnedClass;
            if (!_addedTypes.Add(type))
            {
                return;
            }

            var complexType = new ComplexType(type) {Custom = modelConfiguration.Custom};
            AddDataProperties(
                componentType.PropertyNames,
                componentType.Subtypes,
                componentType.PropertyNullability,
                null,
                null,
                partOfKey,
                complexType,
                modelConfiguration,
                metadata,
                sessionFactory);

            // Add complex types at the beginning of the list
            metadata.StructuralTypes.Insert(0, complexType);
        }

        private void AddComplexType(Type type, BreezeMetadata metadata)
        {
            if (!_addedTypes.Add(type))
            {
                return;
            }

            var modelConfiguration = _breezeConfigurator.GetModelConfiguration(type);
            var complexType = new ComplexType(type) {Custom = modelConfiguration.Custom};
            foreach (var property in type.GetProperties())
            {
                if (!TryGetDataType(NHibernateUtil.GuessType(property.PropertyType), out var dataType))
                {
                    continue;
                }

                complexType.DataProperties.Add(
                    CreateDataProperty(
                        property.Name,
                        dataType,
                        false,
                        !property.PropertyType.IsValueType,
                        false,
                        modelConfiguration.GetMember(property.Name),
                        complexType,
                        null));
            }

            // Add complex types at the beginning of the list
            metadata.StructuralTypes.Insert(0, complexType);
        }

        private string Pluralize(string name)
        {
            return _pluralizeFunction?.Invoke(name) ?? DefaultPluralize(name);
        }

        private DataProperty CreateDataProperty(
            ClientModelProperty property,
            MemberConfiguration memberConfiguration,
            StructuralType structuralType)
        {
            var dataProperty = CreateDataProperty(
                property.Name,
                property.DataType,
                property.IsPartOfKey,
                property.IsNullable,
                false,
                memberConfiguration,
                structuralType,
                null);
            dataProperty.ComplexTypeName = property.IsComplexType ? GetBreezeTypeFullName(property.Type) : null;

            return dataProperty;
        }

        private DataProperty CreateDataProperty(
            string name,
            IType type,
            bool isPartOfKey,
            bool nullable,
            bool isVersion,
            MemberConfiguration memberConfiguration,
            StructuralType structuralType,
            ISessionFactoryImplementor sessionFactory)
        {
            var dataType = type.IsComponentType
                ? (DataType?) null
                : GetDataType(type);
            var dataProperty = CreateDataProperty(
                name,
                dataType,
                isPartOfKey,
                nullable,
                isVersion,
                memberConfiguration,
                structuralType,
                GetTypeLength(type, sessionFactory));
            dataProperty.ComplexTypeName = type.IsComponentType ? GetBreezeTypeFullName(type.ReturnedClass) : null;

            return dataProperty;
        }

        private DataProperty CreateDataProperty(
            string name,
            DataType? dataType,
            bool isPartOfKey,
            bool isNullable,
            bool isVersion,
            SyntheticMemberConfiguration memberConfiguration,
            StructuralType structuralType,
            int? typeLength)
        {
            return CreateDataProperty(
                name,
                dataType,
                isPartOfKey,
                memberConfiguration?.IsNullable ?? isNullable,
                isVersion,
                memberConfiguration?.HasDefaultValue == true,
                memberConfiguration?.DefaultValue,
                structuralType,
                memberConfiguration?.MaxLength ?? typeLength,
                memberConfiguration?.Custom);
        }

        private DataProperty CreateDataProperty(
            string name,
            DataType? dataType,
            bool isPartOfKey,
            bool isNullable,
            bool isVersion,
            MemberConfiguration memberConfiguration,
            StructuralType structuralType,
            int? typeLength)
        {
            return CreateDataProperty(
                name,
                dataType,
                isPartOfKey,
                memberConfiguration?.IsNullable ?? isNullable,
                isVersion,
                memberConfiguration?.HasDefaultValue == true,
                memberConfiguration?.DefaultValue,
                structuralType,
                memberConfiguration?.MaxLength ?? typeLength,
                memberConfiguration?.Custom);
        }

        private DataProperty CreateDataProperty(
            string name,
            DataType? dataType,
            bool isPartOfKey,
            bool isNullable,
            bool isVersion,
            bool hasDefaultValue,
            object defaultValue,
            StructuralType structuralType,
            int? typeLength,
            object custom)
        {
            var dataProperty = new DataProperty
            {
                NameOnServer = name,
                DataType = dataType,
                IsPartOfKey = isPartOfKey,
                IsNullable = isNullable,
                MaxLength = typeLength,
                Custom = custom,
                ConcurrencyMode = isVersion ? ConcurrencyMode.Fixed : (ConcurrencyMode?)null
            };
            if (hasDefaultValue)
            {
                dataProperty.DefaultValue = defaultValue;
            }

            _dataPropertyCreatedCallback?.Invoke(dataProperty, structuralType);

            var validators = _propertyValidatorsProvider.GetValidators(dataProperty, structuralType.Type).ToList();
            if (validators.Count > 0)
            {
                dataProperty.Validators = validators;
            }

            return dataProperty;
        }

        /// <summary>
        /// Creates an association name from two entity names.
        /// For consistency, puts the entity names in alphabetical order.
        /// </summary>
        /// <param name="name1"></param>
        /// <param name="name2"></param>
        /// <param name="fkPropertyNames">Used to ensure the association name is unique for a type</param>
        /// <returns></returns>
        private static string GetAssociationName(string name1, string name2, IEnumerable<string> fkPropertyNames)
        {
            var cols = string.Join("_", fkPropertyNames);

            return string.Compare(name1, name2, StringComparison.OrdinalIgnoreCase) < 0
                ? "AN_" + name1 + '_' + name2 + '_' + cols
                : "AN_" + name2 + '_' + name1 + '_' + cols;
        }

        private static string DefaultPluralize(string name)
        {
            var last = name.Length - 1;
            var c = name[last];
            switch (c)
            {
                case 'y':
                    return name.Substring(0, last) + "ies";
                default:
                    return name + 's';
            }
        }

        private static EntityType GetOrCreateEntityType(
            Type type,
            ModelConfiguration modelConfiguration,
            BreezeMetadata metadata)
        {
            if (metadata.StructuralTypes.FirstOrDefault(o => o.Type == type) is EntityType entityType)
            {
                return entityType;
            }

            entityType = new EntityType(type)
            {
                DefaultResourceName = modelConfiguration.ResourceName,
                AutoGeneratedKeyType = AutoGeneratedKeyType.KeyGenerator,
                Custom = modelConfiguration.Custom
            };

            if (entityType.DefaultResourceName != null)
            {
                metadata.ResourceEntityTypeMap.Add(modelConfiguration.ResourceName, GetBreezeTypeFullName(type));
            }

            metadata.StructuralTypes.Add(entityType);

            return entityType;
        }
    }
}
