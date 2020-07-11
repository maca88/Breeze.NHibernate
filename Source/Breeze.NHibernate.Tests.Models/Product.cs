using System;

namespace Breeze.NHibernate.Tests.Models
{
    public class Product : VersionedEntity
    {
        public Product()
        {
            CreatedDate = DateTime.UtcNow;
        }

        public virtual string Name { get; set; }

        public virtual string Category { get; set; }

        public virtual bool Active { get; set; }

        public override int GetHashCode()
        {
            if (Id == default)
            {
                return base.GetHashCode();
            }

            return typeof(Product).GetHashCode() ^ Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Product product))
            {
                return false;
            }

            if (Id == default)
            {
                return base.Equals(obj);
            }

            return Id == product.Id;
        }
    }

    public class ProductMapping : VersionedEntityMapping<Product>
    {
        public ProductMapping()
        {
            Property(o => o.Name, o => o.NotNullable(true));
            Property(o => o.Category);
            Property(o => o.Active, o => o.NotNullable(true));
        }
    }
}
