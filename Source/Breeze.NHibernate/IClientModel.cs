using Breeze.NHibernate.Metadata;

namespace Breeze.NHibernate
{
    /// <summary>
    /// An interface that represents a client model, which can be added to <see cref="BreezeMetadata"/> as an unmapped <see cref="EntityType"/>.
    /// </summary>
    public interface IClientModel
    {
        /// <summary>
        /// The id of the model, which is required for constructing an <see cref="EntityType"/>.
        /// </summary>
        long Id { get; set; }
    }
}
