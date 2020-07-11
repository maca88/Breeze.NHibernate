using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Breeze.NHibernate.Internal
{
    internal interface ISaveChangesOptions
    {
        void ValidateDependencyGraph(DependencyGraph dependencyGraph, SaveChangesContext context);

        void BeforeFetchEntities(SaveChangesContext context);

        Task BeforeFetchEntitiesAsync(SaveChangesContext context, CancellationToken cancellationToken);

        void BeforeApplyChanges(SaveChangesContext context);

        Task BeforeApplyChangesAsync(SaveChangesContext context, CancellationToken cancellationToken);

        void BeforeSaveChanges(List<EntityInfo> saveOrder, SaveChangesContext context);

        Task BeforeSaveChangesAsync(List<EntityInfo> saveOrder, SaveChangesContext context, CancellationToken cancellationToken);

        void BeforeSaveEntityChanges(EntityInfo entityInfo, SaveChangesContext context);

        Task BeforeSaveEntityChangesAsync(EntityInfo entityInfo, SaveChangesContext context, CancellationToken cancellationToken);

        void AfterSaveChanges(List<EntityInfo> saveOrder, SaveChangesContext context);

        Task AfterSaveChangesAsync(List<EntityInfo> saveOrder, SaveChangesContext context, CancellationToken cancellationToken);

        void AfterFlushChanges(SaveChangesContext context, List<KeyMapping> keyMappings);

        Task AfterFlushChangesAsync(SaveChangesContext context, List<KeyMapping> keyMappings, CancellationToken cancellationToken);
    }
}
