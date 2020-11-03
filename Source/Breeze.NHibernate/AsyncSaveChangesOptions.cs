using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Breeze.NHibernate.Internal;

namespace Breeze.NHibernate
{
    /// <summary>
    /// A delegate called before an async save operation.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
    public delegate Task AsyncBeforeSaveOperationDelegate(SaveChangesContext context, CancellationToken cancellationToken);

    /// <summary>
    /// A delegate representing an async save operation.
    /// </summary>
    /// <param name="saveOrder">The save order.</param>
    /// <param name="context">The context.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
    public delegate Task AsyncSaveOperationDelegate(List<EntityInfo> saveOrder, SaveChangesContext context, CancellationToken cancellationToken);

    /// <summary>
    /// A delegate that can decide whether the entity will be saved.
    /// </summary>
    /// <param name="entityInfo">The entity info.</param>
    /// <param name="context">The context.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
    public delegate Task AsyncBeforeSaveEntityChangesDelegate(EntityInfo entityInfo, SaveChangesContext context, CancellationToken cancellationToken);

    /// <summary>
    /// A delegate that is called after all changes are flushed into the database.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="keyMappings">The key mappings</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
    public delegate Task AsyncAfterFlushChangesDelegate(SaveChangesContext context, List<KeyMapping> keyMappings, CancellationToken cancellationToken);

    /// <summary>
    /// A configuration used to configure <see cref="PersistenceManager.SaveChangesAsync(SaveBundle,CancellationToken)"/> method.
    /// </summary>
    public class AsyncSaveChangesOptions : ISaveChangesOptions
    {
        internal static readonly AsyncSaveChangesOptions Default = new AsyncSaveChangesOptions();

        /// <summary>
        /// A delegate that is called after an <see cref="EntityInfo"/> is created.
        /// </summary>
        public AfterCreateEntityInfoDelegate AfterCreateEntityInfoAction { get; set; }

        /// <summary>
        /// A delegate that will be invoked before fetching entities from the database.
        /// </summary>
        public AsyncBeforeSaveOperationDelegate BeforeFetchEntitiesAction { get; set; }

        /// <summary>
        /// A delegate that validates the dependency graph.
        /// </summary>
        public ValidateDependencyGraphDelegate ValidateDependencyGraphAction { get; set; }

        /// <summary>
        /// A delegate that will be invoked before applying the changes made by the breeze client.
        /// </summary>
        public AsyncBeforeSaveOperationDelegate BeforeApplyChangesAction { get; set; }

        /// <summary>
        /// A delegate that will be invoked before saving entities.
        /// </summary>
        public AsyncSaveOperationDelegate BeforeSaveChangesAction { get; set; }

        /// <summary>
        /// A delegate that will be invoked before saving an entity.
        /// </summary>
        public AsyncBeforeSaveEntityChangesDelegate BeforeSaveEntityChangesAction { get; set; }

        /// <summary>
        /// A delegate that will be invoked after saving entities.
        /// </summary>
        public AsyncSaveOperationDelegate AfterSaveChangesAction { get; set; }

        /// <summary>
        /// A delegate that will be invoked after all changes are flushed into the database.
        /// </summary>
        public AsyncAfterFlushChangesDelegate AfterFlushChangesAction { get; set; }

        void ISaveChangesOptions.ValidateDependencyGraph(DependencyGraph dependencyGraph, SaveChangesContext context)
        {
            ValidateDependencyGraphAction?.Invoke(dependencyGraph, context);
        }

        void ISaveChangesOptions.BeforeFetchEntities(SaveChangesContext context)
        {
            throw new NotSupportedException();
        }

        Task ISaveChangesOptions.BeforeFetchEntitiesAsync(SaveChangesContext context, CancellationToken cancellationToken)
        {
            return BeforeFetchEntitiesAction?.Invoke(context, cancellationToken);
        }

        void ISaveChangesOptions.BeforeApplyChanges(SaveChangesContext context)
        {
            throw new NotSupportedException();
        }

        Task ISaveChangesOptions.BeforeApplyChangesAsync(SaveChangesContext context, CancellationToken cancellationToken)
        {
            return BeforeApplyChangesAction?.Invoke(context, cancellationToken) ?? Task.CompletedTask;
        }

        void ISaveChangesOptions.BeforeSaveChanges(List<EntityInfo> saveOrder, SaveChangesContext context)
        {
            throw new NotSupportedException();
        }

        Task ISaveChangesOptions.BeforeSaveChangesAsync(List<EntityInfo> saveOrder, SaveChangesContext context, CancellationToken cancellationToken)
        {
            return BeforeSaveChangesAction?.Invoke(saveOrder, context, cancellationToken) ?? Task.CompletedTask;
        }

        void ISaveChangesOptions.AfterSaveChanges(List<EntityInfo> saveOrder, SaveChangesContext context)
        {
            throw new NotSupportedException();
        }

        Task ISaveChangesOptions.AfterSaveChangesAsync(List<EntityInfo> saveOrder, SaveChangesContext context, CancellationToken cancellationToken)
        {
            return AfterSaveChangesAction?.Invoke(saveOrder, context, cancellationToken) ?? Task.CompletedTask;
        }

        void ISaveChangesOptions.AfterFlushChanges(SaveChangesContext context, List<KeyMapping> keyMappings)
        {
            throw new NotSupportedException();
        }

        Task ISaveChangesOptions.AfterFlushChangesAsync(SaveChangesContext context, List<KeyMapping> keyMappings, CancellationToken cancellationToken)
        {
            return AfterFlushChangesAction?.Invoke(context, keyMappings, cancellationToken);
        }

        void ISaveChangesOptions.BeforeSaveEntityChanges(EntityInfo entityInfo, SaveChangesContext context)
        {
            throw new NotSupportedException();
        }

        Task ISaveChangesOptions.BeforeSaveEntityChangesAsync(EntityInfo entityInfo, SaveChangesContext context, CancellationToken cancellationToken)
        {
            return BeforeSaveEntityChangesAction?.Invoke(entityInfo, context, cancellationToken) ?? Task.CompletedTask;
        }
    }
}
