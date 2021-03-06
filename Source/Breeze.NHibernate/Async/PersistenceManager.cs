﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Breeze.NHibernate.Configuration;
using NHibernate;

namespace Breeze.NHibernate
{
    public partial class PersistenceManager
    {

        /// <summary>
        /// The method that is called before starting to fetch and apply entity changes. Note that in this method <see cref="EntityInfo.Entity"/> will be <see langword="null"/>, only
        /// <see cref="EntityInfo.ClientEntity"/> is set.
        /// </summary>
        /// <param name="context">The save changes context.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
        protected internal virtual Task BeforeFetchEntitiesAsync(SaveChangesContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<object>(cancellationToken);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// The method that is called after the entities are fetched from the database. 
        /// </summary>
        /// <param name="context">The save changes context.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
        protected internal virtual Task BeforeApplyChangesAsync(SaveChangesContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<object>(cancellationToken);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// The method that is called after the entity changes are applied and before are saved using NHibernate methods Save, Update and Delete from ISession.
        /// </summary>
        /// <param name="saveOrder">The order in which the entities will be saved.</param>
        /// <param name="context">The save changes context.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
        protected internal virtual Task BeforeSaveChangesAsync(List<EntityInfo> saveOrder, SaveChangesContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<object>(cancellationToken);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// The method that is called before an entity is saved using NHibernate Save, Update or Delete methods from ISession.
        /// </summary>
        /// <param name="entityInfo">The entity info that is being saved.</param>
        /// <param name="context">The save changes context.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
        /// <returns>Whether the entity should be saved using the Save, Update or Delete methods from ISession.</returns>
        protected internal virtual Task BeforeSaveEntityChangesAsync(EntityInfo entityInfo, SaveChangesContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<object>(cancellationToken);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// The method that is called after the entities are saved by NHibernate methods Save, Update and Delete from ISession.
        /// </summary>
        /// <param name="saveOrder">The order in which the entities were saved.</param>
        /// <param name="context">The save changes context.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
        protected internal virtual Task AfterSaveChangesAsync(List<EntityInfo> saveOrder, SaveChangesContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<object>(cancellationToken);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// The method that is called after the entity changes where applied and flushed into the database.
        /// </summary>
        /// <param name="context">The save changes context.</param>
        /// <param name="keyMappings">The key mappings for auto-generated primary keys.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
        protected internal virtual Task AfterFlushChangesAsync(SaveChangesContext context, List<KeyMapping> keyMappings, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<object>(cancellationToken);
            }
            return Task.CompletedTask;
        }
    }
}
