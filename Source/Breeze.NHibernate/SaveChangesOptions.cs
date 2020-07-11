using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Breeze.NHibernate.Configuration;
using Breeze.NHibernate.Internal;

namespace Breeze.NHibernate
{
    /// <summary>
    /// A delegate called before a save operation.
    /// </summary>
    /// <param name="context"></param>
    public delegate void BeforeSaveOperationDelegate(SaveChangesContext context);

    /// <summary>
    /// A delegate that validates the <see cref="DependencyGraph"/>.
    /// </summary>
    /// <param name="dependencyGraph">The dependency graph to validate.</param>
    /// <param name="context">The context.</param>
    public delegate void ValidateDependencyGraphDelegate(DependencyGraph dependencyGraph, SaveChangesContext context);

    /// <summary>
    /// A delegate representing a save operation.
    /// </summary>
    /// <param name="saveOrder">The entities save order.</param>
    /// <param name="context">The context.</param>
    public delegate void SaveOperationDelegate(List<EntityInfo> saveOrder, SaveChangesContext context);

    /// <summary>
    /// A delegate that can decide whether the entity will be saved.
    /// </summary>
    /// <param name="entityInfo">The entity info.</param>
    /// <param name="context">The context.</param>
    public delegate void BeforeSaveEntityChangesDelegate(EntityInfo entityInfo, SaveChangesContext context);

    /// <summary>
    /// A delegate that is called after an <see cref="EntityInfo"/> is created.
    /// </summary>
    /// <param name="entityInfo">The created entity info.</param>
    /// <param name="saveOptions">The save options sent from the client.</param>
    /// <returns>Whether to add the entity info into the save map.</returns>
    public delegate bool AfterCreateEntityInfoDelegate(EntityInfo entityInfo, SaveOptions saveOptions);

    /// <summary>
    /// A delegate that is called after all changes are flushed into the database.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="keyMappings">The key mappings</param>
    public delegate void AfterFlushChangesDelegate(SaveChangesContext context, List<KeyMapping> keyMappings);

    /// <summary>
    /// A configuration used to configure <see cref="PersistenceManager.SaveChanges(SaveBundle,Action{SaveChangesOptionsConfigurator})"/> method.
    /// </summary>
    public class SaveChangesOptions : ISaveChangesOptions
    {

        /// <summary>
        /// A delegate that is called after an <see cref="EntityInfo"/> is created.
        /// </summary>
        public AfterCreateEntityInfoDelegate AfterCreateEntityInfoAction { get; set; }

        /// <summary>
        /// A delegate that will be invoked before fetching entities from the database.
        /// </summary>
        public BeforeSaveOperationDelegate BeforeFetchEntitiesAction { get; set; }

        /// <summary>
        /// A delegate that validates the dependency graph.
        /// </summary>
        public ValidateDependencyGraphDelegate ValidateDependencyGraphAction { get; set; }

        /// <summary>
        /// A delegate that will be invoked before applying the changes made by the breeze client.
        /// </summary>
        public BeforeSaveOperationDelegate BeforeApplyChangesAction { get; set; }

        /// <summary>
        /// A delegate that will be invoked before saving entities.
        /// </summary>
        public SaveOperationDelegate BeforeSaveChangesAction { get; set; }

        /// <summary>
        /// A delegate that will be invoked before saving an entity.
        /// </summary>
        public BeforeSaveEntityChangesDelegate BeforeSaveEntityChangesAction { get; set; }

        /// <summary>
        /// A delegate that will be invoked after saving entities.
        /// </summary>
        public SaveOperationDelegate AfterSaveChangesAction { get; set; }

        /// <summary>
        /// A delegate that will be invoked after all changes are flushed into the database.
        /// </summary>
        public AfterFlushChangesDelegate AfterFlushChangesAction { get; set; }

        void ISaveChangesOptions.ValidateDependencyGraph(DependencyGraph dependencyGraph, SaveChangesContext context)
        {
            ValidateDependencyGraphAction?.Invoke(dependencyGraph, context);
        }

        void ISaveChangesOptions.BeforeFetchEntities(SaveChangesContext context)
        {
            BeforeFetchEntitiesAction?.Invoke(context);
        }

        Task ISaveChangesOptions.BeforeFetchEntitiesAsync(SaveChangesContext context, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        void ISaveChangesOptions.BeforeApplyChanges(SaveChangesContext context)
        {
            BeforeApplyChangesAction?.Invoke(context);
        }

        Task ISaveChangesOptions.BeforeApplyChangesAsync(SaveChangesContext context, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        void ISaveChangesOptions.BeforeSaveChanges(List<EntityInfo> saveOrder, SaveChangesContext context)
        {
            BeforeSaveChangesAction?.Invoke(saveOrder, context);
        }

        Task ISaveChangesOptions.BeforeSaveChangesAsync(List<EntityInfo> saveOrder, SaveChangesContext context, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        void ISaveChangesOptions.AfterSaveChanges(List<EntityInfo> saveOrder, SaveChangesContext context)
        {
            AfterSaveChangesAction?.Invoke(saveOrder, context);
        }

        Task ISaveChangesOptions.AfterSaveChangesAsync(List<EntityInfo> saveOrder, SaveChangesContext context, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        void ISaveChangesOptions.AfterFlushChanges(SaveChangesContext context, List<KeyMapping> keyMappings)
        {
            AfterFlushChangesAction?.Invoke(context, keyMappings);
        }

        Task ISaveChangesOptions.AfterFlushChangesAsync(SaveChangesContext context, List<KeyMapping> keyMappings, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        void ISaveChangesOptions.BeforeSaveEntityChanges(EntityInfo entityInfo, SaveChangesContext context)
        {
            BeforeSaveEntityChangesAction?.Invoke(entityInfo, context);
        }

        Task ISaveChangesOptions.BeforeSaveEntityChangesAsync(EntityInfo entityInfo, SaveChangesContext context, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
