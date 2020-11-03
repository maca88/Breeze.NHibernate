
namespace Breeze.NHibernate.Metadata
{
    /// <summary>
    /// A breeze validator.
    /// </summary>
    public class Validator : MetadataObject
    {
        /// <summary>
        /// Constructs an instance of <see cref="Validator"/>.
        /// </summary>
        public Validator(string name)
        {
            Name = name;
        }

        /// <summary>
        /// On deserialization, this must match the name of some validator already registered on the breeze client.
        /// </summary>
        public string Name
        {
            get => Get<string>(nameof(Name));
            private set => Set(nameof(Name), value);
        }
    }
}
