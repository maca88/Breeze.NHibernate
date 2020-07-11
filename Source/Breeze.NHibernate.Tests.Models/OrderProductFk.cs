namespace Breeze.NHibernate.Tests.Models
{
    public class OrderProductFk : Entity
    {
        public virtual Order Order { get; set; }

        public virtual long OrderId { get; set; }

        public virtual Product Product { get; set; }

        public virtual long ProductId { get; set; }

        public virtual int Quantity { get; set; }

        public virtual decimal TotalPrice { get; set; }

        public virtual string ProductCategory => Product.Category;
    }

    public class OrderProductFkMapping : EntityMapping<OrderProductFk>
    {
        public OrderProductFkMapping()
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
            Property(o => o.OrderId, o =>
            {
                o.NotNullable(true);
                o.Insert(false);
                o.Update(false);
            });
            Property(o => o.ProductId, o =>
            {
                o.NotNullable(true);
                o.Insert(false);
                o.Update(false);
            });
            Property(o => o.TotalPrice, o => o.NotNullable(true));
            Property(o => o.Quantity, o => o.NotNullable(true));
        }
    }
}
