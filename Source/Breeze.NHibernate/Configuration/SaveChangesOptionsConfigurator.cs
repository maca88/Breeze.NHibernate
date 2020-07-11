using System;
using System.Collections.Generic;
using System.Linq;

namespace Breeze.NHibernate.Configuration
{
    /// <summary>
    /// A delegate representing a save operation.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="entityInfo">The entity info.</param>
    /// <param name="saveOrder">The save order.</param>
    /// <param name="context">The context.</param>
    public delegate void ModelSaveOperationDelegate<in TEntity>(TEntity entity, EntityInfo entityInfo, List<EntityInfo> saveOrder, SaveChangesContext context);

    /// <summary>
    /// A delegate called before a save operation.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="entityInfo">The entity info.</param>
    /// <param name="context">The context.</param>
    public delegate void ModelBeforeApplyChangesDelegate<in TEntity>(TEntity entity, EntityInfo entityInfo, SaveChangesContext context);

    /// <summary>
    /// A delegate called after flush entities changes.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="entityInfo">The entity info.</param>
    /// <param name="context">The context.</param>
    /// <param name="keyMappings">The key mappings.</param>
    public delegate void ModelAfterFlushChangesDelegate<in TEntity>(TEntity entity, EntityInfo entityInfo, SaveChangesContext context, List<KeyMapping> keyMappings);

    /// <summary>
    /// Fluent api for configure <see cref="SaveChangesOptions"/>.
    /// </summary>
    public class SaveChangesOptionsConfigurator
    {
        internal SaveChangesOptionsConfigurator()
        {
        }

        internal SaveChangesOptions SaveChangesOptions { get; } = new SaveChangesOptions();

        /// <summary>
        /// Set the delegate that will be invoked after an <see cref="EntityInfo"/> is created.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public SaveChangesOptionsConfigurator AfterCreateEntityInfo(AfterCreateEntityInfoDelegate action)
        {
            SaveChangesOptions.AfterCreateEntityInfoAction = action;
            return this;
        }

        /// <summary>
        /// Set the delegate that will be invoked before fetching entities from the database.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public SaveChangesOptionsConfigurator BeforeFetchEntities(BeforeSaveOperationDelegate action)
        {
            SaveChangesOptions.BeforeFetchEntitiesAction = action;
            return this;
        }

        /// <summary>
        /// Set the delegate that will be invoked before applying the changes made by the breeze client.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public SaveChangesOptionsConfigurator BeforeApplyChanges(BeforeSaveOperationDelegate action)
        {
            SaveChangesOptions.BeforeApplyChangesAction = action;
            return this;
        }

        /// <summary>
        /// Set the delegate that validates the dependency graph.
        /// </summary>
        /// <param name="action">The validation delegate.</param>
        public SaveChangesOptionsConfigurator ValidateDependencyGraph(ValidateDependencyGraphDelegate action)
        {
            SaveChangesOptions.ValidateDependencyGraphAction = action;
            return this;
        }

        /// <summary>
        /// Set the delegate that will be invoked before saving entities.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public SaveChangesOptionsConfigurator BeforeSaveChanges(SaveOperationDelegate action)
        {
            SaveChangesOptions.BeforeSaveChangesAction = action;
            return this;
        }

        /// <summary>
        /// Set the delegate that will be invoked before saving an entity.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public SaveChangesOptionsConfigurator BeforeSaveEntityChanges(BeforeSaveEntityChangesDelegate action)
        {
            SaveChangesOptions.BeforeSaveEntityChangesAction = action;
            return this;
        }

        /// <summary>
        /// Set the delegate that will be invoked after saving entities.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public SaveChangesOptionsConfigurator AfterSaveChanges(SaveOperationDelegate action)
        {
            SaveChangesOptions.AfterSaveChangesAction = action;
            return this;
        }

        /// <summary>
        /// Set the delegate that will be invoked after all changes are flushed into the database.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public SaveChangesOptionsConfigurator AfterFlushChanges(AfterFlushChangesDelegate action)
        {
            SaveChangesOptions.AfterFlushChangesAction = action;
            return this;
        }
    }

    /// <summary>
    /// Fluent api for configure <see cref="SaveChangesOptions"/>.
    /// </summary>
    public class SaveChangesOptionsConfigurator<TEntity>
    {
        private IModelSaveValidator _modelSaveValidator;
        private ModelSaveOperationDelegate<TEntity> _beforeSaveChangesAction;
        private ModelSaveOperationDelegate<TEntity> _afterSaveChangesAction;
        private ModelAfterFlushChangesDelegate<TEntity> _afterFlushChangesAction;
        private ModelBeforeApplyChangesDelegate<TEntity> _beforeApplyChangesAction;

        internal SaveChangesOptionsConfigurator(IModelSaveValidator modelSaveValidator)
        {
            _modelSaveValidator = modelSaveValidator;
            SaveChangesOptions.ValidateDependencyGraphAction = ValidateDependencyGraph;
        }

        internal SaveChangesOptions SaveChangesOptions { get; } = new SaveChangesOptions();

        /// <summary>
        /// Set the delegate that will be invoked after an <see cref="EntityInfo"/> is created.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public SaveChangesOptionsConfigurator<TEntity> AfterCreateEntityInfo(AfterCreateEntityInfoDelegate action)
        {
            SaveChangesOptions.AfterCreateEntityInfoAction = action;
            return this;
        }

        /// <summary>
        /// Set the delegate that will be invoked before fetching entities from the database.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public SaveChangesOptionsConfigurator<TEntity> BeforeFetchEntities(BeforeSaveOperationDelegate action)
        {
            SaveChangesOptions.BeforeFetchEntitiesAction = action;
            return this;
        }

        /// <summary>
        /// Set the delegate that will be invoked before applying the changes made by the breeze client.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public SaveChangesOptionsConfigurator<TEntity> BeforeApplyChanges(ModelBeforeApplyChangesDelegate<TEntity> action)
        {
            _beforeApplyChangesAction = action;
            SaveChangesOptions.BeforeApplyChangesAction = BeforeApplyChanges;
            return this;
        }

        /// <summary>
        /// Set the delegate that will be invoked before saving entities.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public SaveChangesOptionsConfigurator<TEntity> BeforeSaveChanges(ModelSaveOperationDelegate<TEntity> action)
        {
            _beforeSaveChangesAction = action;
            SaveChangesOptions.BeforeSaveChangesAction = BeforeSaveChanges;
            return this;
        }

        /// <summary>
        /// Set the delegate that will be invoked after saving entities.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public SaveChangesOptionsConfigurator<TEntity> AfterSaveChanges(ModelSaveOperationDelegate<TEntity> action)
        {
            _afterSaveChangesAction = action;
            SaveChangesOptions.AfterSaveChangesAction = AfterSaveChanges;
            return this;
        }

        /// <summary>
        /// Set the delegate that will be invoked after all changes are flushed into the database.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public SaveChangesOptionsConfigurator<TEntity> AfterFlushChanges(ModelAfterFlushChangesDelegate<TEntity> action)
        {
            _afterFlushChangesAction = action;
            SaveChangesOptions.AfterFlushChangesAction = AfterFlushChanges;
            return this;
        }

        /// <summary>
        /// Set the model validator that will be used to validate the dependency graph.
        /// </summary>
        /// <param name="validator">The model validator.</param>
        public SaveChangesOptionsConfigurator<TEntity> ModelSaveValidator(IModelSaveValidator validator)
        {
            _modelSaveValidator = validator;
            return this;
        }

        private void ValidateDependencyGraph(DependencyGraph dependencyGraph, SaveChangesContext context)
        {
            context.Data[nameof(IModelSaveValidator)] = _modelSaveValidator.Validate(typeof(TEntity), dependencyGraph).ToList();
        }

        private void BeforeApplyChanges(SaveChangesContext context)
        {
            if (!context.SaveMap.TryGetValue(typeof(TEntity), out var entityInfoList))
            {
                return;
            }

            foreach (var entityInfo in entityInfoList)
            {
                _beforeApplyChangesAction((TEntity)entityInfo.Entity, entityInfo, context);
            }
        }

        private void BeforeSaveChanges(List<EntityInfo> saveOrder, SaveChangesContext context)
        {
            var roots = (List<EntityInfo>)context.Data[nameof(IModelSaveValidator)];
            foreach (var root in roots)
            {
                _beforeSaveChangesAction((TEntity)root.Entity, root, saveOrder, context);
            }
        }

        private void AfterSaveChanges(List<EntityInfo> saveOrder, SaveChangesContext context)
        {
            var roots = (List<EntityInfo>)context.Data[nameof(IModelSaveValidator)];
            foreach (var root in roots)
            {
                _afterSaveChangesAction((TEntity)root.Entity, root, saveOrder, context);
            }
        }

        private void AfterFlushChanges(SaveChangesContext context, List<KeyMapping> keyMappings)
        {
            var roots = (List<EntityInfo>)context.Data[nameof(IModelSaveValidator)];
            foreach (var root in roots)
            {
                _afterFlushChangesAction((TEntity)root.Entity, root, context, keyMappings);
            }
        }
    }
}
