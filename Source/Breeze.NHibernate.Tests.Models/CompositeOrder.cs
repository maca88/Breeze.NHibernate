using System.Collections.Generic;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Breeze.NHibernate.Tests.Models
{
    public class CompositeOrder : CompositeEntity<CompositeOrder, int, long, string>
    {
        public CompositeOrder(int year, long number, string status)
        {
            Year = year;
            Number = number;
            Status = status;
        }

        protected CompositeOrder()
        {
        }

        public virtual int Year { get; protected set; }

        public virtual long Number { get; protected set; }

        public virtual string Status { get; protected set; }

        public virtual void SetStatus(string status)
        {
            Status = status;
        }

        public virtual decimal TotalPrice { get; set; }

        public virtual ISet<CompositeOrderRow> CompositeOrderRows { get; set; } = new HashSet<CompositeOrderRow>();

        public virtual ISet<CompositeOrderProduct> CompositeOrderProducts { get; set; } = new HashSet<CompositeOrderProduct>();

        protected override CompositeKey<CompositeOrder, int, long, string> CreateCompositeKey()
        {
            return new CompositeKey<CompositeOrder, int, long, string>(this, o => o.Year, o => o.Number, o => o.Status);
        }
    }

    public class CompositeOrderMapping : ClassMapping<CompositeOrder>
    {
        public CompositeOrderMapping()
        {
            ComposedId(o =>
            {
                o.Property(p => p.Year);
                o.Property(p => p.Number);
                o.Property(p => p.Status);
            });
            Property(o => o.TotalPrice, o => o.NotNullable(true));
            Set(o => o.CompositeOrderRows, o =>
            {
                o.Key(k => k.Columns(
                    c => c.Name($"{nameof(CompositeOrder)}{nameof(CompositeOrder.Year)}"),
                    c => c.Name($"{nameof(CompositeOrder)}{nameof(CompositeOrder.Number)}"),
                    c => c.Name($"{nameof(CompositeOrder)}{nameof(CompositeOrder.Status)}")
                ));
                o.Inverse(true);
                o.Cascade(Cascade.All);
                o.Lazy(CollectionLazy.Extra);
            }, o => o.OneToMany());
            Set(o => o.CompositeOrderProducts, o =>
            {
                o.Key(k => k.Columns(
                    c => c.Name($"{nameof(CompositeOrder)}{nameof(CompositeOrder.Year)}"),
                    c => c.Name($"{nameof(CompositeOrder)}{nameof(CompositeOrder.Number)}"),
                    c => c.Name($"{nameof(CompositeOrder)}{nameof(CompositeOrder.Status)}")
                ));
                o.Inverse(true);
                o.Cascade(Cascade.All);
            }, o => o.OneToMany());
        }
    }
}
