namespace Breeze.NHibernate.Tests.Models
{
    public class Attachment : Entity
    {
        public virtual string Name { get; set; }

        public virtual byte[] Content { get; set; }
    }

    public class AttachmentMapping : EntityMapping<Attachment>
    {
        public AttachmentMapping()
        {
            Property(o => o.Name, o => o.NotNullable(true));
            Property(o => o.Content, o => o.Lazy(true));
        }
    }
}
