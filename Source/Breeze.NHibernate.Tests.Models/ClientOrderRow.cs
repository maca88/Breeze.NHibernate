using Breeze.NHibernate.Tests.Models.Attributes;

namespace Breeze.NHibernate.Tests.Models
{
    public class ClientOrderRow : IClientModel
    {
        public long Id { get; set; }

        public bool IsNew() => Id <= 0;

        [NotNull]
        public ClientOrder ClientOrder { get; set; }

        public Product Product { get; set; }

        public decimal Price { get; set; }
    }
}
