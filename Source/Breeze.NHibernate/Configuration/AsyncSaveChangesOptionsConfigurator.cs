using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Breeze.NHibernate.Configuration
{
    /// <summary>
    /// A delegate representing an async save operation.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="entityInfo">The entity info.</param>
    /// <param name="saveOrder">The save order.</param>
    /// <param name="context">The context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public delegate Task AsyncModelSaveOperationDelegate<in TEntity>(TEntity entity, EntityInfo entityInfo, List<EntityInfo> saveOrder, SaveChangesContext context, CancellationToken cancellationToken);

    /// <summary>
    /// A delegate called before an async save operation.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="entityInfo">The entity info.</param>
    /// <param name="context">The context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public delegate Task AsyncModelBeforeApplyChangesDelegate<in TEntity>(TEntity entity, EntityInfo entityInfo, SaveChangesContext context, CancellationToken cancellationToken);

    /// <summary>
    /// A delegate called after flush entities changes.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="entityInfo">The entity info.</param>
    /// <param name="context">The context.</param>
    /// <param name="keyMappings">The key mappings.</param>
    public delegate Task AsyncModelAfterFlushChangesDelegate<in TEntity>(TEntity entity, EntityInfo entityInfo, SaveChangesContext context, List<KeyMapping> keyMappings, CancellationToken cancellationToken);

    /// <summary>
    /// Fluent api for configure <see cref="AsyncSaveChangesOptions"/>.
    /// </summary>
    public class AsyncSaveChangesOptionsConfigurator
    {
        internal AsyncSaveChangesOptionsConfigurator()
        {
        }

        internal AsyncSaveChangesOptions SaveChangesOptions { get; } = new AsyncSaveChangesOptions();

        /// <summary>
        /// Set the delegate that will be invoked after an <see cref="EntityInfo"/> is created.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public AsyncSaveChangesOptionsConfigurator AfterCreateEntityInfo(AfterCreateEntityInfoDelegate action)
        {
            SaveChangesOptions.AfterCreateEntityInfoAction = action;
            return this;
        }

        /// <summary>
        /// Set the delegate that will be invoked before fetching entities from the database.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public AsyncSaveChangesOptionsConfigurator BeforeFetchEntities(AsyncBeforeSaveOperationDelegate action)
        {
            SaveChangesOptions.BeforeFetchEntitiesAction = action;
            return this;
        }

        /// <summary>
        /// Set the delegate that will be invoked before applying the changes made by the breeze client.
        /// </summary>
        /// <param name="action">The delegate to invoke</param>
        public AsyncSaveChangesOptionsConfigurator BeforeApplyChanges(AsyncBeforeSaveOperationDelegate action)
        {
            SaveChangesOptions.BeforeApplyChangesAction = action;
            return this;
        }

        /// <summary>
        /// Set the delegate that validates the dependency graph.
        /// </summary>
        /// <param name="action">The validation delegate</param>
        public AsyncSaveChangesOptionsConfigurator ValidateDependencyGraph(ValidateDependencyGraphDelegate action)
        {
            SaveChangesOptions.ValidateDependencyGraphAction = action;
            return this;
        }

        /// <summary>
        /// Set the delegate that will be invoked before saving entities.
        /// </summary>
        /// <param name="action">The delegate to invoke</param>
        public AsyncSaveChangesOptionsConfigurator BeforeSaveChanges(AsyncSaveOperationDelegate action)
        {
            SaveChangesOptions.BeforeSaveChangesAction = action;
            return this;
        }

        /// <summary>
        /// Set the delegate that will be invoked before saving an entity.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public AsyncSaveChangesOptionsConfigurator BeforeSaveEntityChanges(AsyncBeforeSaveEntityChangesDelegate action)
        {
            SaveChangesOptions.BeforeSaveEntityChangesAction = action;
            return this;
        }

        /// <summary>
        /// Set the delegate that will be invoked after saving entities.
        /// </summary>
        /// <param name="action">The delegate to invoke</param>
        public AsyncSaveChangesOptionsConfigurator AfterSaveChanges(AsyncSaveOperationDelegate action)
        {
            SaveChangesOptions.AfterSaveChangesAction = action;
            return this;
        }

        /// <summary>
        /// Set the delegate that will be invoked after all changes are flushed into the database.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public AsyncSaveChangesOptionsConfigurator AfterFlushChanges(AsyncAfterFlushChangesDelegate action)
        {
            SaveChangesOptions.AfterFlushChangesAction = action;
            return this;
        }
    }

    /// <summary>
    /// Fluent api for configure <see cref="AsyncSaveChangesOptions"/>.
    /// </summary>
    public class AsyncSaveChangesOptionsConfigurator<TEntity>
    {
        private IModelSaveValidator _modelSaveValidator;
        private AsyncModelSaveOperationDelegate<TEntity> _beforeSaveChangesAction;
        private AsyncModelSaveOperationDelegate<TEntity> _afterSaveChangesAction;
        private AsyncModelBeforeApplyChangesDelegate<TEntity> _beforeApplyChangesAction;
        private AsyncModelAfterFlushChangesDelegate<TEntity> _afterFlushChangesAction;

        internal AsyncSaveChangesOptionsConfigurator(IModelSaveValidator modelSaveValidator)
        {
            _modelSaveValidator = modelSaveValidator;
            SaveChangesOptions.ValidateDependencyGraphAction = ValidateDependencyGraph;
        }

        internal AsyncSaveChangesOptions SaveChangesOptions { get; } = new AsyncSaveChangesOptions();

        /// <summary>
        /// Set the delegate that will be invoked after an <see cref="EntityInfo"/> is created.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public AsyncSaveChangesOptionsConfigurator<TEntity> AfterCreateEntityInfo(AfterCreateEntityInfoDelegate action)
        {
            SaveChangesOptions.AfterCreateEntityInfoAction = action;
            return this;
        }

        /// <summary>
        /// Set the delegate that will be invoked before fetching entities from the database.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public AsyncSaveChangesOptionsConfigurator<TEntity> BeforeFetchEntities(AsyncBeforeSaveOperationDelegate action)
        {
            SaveChangesOptions.BeforeFetchEntitiesAction = action;
            return this;
        }

        /// <summary>
        /// Set the delegate that will be invoked before applying the changes made by the breeze client.
        /// </summary>
        /// <param name="action">The delegate to invoke</param>
        public AsyncSaveChangesOptionsConfigurator<TEntity> BeforeApplyChanges(AsyncModelBeforeApplyChangesDelegate<TEntity> action)
        {
            _beforeApplyChangesAction = action;
            SaveChangesOptions.BeforeApplyChangesAction = BeforeApplyChangesAsync;
            return this;
        }

        /// <summary>
        /// Set the delegate that will be invoked before saving entities.
        /// </summary>
        /// <param name="action">The delegate to invoke</param>
        public AsyncSaveChangesOptionsConfigurator<TEntity> BeforeSaveChanges(AsyncModelSaveOperationDelegate<TEntity> action)
        {
            _beforeSaveChangesAction = action;
            SaveChangesOptions.BeforeSaveChangesAction = BeforeSaveChangesAsync;
            return this;
        }

        /// <summary>
        /// Set the delegate that will be invoked after saving entities.
        /// </summary>
        /// <param name="action">The delegate to invoke</param>
        public AsyncSaveChangesOptionsConfigurator<TEntity> AfterSaveChanges(AsyncModelSaveOperationDelegate<TEntity> action)
        {
            _afterSaveChangesAction = action;
            SaveChangesOptions.AfterSaveChangesAction = AfterSaveChangesAsync;
            return this;
        }

        /// <summary>
        /// Set the delegate that will be invoked after all changes are flushed into the database.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public AsyncSaveChangesOptionsConfigurator<TEntity> AfterFlushChanges(AsyncModelAfterFlushChangesDelegate<TEntity> action)
        {
            _afterFlushChangesAction = action;
            SaveChangesOptions.AfterFlushChangesAction = AfterFlushChangesAsync;
            return this;
        }

        /// <summary>
        /// Set the model validator that will be used to validate the dependency graph.
        /// </summary>
        /// <param name="validator">The model validator.</param>
        public AsyncSaveChangesOptionsConfigurator<TEntity> ModelSaveValidator(IModelSaveValidator validator)
        {
            _modelSaveValidator = validator ?? throw new ArgumentNullException(nameof(validator));
            return this;
        }

        private void ValidateDependencyGraph(DependencyGraph dependencyGraph, SaveChangesContext context)
        {
            context.Data[nameof(IModelSaveValidator)] = _modelSaveValidator.Validate(typeof(TEntity), dependencyGraph).ToList();
        }

        private async Task BeforeApplyChangesAsync(SaveChangesContext context, CancellationToken cancellationToken)
        {
            if (!context.SaveMap.TryGetValue(typeof(TEntity), out var entityInfoList))
            {
                return;
            }

            foreach (var entityInfo in entityInfoList)
            {
                await _beforeApplyChangesAction((TEntity)entityInfo.Entity, entityInfo, context, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task BeforeSaveChangesAsync(List<EntityInfo> saveOrder, SaveChangesContext context, CancellationToken cancellationToken)
        {
            var roots = (List<EntityInfo>) context.Data[nameof(IModelSaveValidator)];
            foreach (var root in roots)
            {
                await _beforeSaveChangesAction((TEntity)root.Entity, root, saveOrder, context, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task AfterSaveChangesAsync(List<EntityInfo> saveOrder, SaveChangesContext context, CancellationToken cancellationToken)
        {
            var roots = (List<EntityInfo>)context.Data[nameof(IModelSaveValidator)];
            foreach (var root in roots)
            {
                await _afterSaveChangesAction((TEntity)root.Entity, root, saveOrder, context, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task AfterFlushChangesAsync(SaveChangesContext context, List<KeyMapping> keyMappings, CancellationToken cancellationToken)
        {
            var roots = (List<EntityInfo>)context.Data[nameof(IModelSaveValidator)];
            foreach (var root in roots)
            {
                await _afterFlushChangesAction((TEntity)root.Entity, root, context, keyMappings, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
