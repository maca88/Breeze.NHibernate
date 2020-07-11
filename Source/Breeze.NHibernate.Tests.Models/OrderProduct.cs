
namespace Breeze.NHibernate.Tests.Models
{
    public class OrderProduct : Entity
    {
        public virtual Order Order { get; set; }

        public virtual Product Product { get; set; }

        public virtual int Quantity { get; set; }

        public virtual decimal TotalPrice { get; set; }

        public virtual string ProductCategory => Product.Category;
    }

    public class OrderProductMapping : EntityMapping<OrderProduct>
    {
        public OrderProductMapping()
        {
            ManyToOne(o => o.Order, o =>
            {
                o.NotNullable(true);
                o.Column("OrderId");
            });
            ManyToOne(o => o.Product, o =>
            {
                o.NotNullable(true);
                o.Column("ProductId");
            });
            Property(o => o.Quantity, o => o.NotNullable(true));
            Property(o => o.TotalPrice, o => o.NotNullable(true));
        }
    }
}
