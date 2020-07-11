using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Breeze.NHibernate.Tests.Models
{
    public abstract class Entity
    {
        public virtual long Id { get; protected set; }
    }

    public abstract class EntityMapping<TEntity> : ClassMapping<TEntity>
        where TEntity : Entity
    {
        protected EntityMapping()
        {
            Id(o => o.Id, o => o.Generator(Generators.Native));
        }
    }
}
