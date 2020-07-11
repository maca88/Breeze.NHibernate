using System;
using System.Collections.Generic;
using NHibernate.Mapping.ByCode;

namespace Breeze.NHibernate.Tests.Models
{
    public class Order : VersionedEntity
    {
        public Order()
        {
            CreatedDate = DateTime.UtcNow;
        }

        public virtual void SetId(long id)
        {
            Id = id;
        }

        public virtual OrderStatus Status { get; set; }

        public virtual string Name { get; set; }

        public virtual decimal TotalPrice { get; set; }

        public virtual bool Active { get; set; }

        public virtual Address Address { get; set; } = new Address();

        public virtual ISet<OrderProduct> Products { get; set; } = new HashSet<OrderProduct>();

        public virtual ISet<OrderProductFk> FkProducts { get; set; } = new HashSet<OrderProductFk>();
    }

    public class OrderMapping : VersionedEntityMapping<Order>
    {
        public OrderMapping() : this(Cascade.All, Cascade.All)
        {
        }

        public OrderMapping(Cascade productsCascade, Cascade fkProductsCascade)
        {
            Property(o => o.Status, o => o.NotNullable(true));
            Property(o => o.Name, o =>
            {
                o.NotNullable(true);
                o.Length(20);
            });
            Property(o => o.TotalPrice, o => o.NotNullable(true));
            Property(o => o.Active, o => o.NotNullable(true));
            Component(o => o.Address, o =>
            {
                o.Property(c => c.City);
                o.Property(c => c.Country);
                o.Property(c => c.HouseNumber);
                o.Property(c => c.Street);
            });
            Set(o => o.Products, o =>
            {
                o.Key(k => k.Column("OrderId"));
                o.Inverse(true);
                o.Cascade(productsCascade);
                o.Lazy(CollectionLazy.Extra);
            }, o => o.OneToMany());
            Set(o => o.FkProducts, o =>
            {
                o.Key(k => k.Column("OrderId"));
                o.Inverse(true);
                o.Cascade(fkProductsCascade);
                o.Lazy(CollectionLazy.Extra);
            }, o => o.OneToMany());
        }
    }
}
