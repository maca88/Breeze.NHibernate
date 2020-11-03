using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Breeze.NHibernate.Configuration;
using Breeze.NHibernate.Extensions;
using Breeze.NHibernate.Internal;
using Breeze.NHibernate.Metadata;
using Newtonsoft.Json.Linq;
using NHibernate;
using NHibernate.Id;
using NHibernate.Persister.Entity;
using NHibernate.Type;
using EntityType = NHibernate.Type.EntityType;
using static Breeze.NHibernate.Internal.BreezeHelper;

namespace Breeze.NHibernate
{
    /// <summary>
    /// Fetches changed entities and applies changes made by the client.
    /// </summary>
    public partial class EntityUpdater
    {
        private static readonly ConcurrentDictionary<Type, Dictionary<string, AdditionalProperty>> TypeAdditionalProperties =
            new ConcurrentDictionary<Type, Dictionary<string, AdditionalProperty>>();
        private static readonly Dictionary<string, AdditionalProperty> DefaultAdditionalProperties =
            new Dictionary<string, AdditionalProperty>();

        private readonly IEntityMetadataProvider _entityMetadataProvider;
        private readonly IBreezeConfigurator _breezeConfigurator;
        private readonly ISessionProvider _sessionProvider;

        private class AdditionalProperty
        {
            public AdditionalProperty(
                Func<object, object> getValue,
                Action<object, object> setValue)
            {
                GetValue = getValue;
                SetValue = setValue;
            }

            public Func<object, object> GetValue { get; }

            public Action<object, object> SetValue { get; }
        }

        /// <summary>
        /// Constructs an instance of <see cref="EntityUpdater"/>.
        /// </summary>
        public EntityUpdater(
            IEntityMetadataProvider entityMetadataProvider,
            IBreezeConfigurator breezeConfigurator,
            ISessionProvider sessionProvider)
        {
            _entityMetadataProvider = entityMetadataProvider;
            _breezeConfigurator = breezeConfigurator;
            _sessionProvider = sessionProvider;
        }

        /// <summary>
        /// Fetches changed entities and applies changes made by the client.
        /// </summary>
        /// <param name="persistenceManager">The persistence manager.</param>
        /// <param name="saveChangesContext">The save changes context.</param>
        /// <param name="saveChangesOptions">The save changes options.</param>
        /// <returns>The key mappings.</returns>
        public List<KeyMapping> FetchAndApplyChanges(
            PersistenceManager persistenceManager,
            SaveChangesContext saveChangesContext,
            SaveChangesOptions saveChangesOptions)
        {
            return FetchAndApplyChangesInternal(persistenceManager, saveChangesContext, saveChangesOptions);
        }

        /// <summary>
        /// Fetches changed entities and applies changes made by the client.
        /// </summary>
        /// <param name="persistenceManager">The persistence manager.</param>
        /// <param name="saveChangesContext">The save changes context.</param>
        /// <param name="saveChangesOptions">The save changes options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The key mappings.</returns>
        public Task<List<KeyMapping>> FetchAndApplyChangesAsync(
            PersistenceManager persistenceManager,
            SaveChangesContext saveChangesContext,
            AsyncSaveChangesOptions saveChangesOptions,
            CancellationToken cancellationToken = default)
        {
            return FetchAndApplyChangesInternalAsync(persistenceManager, saveChangesContext, saveChangesOptions, cancellationToken);
        }

        internal List<KeyMapping> FetchAndApplyChangesInternal(
            PersistenceManager persistenceManager,
            SaveChangesContext context,
            ISaveChangesOptions saveChangesOptions)
        {
            persistenceManager.BeforeFetchEntities(context);
            saveChangesOptions?.BeforeFetchEntities(context);

            var saveMap = context.SaveMap;
            using var sessionProvider = new ConfiguredSessionProvider(_sessionProvider);
            var entitiesIdMap = new EntityIdMap(saveMap.Count);
            var saveMapList = saveMap.ToList();
            // Make sure that entity types that have all key-many-to-one are sorted so that the associated entities will be processed before them
            saveMapList.Sort(CompareSaveMapTypes);
            foreach (var pair in saveMapList)
            {
                SetupDatabaseEntities(pair.Key, pair.Value, saveMap, entitiesIdMap, sessionProvider);
            }

            persistenceManager.BeforeApplyChanges(context);
            saveChangesOptions?.BeforeApplyChanges(context);
            
            AddAdditionalEntities(context.AdditionalEntities, context.SaveMap);
            context.AdditionalEntities?.Clear();
            var dependencyGraph = new DependencyGraph(saveMap.Count);
            foreach (var pair in saveMap)
            {
                ApplyChanges(pair.Key, pair.Value, dependencyGraph, saveMap, entitiesIdMap, sessionProvider);
            }

            persistenceManager.ValidateDependencyGraph(dependencyGraph, context);
            saveChangesOptions?.ValidateDependencyGraph(dependencyGraph, context);

            var saveOrder = dependencyGraph.GetSaveOrder();

            persistenceManager.BeforeSaveChanges(saveOrder, context);
            saveChangesOptions?.BeforeSaveChanges(saveOrder, context);

            ProcessSaves(saveOrder, persistenceManager, context, saveChangesOptions, sessionProvider);

            persistenceManager.AfterSaveChanges(saveOrder, context);
            saveChangesOptions?.AfterSaveChanges(saveOrder, context);

            FlushSessions(sessionProvider);
            RefreshFromSession(saveMap, sessionProvider);
            var keyMappings = GetKeyMappings(saveMap).ToList();

            persistenceManager.AfterFlushChanges(context, keyMappings);
            saveChangesOptions?.AfterFlushChanges(context, keyMappings);

            UpdateClientEntities(context);

            return keyMappings;
        }

        /// <summary>
        /// Updates <see cref="EntityInfo.ClientEntity"/> for to be returned back to the client.
        /// </summary>
        /// <param name="saveChangesContext">The save changes context.</param>
        private void UpdateClientEntities(SaveChangesContext saveChangesContext)
        {
            var saveMap = saveChangesContext.SaveMap;
            // Add additional entities that were added after ApplyChanges
            AddAdditionalEntities(saveChangesContext.AdditionalEntities, saveMap);
            var typesProxies = new Dictionary<Type, Dictionary<object, object>>();
            foreach (var pair in saveMap)
            {
                UpdateClientEntities(pair.Key, pair.Value, typesProxies);
            }
        }

        private void UpdateClientEntities(
            Type entityType,
            List<EntityInfo> entityInfos,
            Dictionary<Type, Dictionary<object, object>> typesProxies)
        {
            var metadata = _entityMetadataProvider.GetMetadata(entityType);
            var persister = metadata.EntityPersister;
            foreach (var entityInfo in entityInfos)
            {
                if (entityInfo.EntityState == EntityState.Detached)
                {
                    continue; // The added entity will be deleted on the client so no need to update any values
                }

                var clientEntity = entityInfo.ClientEntity;
                var dbEntity = entityInfo.Entity;
                var propertyTypes = persister.PropertyTypes;
                var propertyLaziness = persister.PropertyLaziness;
                for (var i = 0; i < propertyTypes.Length; i++)
                {
                    var propertyType = propertyTypes[i];
                    if (propertyType.IsCollectionType)
                    {
                        persister.SetPropertyValue(clientEntity, i, null);
                        continue;
                    }

                    object dbValue = null;
                    var propertyName = persister.PropertyNames[i];
                    // Update real FK property from the association property
                    if (metadata.ForeignKeyAssociations.TryGetValue(propertyName, out var entityAssociation))
                    {
                        object associationValue;
                        if (metadata.ManyToOneIdentifierProperties.TryGetValue(entityAssociation.AssociationPropertyName, out var getter))
                        {
                            associationValue = getter(dbEntity);
                        }
                        else
                        {
                            associationValue = persister.GetPropertyValue(dbEntity, persister.GetPropertyIndex(entityAssociation.AssociationPropertyName));
                        }

                        if (associationValue != null)
                        {
                            var associationMetadata = _entityMetadataProvider.GetMetadata(entityAssociation.EntityType);
                            var fkIndex = entityAssociation.ForeignKeyPropertyNames.IndexOf(propertyName);
                            dbValue = associationMetadata.IdentifierPropertyGetters[fkIndex](associationValue);
                        }
                    }
                    else if (!propertyLaziness[i] || NHibernateUtil.IsPropertyInitialized(dbEntity, propertyName))
                    {
                        dbValue = persister.GetPropertyValue(dbEntity, i);
                    }

                    if (metadata.Associations.TryGetValue(propertyName, out var association) && dbValue != null)
                    {
                        var relatedPersister = _entityMetadataProvider.GetMetadata(association.EntityType).EntityPersister;
                        if (!typesProxies.TryGetValue(association.EntityType, out var proxies))
                        {
                            proxies = new Dictionary<object, object>();
                            typesProxies.Add(association.EntityType, proxies);
                        }

                        var relationId = relatedPersister.GetIdentifier(dbValue);
                        if (!proxies.TryGetValue(relationId, out var proxy))
                        {
                            // We don't want to set the session in order to prevent lazy loading on serialization
                            proxy = relatedPersister.CreateProxy(relatedPersister.GetIdentifier(dbValue), null);
                            proxies.Add(relationId, proxy);
                        }

                        dbValue = proxy;
                    }

                    persister.SetPropertyValue(clientEntity, i, dbValue);
                }

                foreach (var additionalProperty in GetAdditionalProperties(metadata).Values)
                {
                    additionalProperty.SetValue(clientEntity, additionalProperty.GetValue(dbEntity));
                }

                // For key-many-to-one we have to create proxies in order to avoid serializing the associations
                persister.SetIdentifier(clientEntity,
                    metadata.ManyToOneIdentifierProperties.Count > 0
                        ? GetManyToOneClientIdentifier(entityInfo, typesProxies)
                        : persister.GetIdentifier(dbEntity));
            }
        }

        private void SetupManyToOneIdentifierProperties(
            List<EntityInfo> entitiesInfo,
            Dictionary<Type, List<EntityInfo>> saveMap,
            bool forClient,
            ConfiguredSessionProvider sessionProvider)
        {
            foreach (var entityInfo in entitiesInfo)
            {
                SetManyToOneIdentifier(entityInfo, entityInfo.ClientEntity, saveMap, forClient, sessionProvider);
            }
        }

        private void SetManyToOneIdentifier(
            EntityInfo entityInfo,
            object entity,
            Dictionary<Type, List<EntityInfo>> saveMap,
            bool forClient,
            ConfiguredSessionProvider sessionProvider)
        {
            var metadata = entityInfo.EntityMetadata;
            var idComponentType = (ComponentType) metadata.EntityPersister.IdentifierType;
            var idValues = idComponentType.GetPropertyValues(entityInfo.ClientEntity);
            foreach (var pkPropertyName in metadata.ManyToOneIdentifierProperties.Keys)
            {
                var association = (EntityAssociation) metadata.Associations[pkPropertyName];
                // Create PK
                var fkPropertyNames = association.ForeignKeyPropertyNames;
                var fkValues = new object[association.ForeignKeyPropertyNames.Count];
                for (var i = 0; i < fkPropertyNames.Count; i++)
                {
                    var fkPropertyName = fkPropertyNames[i];
                    if (metadata.SyntheticForeignKeyProperties.TryGetValue(fkPropertyName, out var syntheticFkProperty))
                    {
                        fkValues[i] = ConvertToType(entityInfo.UnmappedValuesMap[fkPropertyName], syntheticFkProperty.IdentifierType);
                    }
                    else
                    {
                        var index = metadata.EntityPersister.GetPropertyIndex(fkPropertyName);
                        fkValues[i] = metadata.EntityPersister.GetPropertyValue(entityInfo.ClientEntity, index);
                    }
                }

                object fkValue;
                if (association.IdentifierType is ComponentType componentType)
                {
                    // TODO: add support for a relation to another key-many-to-one
                    fkValue = componentType.Instantiate();
                    componentType.SetPropertyValues(fkValue, fkValues);
                }
                else
                {
                    fkValue = fkValues[0];
                }

                // Try find the related entity in the save map
                object fkEntity = null;
                var fkPersister = _entityMetadataProvider.GetMetadata(association.EntityType).EntityPersister;
                if (saveMap.TryGetValue(association.EntityType, out var fkEntitiesInfo))
                {
                    var fkEntityInfo = fkEntitiesInfo.Find(info => Equals(fkPersister.GetIdentifier(info.ClientEntity), fkValue));
                    if (fkEntityInfo != null)
                    {
                        fkEntity = forClient ? fkEntityInfo.ClientEntity : fkEntityInfo.Entity;
                    }
                }

                if (fkEntity == null)
                {
                    fkEntity = LoadEntity(association.EntityType, fkValue, sessionProvider);
                }

                var pkIndex = idComponentType.GetPropertyIndex(pkPropertyName);
                idValues[pkIndex] = fkEntity;
            }

            idComponentType.SetPropertyValues(entity, idValues);
        }

        private object GetManyToOneClientIdentifier(
            EntityInfo entityInfo,
            Dictionary<Type, Dictionary<object, object>> typesProxies)
        {
            var metadata = entityInfo.EntityMetadata;
            var idComponentType = (ComponentType)metadata.EntityPersister.IdentifierType;
            var idValues = idComponentType.GetPropertyValues(entityInfo.Entity);
            foreach (var pkPropertyName in metadata.ManyToOneIdentifierProperties.Keys)
            {
                var association = (EntityAssociation)metadata.Associations[pkPropertyName];
                var relatedPersister = _entityMetadataProvider.GetMetadata(association.EntityType).EntityPersister;
                var index = idComponentType.GetPropertyIndex(pkPropertyName);
                var pkPropertyValue = idValues[index];
                var pkPropertyId = relatedPersister.GetIdentifier(pkPropertyValue);
                if (!typesProxies.TryGetValue(association.EntityType, out var proxies))
                {
                    proxies = new Dictionary<object, object>();
                    typesProxies.Add(association.EntityType, proxies);
                }

                if (!proxies.TryGetValue(pkPropertyId, out var proxy))
                {
                    // We don't want to set the session in order to prevent lazy loading on serialization
                    proxy = relatedPersister.CreateProxy(pkPropertyId, null);
                    proxies.Add(pkPropertyId, proxy);
                }

                idValues[index] = proxy;
            }

            var id = idComponentType.Instantiate();
            idComponentType.SetPropertyValues(id, idValues);

            return id;
        }

        private void SetupDatabaseEntities(Type entityType,
            List<EntityInfo> entitiesInfo,
            Dictionary<Type, List<EntityInfo>> saveMap,
            EntityIdMap entitiesIdMap,
            ConfiguredSessionProvider sessionProvider)
        {
            var modelConfiguration = _breezeConfigurator.GetModelConfiguration(entityType);
            var metadata = _entityMetadataProvider.GetMetadata(entityType);
            if (metadata.ManyToOneIdentifierProperties.Count > 0)
            {
                SetupManyToOneIdentifierProperties(entitiesInfo, saveMap, true, sessionProvider);
            }

            var session = sessionProvider.GetSession(entityType);
            var batchSize = modelConfiguration.BatchFetchSize ?? session.GetSessionImplementation().Factory.Settings.DefaultBatchFetchSize;
            var persister = metadata.EntityPersister;
            var existingIds = entitiesInfo.Where(o => o.EntityState != EntityState.Added)
                .Select(o => persister.GetIdentifier(o.ClientEntity))
                .ToList();
            var batchFetcher =  metadata.BatchFetcher;
            var dbEntities = existingIds.Count > 0 ? batchFetcher.BatchFetch(session, existingIds, batchSize) : null;

            var idMap = entitiesIdMap.GetTypeIdMap(entityType, entitiesInfo.Count);
            foreach (var entityInfo in entitiesInfo)
            {
                foreach (var identifierPropertyName in metadata.IdentifierPropertyNames)
                {
                    if (entityInfo.OriginalValuesMap.ContainsKey(identifierPropertyName))
                    {
                        var errors = new[]
                        {
                            new EntityError
                            {
                                EntityTypeName = entityInfo.EntityType.FullName,
                                ErrorMessage = "Cannot update part of the entity's key",
                                ErrorName = "KeyUpdateException",
                                KeyValues = entityInfo.GetIdentifierValues(),
                                PropertyName = identifierPropertyName
                            }
                        };

                        throw new EntityErrorsException("Cannot update part of the entity's key", errors);
                    }
                }

                var id = persister.GetIdentifier(entityInfo.ClientEntity);
                object dbEntity;
                if (entityInfo.EntityState == EntityState.Added)
                {
                    dbEntity = metadata.CreateInstance();
                    if (metadata.AutoGeneratedKeyType != AutoGeneratedKeyType.Identity &&
                        !(persister.IdentifierGenerator is ForeignGenerator))
                    {
                        if (metadata.ManyToOneIdentifierProperties.Count > 0)
                        {
                            SetManyToOneIdentifier(entityInfo, dbEntity, saveMap, false, sessionProvider);
                        }
                        else
                        {
                            persister.SetIdentifier(dbEntity, id);
                        }
                    }
                }
                else if (dbEntities == null || !dbEntities.TryGetValue(id, out dbEntity) || dbEntity == null)
                {
                    throw new InvalidOperationException($"Entity {entityType} with id {id} was not found.");
                }

                entityInfo.Entity = dbEntity;
                idMap.Add(id, entityInfo);
                entitiesIdMap.AddToDerivedTypes(entityInfo, id, entitiesInfo.Count);
            }
        }

        private void ApplyChanges(
            Type entityType,
            List<EntityInfo> entityInfos,
            DependencyGraph dependencyGraph,
            Dictionary<Type, List<EntityInfo>> saveMap,
            EntityIdMap entitiesIdMap,
            ConfiguredSessionProvider sessionProvider)
        {
            var modelConfiguration = _breezeConfigurator.GetModelConfiguration(entityType);
            foreach (var entityInfo in entityInfos)
            {
                dependencyGraph.AddToGraph(entityInfo);
                if (entityInfo.ServerSide)
                {
                    AddToGraphAdditionalEntityAssociations(entityInfo, saveMap, dependencyGraph);
                    continue;
                }

                switch (entityInfo.EntityState)
                {
                    case EntityState.Added:
                        ApplyChangesForAdded(
                            entityInfo,
                            modelConfiguration,
                            entitiesIdMap,
                            dependencyGraph,
                            sessionProvider);
                        break;
                    case EntityState.Modified:
                    case EntityState.Deleted:
                        ApplyChangesForModified(
                            entityInfo,
                            modelConfiguration,
                            entitiesIdMap,
                            dependencyGraph,
                            sessionProvider);
                        break;
                    default:
                        continue;
                }
            }
        }

        private void ApplyChangesForModified(
            EntityInfo entityInfo,
            ModelConfiguration modelConfiguration,
            EntityIdMap entitiesIdMap,
            DependencyGraph dependencyGraph,
            ConfiguredSessionProvider sessionProvider)
        {
            var dbEntity = entityInfo.Entity;
            var clientEntity = entityInfo.ClientEntity;
            var metadata = entityInfo.EntityMetadata;
            var persister = metadata.EntityPersister;
            var propertyTypes = persister.PropertyTypes;
            HashSet<string> skipProperties = null;
            HashSet<Association> updatedAssociations = null;

            // Update modified values
            var additionalProperties = GetAdditionalProperties(metadata);
            foreach (var pair in entityInfo.OriginalValuesMap)
            {
                var propertyName = pair.Key;
                if (metadata.VersionPropertyName == propertyName)
                {
                    // JSON.NET converts integers to long
                    var oldVersion = ConvertToType(pair.Value, metadata.VersionPropertyType);
                    var currentVersion = persister.GetPropertyValue(dbEntity, persister.VersionProperty);
                    if (!Equals(oldVersion, currentVersion))
                    {
                        var errors = new[]
                        {
                                new EntityError
                                {
                                    EntityTypeName = entityInfo.EntityType.FullName,
                                    ErrorMessage = "Cannot update an old version",
                                    ErrorName = "ConcurrencyException",
                                    KeyValues = entityInfo.GetIdentifierValues(),
                                    PropertyName = propertyName
                                }
                            };

                        throw new EntityErrorsException("Cannot update an old version", errors);
                    }

                    continue;
                }

                if (skipProperties?.Contains(propertyName) == true ||
                    !CanUpdateProperty(metadata, modelConfiguration, propertyName, clientEntity))
                {
                    continue;
                }

                // Find FK property relation (e.g. OrderId -> Order)
                if (metadata.ForeignKeyAssociations.TryGetValue(propertyName, out var association))
                {
                    updatedAssociations ??= new HashSet<Association>();
                    updatedAssociations.Add(association);
                    UpdateEntityAssociation(
                        association,
                        entityInfo,
                        persister,
                        entitiesIdMap,
                        dependencyGraph,
                        sessionProvider);

                    if (association.ForeignKeyPropertyNames.Count > 1)
                    {
                        skipProperties ??= new HashSet<string>();
                        foreach (var fkProperty in association.ForeignKeyPropertyNames)
                        {
                            skipProperties.Add(fkProperty);
                        }
                    }
                }
                else
                {
                    var index = persister.EntityMetamodel.GetPropertyIndexOrNull(propertyName);
                    if (!index.HasValue)
                    {
                        if (additionalProperties.TryGetValue(propertyName, out var additionalProperty))
                        {
                            additionalProperty.SetValue(dbEntity, additionalProperty.GetValue(clientEntity));
                        }

                        continue; // Not mapped property
                    }

                    var propertyType = propertyTypes[index.Value];
                    var propertyValue = persister.GetPropertyValue(clientEntity, index.Value);
                    if (propertyType is IAbstractComponentType componentType)
                    {
                        var dbValue = persister.GetPropertyValue(dbEntity, index.Value);
                        persister.SetPropertyValue(
                            dbEntity,
                            index.Value,
                            UpdateComponentProperties(componentType, propertyValue, dbValue, (JObject)pair.Value));
                    }
                    else
                    {
                        persister.SetPropertyValue(dbEntity, index.Value, propertyValue);
                    }
                }
            }

            var remove = entityInfo.EntityState == EntityState.Deleted;
            // Add dependencies to graph
            foreach (var association in metadata.Associations.Values)
            {
                if (updatedAssociations?.Contains(association) == true ||
                    !(association is EntityAssociation entityAssociation) ||
                    !entityAssociation.IsChild)
                {
                    continue;
                }

                var newFkValue = GetClientForeignKeyValue(entityAssociation, entityInfo, entitiesIdMap, false, sessionProvider);
                // Update association only when removing, otherwise only add it to the graph
                TryAddToGraphAndUpdateParent(entityInfo, entitiesIdMap, dependencyGraph, newFkValue, entityAssociation, remove, remove, sessionProvider, out _);
            }
        }

        private void ApplyChangesForAdded(
            EntityInfo entityInfo,
            ModelConfiguration modelConfiguration,
            EntityIdMap entitiesIdMap,
            DependencyGraph dependencyGraph,
            ConfiguredSessionProvider sessionProvider)
        {
            var dbEntity = entityInfo.Entity;
            var metadata = entityInfo.EntityMetadata;
            var persister = metadata.EntityPersister;
            var clientEntity = entityInfo.ClientEntity;
            var propertyNames = persister.PropertyNames;
            var propertyTypes = persister.PropertyTypes;

            // Identifier is set in SetupDatabaseEntities 
            for (var i = 0; i < propertyTypes.Length; i++)
            {
                var propertyName = propertyNames[i];
                if (propertyTypes[i].IsCollectionType || persister.VersionProperty == i)
                {
                    continue;
                }

                if (!CanUpdateProperty(modelConfiguration, propertyName, clientEntity))
                {
                    continue;
                }

                if (metadata.Associations.TryGetValue(propertyName, out var association) && association is EntityAssociation entityAssociation)
                {
                    UpdateEntityAssociation(
                        entityAssociation,
                        entityInfo,
                        persister,
                        entitiesIdMap,
                        dependencyGraph,
                        sessionProvider);
                }
                else
                {
                    persister.SetPropertyValue(dbEntity, i, persister.GetPropertyValue(clientEntity, i));
                }
            }

            // Add key-many-to-one to graph and on the parent collection
            if (metadata.ManyToOneIdentifierProperties.Count > 0)
            {
                foreach (var pair in metadata.ManyToOneIdentifierProperties)
                {
                    var pkPropertyName = pair.Key;
                    var entityAssociation = (EntityAssociation)metadata.Associations[pkPropertyName];
                    var fkValue = GetClientForeignKeyValue(entityAssociation, entityInfo, entitiesIdMap, false, sessionProvider);
                    TryAddToGraphAndUpdateParent(entityInfo, entitiesIdMap, dependencyGraph, fkValue, entityAssociation, false, true, sessionProvider, out _);
                }
            }

            foreach (var additionalProperty in GetAdditionalProperties(metadata).Values)
            {
                additionalProperty.SetValue(dbEntity, additionalProperty.GetValue(clientEntity));
            }
        }

        private void AddToGraphAdditionalEntityAssociations(
            EntityInfo entityInfo,
            Dictionary<Type, List<EntityInfo>> saveMap,
            DependencyGraph dependencyGraph)
        {
            var dbEntity = entityInfo.Entity;
            var metadata = entityInfo.EntityMetadata;
            var persister = metadata.EntityPersister;
            foreach (var association in metadata.Associations.Values)
            {
                if (!(association is EntityAssociation entityAssociation) || !entityAssociation.IsChild)
                {
                    continue;
                }

                var parentEntity = persister.GetPropertyValue(dbEntity, entityAssociation.AssociationPropertyName);
                if (parentEntity == null || !saveMap.TryGetValue(entityAssociation.EntityType, out var entitiesInfo))
                {
                    continue;
                }

                var parentEntityInfO = entitiesInfo.Find(o => o.Entity == parentEntity);
                dependencyGraph.AddToGraph(entityInfo, parentEntityInfO, entityAssociation);
            }
        }

        private void RefreshFromSession(Dictionary<Type, List<EntityInfo>> saveMap, ConfiguredSessionProvider sessionProvider)
        {
            foreach (var pair in saveMap)
            {
                var modelConfiguration = _breezeConfigurator.GetModelConfiguration(pair.Key);
                if (modelConfiguration.RefreshAfterSave != true && modelConfiguration.RefreshAfterUpdate != true)
                {
                    continue;
                }

                var session = sessionProvider.GetSession(pair.Key);
                foreach (var entityInfo in pair.Value)
                {
                    if (entityInfo.EntityState == EntityState.Added && modelConfiguration.RefreshAfterSave == true ||
                        entityInfo.EntityState == EntityState.Modified && modelConfiguration.RefreshAfterUpdate == true)
                    {
                        session.Refresh(entityInfo.Entity);
                    }
                }
            }
        }

        /// <summary>
        /// Creates <see cref="EntityInfo"/> for each additional entity and adds them to the save map. As for <see cref="EntityIdMap"/>, we
        /// cannot add them there are they may have default identifier values (e.g. 0).
        /// </summary>
        /// <param name="entities">The additional entities.</param>
        /// <param name="saveMap">The save map.</param>
        private void AddAdditionalEntities(
            Dictionary<object, EntityState> entities,
            Dictionary<Type, List<EntityInfo>> saveMap)
        {
            if (entities == null)
            {
                return;
            }

            foreach (var group in entities.GroupBy(t => GetEntityType(t.Key)))
            {
                var entityType = group.Key;
                if (!_entityMetadataProvider.IsEntityType(entityType))
                {
                    throw new InvalidOperationException($"Metadata for additional entity type {entityType} was not found.");
                }

                var metadata = _entityMetadataProvider.GetMetadata(entityType);
                var persister = metadata.EntityPersister;
                foreach (var pair in group)
                {
                    var clientEntity = metadata.EntityPersister.EntityTuplizer.Instantiate();
                    var dbEntity = pair.Key;
                    var state = pair.Value;
                    var id = persister.GetIdentifier(dbEntity);
                    persister.SetIdentifier(clientEntity, id);
                    var entityInfo = new EntityInfo(metadata, clientEntity)
                    {
                        EntityState = state,
                        Entity = dbEntity,
                        ServerSide = true
                    };
                    if (!saveMap.TryGetValue(entityType, out var entitiesInfo))
                    {
                        entitiesInfo = new List<EntityInfo>();
                        saveMap.Add(entityType, entitiesInfo);
                    }

                    entitiesInfo.Add(entityInfo);
                }
            }
        }

        private Dictionary<string, AdditionalProperty> GetAdditionalProperties(EntityMetadata metadata)
        {
            return TypeAdditionalProperties.GetOrAdd(metadata.Type, t => CreateAdditionalProperties(t, metadata));
        }

        private Dictionary<string, AdditionalProperty> CreateAdditionalProperties(Type entityType, EntityMetadata metadata)
        {
            var modelConfigurator = _breezeConfigurator.GetModelConfiguration(entityType);
            var members = modelConfigurator.Members.Values
                .Where(o => o.Serialize != false && o.Ignored == false)
                .Select(o => o.MemberInfo)
                .Where(o => o.CanSetMemberValue(true, false) && !metadata.AllProperties.Contains(o.Name))
                .ToList();

            if (members.Count == 0)
            {
                return DefaultAdditionalProperties;
            }

            var entityParam = Expression.Parameter(typeof(object));
            var valueParam = Expression.Parameter(typeof(object));
            var result = new Dictionary<string, AdditionalProperty>(members.Count);
            foreach (var member in members)
            {
                var memberExpression = Expression.MakeMemberAccess(Expression.Convert(entityParam, entityType), member);
                result.Add(member.Name, new AdditionalProperty(
                    Expression.Lambda<Func<object, object>>(
                        Expression.Convert(memberExpression, typeof(object)),
                        entityParam).Compile(),
                    Expression.Lambda<Action<object, object>>(
                        Expression.Assign(memberExpression,
                            Expression.Convert(valueParam, member.GetUnderlyingType())),
                        entityParam,
                        valueParam).Compile()
                ));
            }

            return result;
        }

        private void UpdateEntityAssociation(
            EntityAssociation association,
            EntityInfo entityInfo,
            AbstractEntityPersister persister,
            EntityIdMap entitiesIdMap,
            DependencyGraph dependencyGraph,
            ConfiguredSessionProvider sessionProvider)
        {
            var dbEntity = entityInfo.Entity;
            var originalFkValue = GetClientForeignKeyValue(association, entityInfo, entitiesIdMap, true, sessionProvider);
            TryAddToGraphAndUpdateParent(entityInfo, entitiesIdMap, dependencyGraph, originalFkValue, association, true, true, sessionProvider, out _);

            var newFkValue = GetClientForeignKeyValue(association, entityInfo, entitiesIdMap, false, sessionProvider);
            TryAddToGraphAndUpdateParent(entityInfo, entitiesIdMap, dependencyGraph, newFkValue, association, false, true, sessionProvider, out var parentEntityInfo);

            // Set the entity association (e.g. Order = value)
            var associatedEntity = newFkValue != null
                ? parentEntityInfo?.Entity ?? LoadEntity(association.EntityType, newFkValue, sessionProvider)
                : null;
            persister.SetPropertyValue(
                dbEntity,
                persister.GetPropertyIndex(association.AssociationPropertyName),
                associatedEntity);
        }

        private void TryAddToGraphAndUpdateParent(
            EntityInfo entityInfo,
            EntityIdMap entitiesIdMap,
            DependencyGraph dependencyGraph,
            object fkValue,
            EntityAssociation association,
            bool remove,
            bool updateParent,
            ConfiguredSessionProvider sessionProvider,
            out EntityInfo parentEntityInfo)
        {
            if (fkValue == null)
            {
                parentEntityInfo = null;
                return;
            }

            object dbParent = null;
            if (entitiesIdMap.TryGetValue(association.EntityType, out var idMap) &&
                idMap.TryGetValue(fkValue, out parentEntityInfo))
            {
                dbParent = parentEntityInfo.Entity;
                if (association.IsChild)
                {
                    dependencyGraph.AddToGraph(entityInfo, parentEntityInfo, association);
                }
            }
            else
            {
                parentEntityInfo = null;
            }

            if (association.InverseAssociationPropertyName == null || !updateParent)
            {
                return;
            }

            if (dbParent == null)
            {
                // Try find the parent from the session in case it was lazy loaded when the child was retrieved due to the specific mapping
                parentEntityInfo = null;
                var relatedPersister = _entityMetadataProvider.GetMetadata(association.EntityType).EntityPersister;
                var session = sessionProvider.GetSession(association.EntityType);
                // For remove, in order to avoid a cascade save operation on a deleted entity (occurs when parent is loaded in
                // before save callback), we remove the association even if it is not loaded.
                dbParent = remove
                    ? session.Get(relatedPersister.EntityName, fkValue)
                    : session.GetSessionImplementation().PersistenceContext
                        .GetEntity(new global::NHibernate.Engine.EntityKey(fkValue, relatedPersister));
            }

            if (dbParent == null)
            {
                 return;
            }

            if (remove)
            {
                // Do not remove a non deleted child from a collection that will delete it, in order to
                // avoid exception: deleted object would be re-saved by cascade
                if (entityInfo.EntityState != EntityState.Deleted &&
                    association.InverseAssociationPropertyCascadeStyle.HasOrphanDelete)
                {
                    return;
                }

                // Remove it from the parent collection/entity
                association.RemoveFromParent(entityInfo.Entity, dbParent);
            }
            else
            {
                // Add it to the parent collection/entity
                association.AddToParent(entityInfo.Entity, dbParent);
            }
        }

        private object GetClientForeignKeyValue(
            EntityAssociation association,
            EntityInfo entityInfo,
            EntityIdMap entitiesIdMap,
            bool original,
            ConfiguredSessionProvider sessionProvider)
        {
            var metadata = entityInfo.EntityMetadata;
            if (association.IsOneToOne)
            {
                // TODO: verify this block
                if (original)
                {
                    return entityInfo.OriginalValuesMap != null && entityInfo.OriginalValuesMap.TryGetValue(metadata.EntityPersister.IdentifierPropertyName, out var originalValue)
                        ? ConvertToType(originalValue, association.IdentifierType.ReturnedClass)
                        : null;
                }

                return metadata.EntityPersister.GetIdentifier(entityInfo.ClientEntity);
            }

            var fkProperties = association.ForeignKeyPropertyNames;
            if (association.IdentifierType is ComponentType idComponentType)
            {
                return GetClientCompositeKeyValue(association, idComponentType, entityInfo, metadata, entitiesIdMap, original, sessionProvider);
            }

            var fkPropertyName = fkProperties[0];
            if (original)
            {
                return entityInfo.OriginalValuesMap != null && entityInfo.OriginalValuesMap.TryGetValue(fkPropertyName, out var originalValue)
                    ? ConvertToType(originalValue, association.IdentifierType.ReturnedClass)
                    : null;
            }

            if (metadata.SyntheticForeignKeyProperties.ContainsKey(fkPropertyName))
            {
                // The unmapped property may be ignored from metadata
                return entityInfo.UnmappedValuesMap != null && entityInfo.UnmappedValuesMap.TryGetValue(fkPropertyName, out var value)
                    ? ConvertToType(value, association.IdentifierType.ReturnedClass)
                    : null;
            }

            var persister = metadata.EntityPersister;
            return persister.GetPropertyValue(entityInfo.ClientEntity, persister.GetPropertyIndex(fkPropertyName));
        }

        private object GetClientCompositeKeyValue(
            EntityAssociation association,
            ComponentType idComponentType,
            EntityInfo entityInfo,
            EntityMetadata metadata,
            EntityIdMap entitiesIdMap,
            bool original,
            ConfiguredSessionProvider sessionProvider)
        {
            var persister = metadata.EntityPersister;
            var types = idComponentType.Subtypes;
            var relatedMetadata = _entityMetadataProvider.GetMetadata(association.EntityType);
            var values = new object[types.Length];
            var fkIndex = 0;
            var counter = 0;
            for (var i = 0; i < types.Length; i++)
            {
                var type = types[i];
                var columnSpan = type.GetColumnSpan(persister.Factory);
                var propertyValues = new object[columnSpan];
                for (var j = 0; j < columnSpan; j++)
                {
                    var fkProperty = association.ForeignKeyPropertyNames[fkIndex];
                    if (original && entityInfo.OriginalValuesMap != null && entityInfo.OriginalValuesMap.TryGetValue(fkProperty, out var originalValue))
                    {
                        propertyValues[j] = ConvertToType(originalValue, relatedMetadata.IdentifierPropertyTypes[fkIndex]);
                        counter++;
                    }
                    else if (metadata.SyntheticForeignKeyProperties.ContainsKey(fkProperty))
                    {
                        if (entityInfo.UnmappedValuesMap == null || !entityInfo.UnmappedValuesMap.TryGetValue(fkProperty, out var fkValue))
                        {
                            throw new InvalidOperationException($"Unable to retrieve the synthetic property '{fkProperty}' value for type {entityInfo.EntityType} from {nameof(EntityInfo.UnmappedValuesMap)}.");
                        }

                        propertyValues[j] = ConvertToType(fkValue, relatedMetadata.IdentifierPropertyTypes[fkIndex]);
                    }
                    else
                    {
                        propertyValues[j] = persister.GetPropertyValue(entityInfo.ClientEntity, persister.GetPropertyIndex(fkProperty));
                    }

                    fkIndex++;
                }

                if (type.IsAssociationType && type is EntityType entityType)
                {
                    var relatedPersister = (IEntityPersister)entityType.GetAssociatedJoinable(persister.Factory);
                    object propertyKey;
                    if (relatedPersister.IdentifierType is ComponentType componentType)
                    {
                        // TODO: add support for a relation to another key-many-to-one
                        propertyKey = componentType.Instantiate();
                        componentType.SetPropertyValues(propertyKey, propertyValues);
                    }
                    else
                    {
                        propertyKey = propertyValues[0];
                    }

                    if (entitiesIdMap.TryGetValue(relatedPersister.MappedClass, out var idMap) && idMap.TryGetValue(propertyKey, out var relatedEntityInfo))
                    {
                        values[i] = relatedEntityInfo.Entity;
                    }
                    else
                    {
                        values[i] = LoadEntity(relatedPersister.MappedClass, propertyKey, sessionProvider);
                    }
                }
                else
                {
                    values[i] = propertyValues[0];
                }
            }

            if (original && counter == 0)
            {
                return null; // The key was not changed
            }

            var key = idComponentType.Instantiate();
            idComponentType.SetPropertyValues(key, values);

            return key;
        }

        private static void ProcessSaves(
            List<EntityInfo> saveOrder,
            PersistenceManager persistenceManager,
            SaveChangesContext context,
            ISaveChangesOptions saveChangesOptions,
            ConfiguredSessionProvider sessionProvider)
        {
            foreach (var entityInfo in saveOrder)
            {
                persistenceManager.BeforeSaveEntityChanges(entityInfo, context);
                saveChangesOptions?.BeforeSaveEntityChanges(entityInfo, context);

                var session = sessionProvider.GetSession(entityInfo.EntityType);
                try
                {
                    switch (entityInfo.EntityState)
                    {
                        case EntityState.Modified:
                            session.Update(entityInfo.Entity);
                            break;
                        case EntityState.Added:
                            session.Save(entityInfo.Entity);
                            break;
                        case EntityState.Deleted:
                            session.Delete(entityInfo.Entity);
                            break;
                    }
                }
                catch (PropertyValueException e)
                {
                    // NH can throw this when a not null property is null or transient (e.g. not-null property references a null or transient value)
                    var errors = new[]
                    {
                        // KeyValues cannot be determined as the exception may reference another entity
                        new EntityError
                        {
                            EntityTypeName = e.EntityName,
                            ErrorMessage = e.Message,
                            ErrorName = "PropertyValueException",
                            PropertyName = e.PropertyName
                        }
                    };

                    throw new EntityErrorsException(e.Message, errors);
                }
            }
        }

        private static void FlushSessions(ConfiguredSessionProvider sessionProvider)
        {
            foreach (var session in sessionProvider.GetSessions())
            {
                session.Flush();
            }
        }

        private static IEnumerable<KeyMapping> GetKeyMappings(Dictionary<Type, List<EntityInfo>> saveMap)
        {
            foreach (var pair in saveMap)
            {
                var entityType = pair.Key;
                foreach (var entityInfo in pair.Value)
                {
                    if (entityInfo.AutoGeneratedKey == null ||
                        entityInfo.AutoGeneratedKey.AutoGeneratedKeyType == AutoGeneratedKeyType.None)
                    {
                        break;
                    }

                    if (entityInfo.EntityState != EntityState.Added)
                    {
                        continue;
                    }

                    yield return new KeyMapping(
                        entityType.FullName,
                        entityInfo.EntityMetadata.GetIdentifier(entityInfo.ClientEntity),
                        entityInfo.EntityMetadata.GetIdentifier(entityInfo.Entity));
                }
            }
        }

        private static object LoadEntity(Type entityType, object id, ConfiguredSessionProvider sessionProvider)
        {
            var session = sessionProvider.GetSession(entityType);
            return session.Load(entityType, id);
        }

        private static object UpdateComponentProperties(
            IAbstractComponentType componentType,
            object clientComponent,
            object dbComponent,
            JObject originalPropertiesObject)
        {
            if (dbComponent == null || clientComponent == null)
            {
                return null;
            }

            // TODO: cache
            var propertyMap = new Dictionary<string, int>(componentType.PropertyNames.Length);
            var propertyNames = componentType.PropertyNames;
            for (var i = 0; i < propertyNames.Length; i++)
            {
                propertyMap.Add(propertyNames[i], i);
            }

            var dbValues = componentType.GetPropertyValues(dbComponent);
            var clientValues = componentType.GetPropertyValues(clientComponent);
            foreach (var property in originalPropertiesObject.Properties())
            {
                if (!propertyMap.TryGetValue(property.Name, out var index))
                {
                    continue; // Not mapped property
                }

                var propertyType = componentType.Subtypes[index];
                if (propertyType is IAbstractComponentType subComponentType)
                {
                    dbValues[index] = UpdateComponentProperties(subComponentType, clientValues[index], dbValues[index], (JObject)property.Value);
                }
                else
                {
                    dbValues[index] = clientValues[index];
                }
            }

            componentType.SetPropertyValues(dbComponent, dbValues);

            return dbComponent;
        }

        private static int CompareSaveMapTypes(KeyValuePair<Type, List<EntityInfo>> x, KeyValuePair<Type, List<EntityInfo>> y)
        {
            EntityMetadata metadataX;
            if (x.Value.Count > 0)
            {
                metadataX = x.Value[0].EntityMetadata;
                if (metadataX.ManyToOneIdentifierProperties.Count == 0)
                {
                    return -1;
                }
            }
            else
            {
                return -1;
            }

            EntityMetadata metadataY;
            if (y.Value.Count > 0)
            {
                metadataY = y.Value[0].EntityMetadata;
                if (metadataY.ManyToOneIdentifierProperties.Count == 0)
                {
                    return 1;
                }
            }
            else
            {
                return 1;
            }

            foreach (var pkPropertyName in metadataX.ManyToOneIdentifierProperties.Keys)
            {
                if (metadataY.Type == metadataX.Associations[pkPropertyName].EntityType)
                {
                    return 1;
                }
            }

            foreach (var pkPropertyName in metadataY.ManyToOneIdentifierProperties.Keys)
            {
                if (metadataX.Type == metadataY.Associations[pkPropertyName].EntityType)
                {
                    return -1;
                }
            }

            return 0;
        }

        private static bool CanUpdateProperty(EntityMetadata metadata, ModelConfiguration modelConfiguration, string propertyName, object entity)
        {
            if (!metadata.SyntheticForeignKeyProperties.TryGetValue(propertyName, out var syntheticProperty))
            {
                return !modelConfiguration.SyntheticMembers.ContainsKey(propertyName) && CanUpdateProperty(modelConfiguration, propertyName, entity);
            }

            if (modelConfiguration.SyntheticMembers.TryGetValue(propertyName, out var syntheticMemberConfiguration) && syntheticMemberConfiguration.Ignored.HasValue)
            {
                return syntheticMemberConfiguration.Ignored.Value;
            }

            return CanUpdateProperty(modelConfiguration, syntheticProperty.AssociationPropertyName, entity);
        }

        private static bool CanUpdateProperty(ModelConfiguration modelConfiguration, string propertyName, object entity)
        {
            var memberConfiguration = modelConfiguration.GetMember(propertyName);
            if (memberConfiguration == null)
            {
                return true;
            }

            return memberConfiguration.Ignored != true &&
                   memberConfiguration.Deserialize != false &&
                   memberConfiguration.ShouldDeserializePredicate?.Invoke(entity) != false;
        }
    }
}
