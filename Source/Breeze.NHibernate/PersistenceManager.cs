using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Breeze.NHibernate.Configuration;
using NHibernate;

namespace Breeze.NHibernate
{
    /// <summary>
    /// A manager for persisting entity changes made by the client.
    /// </summary>
    public partial class PersistenceManager
    {
        private readonly EntityUpdater _entityUpdater;
        private readonly ISaveWorkStateFactory _saveWorkStateFactory;
        private readonly IModelSaveValidatorProvider _modelSaveValidatorProvider;

        public PersistenceManager(
            EntityUpdater entityUpdater,
            ISaveWorkStateFactory saveWorkStateFactory,
            IModelSaveValidatorProvider modelSaveValidatorProvider)
        {
            _entityUpdater = entityUpdater;
            _saveWorkStateFactory = saveWorkStateFactory;
            _modelSaveValidatorProvider = modelSaveValidatorProvider;
        }

        /// <summary>
        /// Saves the changes made by the client, with an additional root model validation performed by the provided or default <see cref="IModelSaveValidator"/>.
        /// </summary>
        /// <typeparam name="TModel">The root model type.</typeparam>
        /// <param name="saveBundle">The changes to save.</param>
        /// <param name="configureAction">The save configuration action.</param>
        /// <returns>The save result.</returns>
        public SaveResult SaveChanges<TModel>(SaveBundle saveBundle, Action<SaveChangesOptionsConfigurator<TModel>> configureAction = null)
        {
            var configurator = new SaveChangesOptionsConfigurator<TModel>(_modelSaveValidatorProvider.Get(typeof(TModel)));
            configureAction?.Invoke(configurator);

            return SaveChanges(saveBundle, configurator.SaveChangesOptions);
        }

        /// <summary>
        /// Saves the changes made by the client, with an additional root model validation performed by the provided or default <see cref="IModelSaveValidator"/>.
        /// </summary>
        /// <typeparam name="TModel">The root model type.</typeparam>
        /// <param name="saveBundle">The changes to save.</param>
        /// <param name="configureAction">The save configuration action.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The save result.</returns>
        public Task<SaveResult> SaveChangesAsync<TModel>(SaveBundle saveBundle, Action<AsyncSaveChangesOptionsConfigurator<TModel>> configureAction, CancellationToken cancellationToken = default)
        {
            var configurator = new AsyncSaveChangesOptionsConfigurator<TModel>(_modelSaveValidatorProvider.Get(typeof(TModel)));
            configureAction?.Invoke(configurator);

            return SaveChangesAsync(saveBundle, configurator.SaveChangesOptions, cancellationToken);
        }

        /// <summary>
        /// Saves the changes made by the client, with an additional root model validation performed by the default <see cref="IModelSaveValidator"/>.
        /// </summary>
        /// <typeparam name="TModel">The root model type.</typeparam>
        /// <param name="saveBundle">The changes to save.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The save result.</returns>
        public Task<SaveResult> SaveChangesAsync<TModel>(SaveBundle saveBundle, CancellationToken cancellationToken = default)
        {
            var configurator = new AsyncSaveChangesOptionsConfigurator<TModel>(_modelSaveValidatorProvider.Get(typeof(TModel)));

            return SaveChangesAsync(saveBundle, configurator.SaveChangesOptions, cancellationToken);
        }

        /// <summary>
        /// Saves the changes made by the client.
        /// </summary>
        /// <param name="saveBundle">The changes to save.</param>
        /// <param name="configureAction">The save configuration action.</param>
        /// <returns>The save result.</returns>
        public SaveResult SaveChanges(SaveBundle saveBundle, Action<SaveChangesOptionsConfigurator> configureAction = null)
        {
            var configurator = new SaveChangesOptionsConfigurator();
            configureAction?.Invoke(configurator);

            return SaveChanges(saveBundle, configurator.SaveChangesOptions);
        }

        /// <summary>
        /// Saves the changes made by the client.
        /// </summary>
        /// <param name="saveBundle">The changes to save.</param>
        /// <param name="configureAction">The save configuration action.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The save result.</returns>
        public Task<SaveResult> SaveChangesAsync(SaveBundle saveBundle, Action<AsyncSaveChangesOptionsConfigurator> configureAction, CancellationToken cancellationToken = default)
        {
            var configurator = new AsyncSaveChangesOptionsConfigurator();
            configureAction?.Invoke(configurator);

            return SaveChangesAsync(saveBundle, configurator.SaveChangesOptions, cancellationToken);
        }

        /// <summary>
        /// Saves the changes made by the client.
        /// </summary>
        /// <param name="saveBundle">The changes to save.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The save result.</returns>
        public Task<SaveResult> SaveChangesAsync(SaveBundle saveBundle, CancellationToken cancellationToken = default)
        {
            return SaveChangesAsync(saveBundle, AsyncSaveChangesOptions.Default, cancellationToken);
        }

        /// <summary>
        /// Saves the changes made by the client.
        /// </summary>
        /// <param name="saveBundle">The changes to save.</param>
        /// <param name="saveChangesOptions">The saving options.</param>
        /// <returns>The save result.</returns>
        protected SaveResult SaveChanges(SaveBundle saveBundle, SaveChangesOptions saveChangesOptions)
        {
            var saveWorkState = _saveWorkStateFactory.Create(saveBundle,
                info => AfterCreateEntityInfo(info, saveBundle.SaveOptions) &&
                        (saveChangesOptions.AfterCreateEntityInfoAction?.Invoke(info, saveBundle.SaveOptions) ?? true));
            try
            {
                var context = new SaveChangesContext(saveWorkState.SaveMap, saveBundle.SaveOptions);
                return SaveChangesCore(saveWorkState, context, saveChangesOptions);
            }
            catch (Exception e)
            {
                if (!HandleSaveException(e, saveWorkState))
                {
                    throw;
                }
            }

            return saveWorkState.ToSaveResult();
        }

        /// <summary>
        /// Saves the changes made by the client.
        /// </summary>
        /// <param name="workState">The save work state.</param>
        /// <param name="context">The save context.</param>
        /// <param name="saveChangesOptions">The save options.</param>
        /// <returns>The save result.</returns>
        protected virtual SaveResult SaveChangesCore(SaveWorkState workState, SaveChangesContext context, SaveChangesOptions saveChangesOptions)
        {
            workState.KeyMappings = _entityUpdater.FetchAndApplyChanges(this, context, saveChangesOptions);

            return workState.ToSaveResult();
        }

        /// <summary>
        /// Saves the changes made by the client.
        /// </summary>
        /// <param name="saveBundle">The changes to save.</param>
        /// <param name="saveChangesOptions">The saving options.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The save result.</returns>
        protected async Task<SaveResult> SaveChangesAsync(SaveBundle saveBundle, AsyncSaveChangesOptions saveChangesOptions, CancellationToken cancellationToken)
        {
            var saveWorkState = _saveWorkStateFactory.Create(saveBundle,
                info => AfterCreateEntityInfo(info, saveBundle.SaveOptions) &&
                        (saveChangesOptions.AfterCreateEntityInfoAction?.Invoke(info, saveBundle.SaveOptions) ?? true));
            try
            {
                var context = new SaveChangesContext(saveWorkState.SaveMap, saveBundle.SaveOptions);
                return await SaveChangesCoreAsync(saveWorkState, context, saveChangesOptions, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (!HandleSaveException(e, saveWorkState))
                {
                    throw;
                }
            }

            return saveWorkState.ToSaveResult();
        }

        /// <summary>
        /// Saves the changes made by the client.
        /// </summary>
        /// <param name="workState">The save work state.</param>
        /// <param name="context">The save context.</param>
        /// <param name="saveChangesOptions">The save options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The save result.</returns>
        protected virtual async Task<SaveResult> SaveChangesCoreAsync(SaveWorkState workState, SaveChangesContext context, AsyncSaveChangesOptions saveChangesOptions, CancellationToken cancellationToken)
        {
            workState.KeyMappings = await _entityUpdater.FetchAndApplyChangesAsync(this, context, saveChangesOptions, cancellationToken).ConfigureAwait(false);

            return workState.ToSaveResult();
        }

        /// <summary>
        /// Handles an exception thrown while saving entities.
        /// </summary>
        /// <param name="exception">The thrown exception.</param>
        /// <param name="saveWorkState">The save work state.</param>
        /// <returns>Whether the exception was handled.</returns>
        protected virtual bool HandleSaveException(Exception exception, SaveWorkState saveWorkState)
        {
            return false;
        }

        /// <summary>
        /// The method that is called after an <see cref="EntityInfo"/> is created from the <see cref="SaveBundle"/>.
        /// </summary>
        /// <param name="entityInfo">The created entity info.</param>
        /// <param name="saveOptions">The save options from the client.</param>
        /// <returns>Whether the <see cref="EntityInfo"/> should be included in the save map.</returns>
        protected internal virtual bool AfterCreateEntityInfo(EntityInfo entityInfo, SaveOptions saveOptions)
        {
            return true;
        }

        /// <summary>
        /// The method that is called before starting to fetch and apply entity changes. Note that in this method <see cref="EntityInfo.Entity"/> will be <see langword="null"/>, only
        /// <see cref="EntityInfo.ClientEntity"/> is set.
        /// </summary>
        /// <param name="context">The save changes context.</param>
        protected internal virtual void BeforeFetchEntities(SaveChangesContext context)
        {
        }

        /// <summary>
        /// The method that is called after the entities are fetched from the database. 
        /// </summary>
        /// <param name="context">The save changes context.</param>
        protected internal virtual void BeforeApplyChanges(SaveChangesContext context)
        {
        }

        /// <summary>
        /// The method that is called after the entity changes are applied and can be overriden to apply a general validation of the dependency graph.
        /// </summary>
        /// <param name="dependencyGraph">The dependency graph of the entities that are going to be saved.</param>
        /// <param name="context">The save changes context.</param>
        protected internal virtual void ValidateDependencyGraph(DependencyGraph dependencyGraph, SaveChangesContext context)
        {
        }

        /// <summary>
        /// The method that is called after the entity changes are applied and before are saved using NHibernate methods Save, Update and Delete from ISession.
        /// </summary>
        /// <param name="saveOrder">The order in which the entities will be saved.</param>
        /// <param name="context">The save changes context.</param>
        protected internal virtual void BeforeSaveChanges(List<EntityInfo> saveOrder, SaveChangesContext context)
        {
        }

        /// <summary>
        /// The method that is called before an entity is saved using NHibernate Save, Update or Delete methods from ISession.
        /// </summary>
        /// <param name="entityInfo">The entity info that is being saved.</param>
        /// <param name="context">The save changes context.</param>
        /// <returns>Whether the entity should be saved using the Save, Update or Delete methods from ISession.</returns>
        protected internal virtual void BeforeSaveEntityChanges(EntityInfo entityInfo, SaveChangesContext context)
        {
        }

        /// <summary>
        /// The method that is called after the entities are saved by NHibernate methods Save, Update and Delete from ISession.
        /// </summary>
        /// <param name="saveOrder">The order in which the entities were saved.</param>
        /// <param name="context">The save changes context.</param>
        protected internal virtual void AfterSaveChanges(List<EntityInfo> saveOrder, SaveChangesContext context)
        {
        }

        /// <summary>
        /// The method that is called after the entity changes where applied and flushed into the database.
        /// </summary>
        /// <param name="context">The save changes context.</param>
        /// <param name="keyMappings">The key mappings for auto-generated primary keys.</param>
        protected internal virtual void AfterFlushChanges(SaveChangesContext context, List<KeyMapping> keyMappings)
        {
        }
    }
}
