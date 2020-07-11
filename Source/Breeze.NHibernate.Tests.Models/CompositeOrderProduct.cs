
using System.Collections.Generic;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Breeze.NHibernate.Tests.Models
{
    public class CompositeOrderProduct : CompositeEntity<CompositeOrderProduct, CompositeOrder, Product>
    {
        public CompositeOrderProduct(CompositeOrder compositeOrder, Product product)
        {
            CompositeOrder = compositeOrder;
            Product = product;
        }

        protected CompositeOrderProduct()
        {
        }

        public virtual CompositeOrder CompositeOrder { get; protected set; }

        public virtual Product Product { get; protected set; }

        public virtual decimal Price { get; set; }

        public virtual int Quantity { get; set; }

        public virtual ISet<CompositeOrderProductRemark> Remarks { get; set; } = new HashSet<CompositeOrderProductRemark>();

        protected override CompositeKey<CompositeOrderProduct, CompositeOrder, Product> CreateCompositeKey()
        {
            return new CompositeKey<CompositeOrderProduct, CompositeOrder, Product>(this, o => o.CompositeOrder, o => o.Product);
        }
    }

    public class CompositeOrderProductMapping : ClassMapping<CompositeOrderProduct>
    {
        public CompositeOrderProductMapping()
        {
            ComposedId(k =>
            {
                k.ManyToOne(p => p.CompositeOrder, o => o.Columns(
                    c => c.Name($"{nameof(CompositeOrder)}{nameof(CompositeOrder.Year)}"),
                    c => c.Name($"{nameof(CompositeOrder)}{nameof(CompositeOrder.Number)}"),
                    c => c.Name($"{nameof(CompositeOrder)}{nameof(CompositeOrder.Status)}")
                ));
                k.ManyToOne(p => p.Product, o => o.Column("ProductId"));
            });

            Property(o => o.Price, o => o.NotNullable(true));
            Property(o => o.Quantity, o => o.NotNullable(true));

            Set(o => o.Remarks, o =>
            {
                o.Key(k => k.Columns(
                    c => c.Name($"{nameof(CompositeOrder)}{nameof(CompositeOrder.Year)}"),
                    c => c.Name($"{nameof(CompositeOrder)}{nameof(CompositeOrder.Number)}"),
                    c => c.Name($"{nameof(CompositeOrder)}{nameof(CompositeOrder.Status)}"),
                    c => c.Name($"{nameof(Product)}{nameof(Product.Id)}")
                ));
                o.Inverse(true);
                o.Cascade(Cascade.All);
            }, o => o.OneToMany());
        }
    }
}
