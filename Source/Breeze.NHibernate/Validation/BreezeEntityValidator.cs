using System;
using System.Collections.Generic;
using System.Linq;
using Breeze.NHibernate.Extensions;
using Breeze.NHibernate.Metadata;
using static Breeze.NHibernate.Internal.BreezeHelper;

namespace Breeze.NHibernate.Validation
{
    /// <summary>
    /// Validator that validates entities by using <see cref="IBreezeValidator"/> located in <see cref="BreezeMetadata"/>.
    /// </summary>
    public class BreezeEntityValidator
    {
        private readonly Dictionary<Type, StructuralType> _structuralTypes;

        /// <summary>
        /// Constructs an instance of <see cref="BreezeEntityValidator"/>.
        /// </summary>
        public BreezeEntityValidator(BreezeMetadata breezeMetadata)
        {
            _structuralTypes = breezeMetadata.StructuralTypes.ToDictionary(o => o.Type);
        }

        /// <summary>
        /// Validate all the entities in the saveMap.
        /// </summary>
        /// <param name="saveMap">Map of type to entities.</param>
        /// <param name="throwIfInvalid">If true, throws an EntityErrorsException if any entity is invalid</param>
        /// <exception cref="EntityErrorsException">Contains all the EntityErrors.  Only thrown if throwIfInvalid is true.</exception>
        /// <returns>List containing an EntityError for each failed validation.</returns>
        public List<EntityError> ValidateEntities(IReadOnlyDictionary<Type, List<EntityInfo>> saveMap, bool throwIfInvalid)
        {
            var entityErrors = new List<EntityError>();
            foreach (var kvp in saveMap)
            {
                foreach (var entityInfo in kvp.Value)
                {
                    ValidateEntity(entityInfo, entityErrors);
                }
            }

            if (throwIfInvalid && entityErrors.Any())
            {
                throw new EntityErrorsException(entityErrors);
            }

            return entityErrors;
        }

        /// <summary>
        /// Validates a single entity.
        /// Skips validation (returns true) if entity is marked Deleted.
        /// </summary>
        /// <param name="entityInfo">contains the entity to validate</param>
        /// <param name="entityErrors">An EntityError is added to this list for each error found in the entity</param>
        /// <returns>true if entity is valid, false if invalid.</returns>
        private bool ValidateEntity(EntityInfo entityInfo, List<EntityError> entityErrors)
        {
            if (entityInfo.EntityState == EntityState.Deleted)
            {
                return true;
            }

            bool isValid = true;
            var metadata = entityInfo.EntityMetadata;
            var entity = entityInfo.ClientEntity;
            var structuralType = _structuralTypes[entityInfo.EntityType];
            var dataProperties = structuralType.DataProperties;
            object[] keyValues = null;
            foreach (var dataProperty in dataProperties)
            {
                if (dataProperty.Validators == null || dataProperty.Validators.Count == 0)
                {
                    continue;
                }

                var propertyName = dataProperty.NameOnServer;
                object propertyValue;
                if (metadata.SyntheticForeignKeyProperties.TryGetValue(propertyName, out var syntheticProperty))
                {
                    propertyValue = ConvertToType(entityInfo.UnmappedValuesMap[propertyName], syntheticProperty.IdentifierType);
                }
                else
                {
                    var index = metadata.EntityPersister.EntityMetamodel.GetPropertyIndexOrNull(propertyName);
                    if (index.HasValue)
                    {
                        propertyValue = metadata.EntityPersister.GetPropertyValue(entity, index.Value);
                    }
                    else if (metadata.EntityPersister.IdentifierPropertyName == propertyName)
                    {
                        propertyValue = metadata.EntityPersister.GetIdentifier(entity);
                    }
                    else
                    {
                        var propertyInfo = entityInfo.EntityType.GetProperty(propertyName);
                        if (propertyInfo == null)
                        {
                            throw new InvalidOperationException($"Unable to find property {propertyName} on type {entityInfo.EntityType}");
                        }

                        propertyValue = propertyInfo.GetValue(entity);
                    }
                }

                foreach (var validator in dataProperty.Validators.OfType<IBreezeValidator>())
                {
                    var errorMessage = validator.Validate(propertyValue);
                    if (errorMessage == null)
                    {
                        continue;
                    }

                    if (keyValues == null)
                    {
                        keyValues = entityInfo.GetIdentifierValues();
                    }

                    entityErrors.Add(new EntityError
                    {
                        EntityTypeName = entityInfo.EntityType.FullName,
                        ErrorMessage = errorMessage,
                        ErrorName = "ValidationError",
                        KeyValues = keyValues,
                        PropertyName = propertyName
                    });

                    isValid = false;
                }
            }

            return isValid;
        }
    }
}
