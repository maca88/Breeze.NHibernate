using System;
using NHibernate.Mapping.ByCode;

namespace Breeze.NHibernate.Tests.Models
{
    public class Passport : VersionedEntity
    {
        public Passport()
        {
            CreatedDate = DateTime.UtcNow;
        }

        public virtual long Number { get; set; }

        public virtual DateTime ExpirationDate { get; set; }

        public virtual string Country { get; set; }

        public virtual Person Owner { get; set; }
    }

    public class PassportMapping : VersionedEntityMapping<Passport>
    {
        public PassportMapping()
        {
            OneToOne(o => o.Owner, o =>
            {
                o.Constrained(true);
                o.Cascade(Cascade.Persist);
                o.ForeignKey("none");
                o.PropertyReference(p => p.Passport);
            });
        }
    }
}
