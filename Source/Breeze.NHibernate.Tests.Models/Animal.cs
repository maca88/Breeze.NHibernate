using System;
using System.Collections.Generic;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Breeze.NHibernate.Tests.Models
{
    public abstract class Animal : VersionedEntity, IAggregate
    {
        public Animal()
        {
            CreatedDate = DateTime.UtcNow;
        }

        public virtual string Name { get; set; }

        public virtual double BodyWeight { get; set; }

        public virtual Animal Parent { get; set; }

        public virtual ISet<Animal> Children { get; set; } = new HashSet<Animal>();
        public virtual object GetAggregateRoot()
        {
            return Parent != null ? Parent.GetAggregateRoot() : this;
        }
    }

    public class AnimalMapping : VersionedEntityMapping<Animal>
    {
        public AnimalMapping()
        {
            Property(o => o.Name, o => o.NotNullable(true));
            Property(o => o.BodyWeight, o => o.NotNullable(true));
            ManyToOne(o => o.Parent, o => o.Column("ParentId"));
            Set(o => o.Children, o =>
            {
                o.Key(k => k.Column("ParentId"));
                o.Inverse(true);
                o.Cascade(Cascade.All);
                o.Lazy(CollectionLazy.Extra);
            }, o => o.OneToMany());
        }
    }

    public class Dog : Animal
    {
        public virtual bool Pregnant { get; set; }

        public virtual DateTime? BirthDate { get; set; }

        public virtual string Breed { get; set; }
    }

    public class DogMapping : JoinedSubclassMapping<Dog>
    {
        public DogMapping()
        {
            Extends(typeof(Animal));
            Property(o => o.Breed);
            Property(o => o.BirthDate);
            Property(o => o.Pregnant, o => o.NotNullable(true));
        }
    }

    public class Cat : Animal
    {
        public virtual bool Pregnant { get; set; }

        public virtual DateTime? BirthDate { get; set; }

        public virtual string Breed { get; set; }
    }

    public class CatMapping : JoinedSubclassMapping<Cat>
    {
        public CatMapping()
        {
            Extends(typeof(Animal));
            Property(o => o.Breed);
            Property(o => o.BirthDate);
            Property(o => o.Pregnant, o => o.NotNullable(true));
        }
    }
}
