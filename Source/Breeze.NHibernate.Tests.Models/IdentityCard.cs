using System;
using NHibernate.Mapping.ByCode;

namespace Breeze.NHibernate.Tests.Models
{
    public class IdentityCard : VersionedEntity
    {
        public IdentityCard()
        {
            CreatedDate = DateTime.UtcNow;
        }

        public virtual string Code { get; set; }

        public virtual Person Owner { get; set; }

        public virtual void SetOwner(Person owner)
        {
            Id = owner.Id;
            Owner = owner;
        }
    }

    public class IdentityCardMapping : VersionedEntityMapping<IdentityCard>
    {
        public IdentityCardMapping()
        {
            Id(o => o.Id, o =>
            {
                o.Generator(Generators.Foreign<IdentityCard>(i => i.Owner));
            });
            Property(o => o.Code);
            OneToOne(o => o.Owner, o => o.Constrained(true));
        }
    }
}
