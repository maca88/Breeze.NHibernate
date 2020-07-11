
namespace Breeze.NHibernate.Tests.Models
{
    public class CompositeOrderRow : Entity
    {
        public virtual CompositeOrder CompositeOrder { get; set; }

        public virtual Product Product { get; set; }

        public virtual decimal Price { get; set; }

        public virtual int Quantity { get; set; }
    }

    public class CompositeOrderRowMapping : EntityMapping<CompositeOrderRow>
    {
        public CompositeOrderRowMapping()
        {
            Property(o => o.Price, o => o.NotNullable(true));
            Property(o => o.Quantity, o => o.NotNullable(true));
            ManyToOne(o => o.Product, o =>
            {
                o.Column("ProductId");
                o.NotNullable(true);
            });
            ManyToOne(o => o.CompositeOrder, o =>
            {
                o.Columns(
                    c =>
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
                    }
                );
            });
        }
    }
}
