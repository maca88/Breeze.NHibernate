using System;

namespace Breeze.NHibernate.Tests.Models
{
    public abstract class VersionedEntity : Entity
    {
        public virtual int Version { get; protected set; }

        public virtual DateTime CreatedDate { get; protected set; }

        public virtual DateTime? LastModifiedDate { get; protected set; }
    }

    public abstract class VersionedEntityMapping<TEntity> : EntityMapping<TEntity>
        where TEntity : VersionedEntity
    {
        protected VersionedEntityMapping()
        {
            Version(o => o.Version, o => { });
            Property(o => o.CreatedDate, o => o.NotNullable(true));
            Property(o => o.LastModifiedDate);
        }
    }
}
