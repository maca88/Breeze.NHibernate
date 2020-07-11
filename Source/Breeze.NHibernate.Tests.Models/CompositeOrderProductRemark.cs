namespace Breeze.NHibernate.Tests.Models
{
    public class CompositeOrderProductRemark : Entity
    {
        public virtual CompositeOrderProduct CompositeOrderProduct { get; set; }

        public virtual string Remark { get; set; }
    }

    public class CompositeOrderProductRemarkMapping : EntityMapping<CompositeOrderProductRemark>
    {
        public CompositeOrderProductRemarkMapping()
        {
            ManyToOne(o => o.CompositeOrderProduct, o =>
            {
                o.Columns(c =>
                    {
                        c.Name($"{nameof(CompositeOrder)}{nameof(CompositeOrder.Year)}");
                        c.NotNullable(true);
                    },
                    c =>
                    {
                        c.Name($"{nameof(CompositeOrder)}{nameof(CompositeOrder.Number)}");
                        c.NotNullable(true);
                    },
                    c =>
                    {
                        c.Name($"{nameof(CompositeOrder)}{nameof(CompositeOrder.Status)}");
                        c.NotNullable(true);
                    },
                    c =>
                    {
                        c.Name($"{nameof(Product)}{nameof(Product.Id)}");
                        c.NotNullable(true);
                    });
            });

            Property(o => o.Remark, o =>
            {
                o.Length(500);
                o.Lazy(true);
            });
        }
    }
}
