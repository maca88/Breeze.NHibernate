using System;
using NHibernate.Mapping.ByCode;

namespace Breeze.NHibernate.Tests.Models
{
    public class Person : VersionedEntity
    {
        public Person()
        {
            CreatedDate = DateTime.UtcNow;
        }

        public virtual string Name { get; set; }

        public virtual string Surname { get; set; }

        public virtual string FullName { get; protected set; }

        public virtual IdentityCard IdentityCard { get; set; }

        public virtual Passport Passport { get; set; }
    }

    public class PersonMapping : VersionedEntityMapping<Person>
    {
        public PersonMapping()
        {
            Property(o => o.Name);
            Property(o => o.Surname);
            Property(o => o.FullName, o => o.Formula("(Name)"));
            OneToOne(o => o.IdentityCard, o =>
            {
                // With PropertyRef you specify that foreign key will be created on the related table (ie. IdentityCard)
                //o.PropertyReference(p => p.Owner);
                o.Cascade(Cascade.All);
            });
            ManyToOne(o => o.Passport, o =>
            {
                o.Column("PassportId");
                o.Cascade(Cascade.Persist);
                // o.Unique(true); On sql server null values are also included by default which break tests
            });
        }
    }
}
