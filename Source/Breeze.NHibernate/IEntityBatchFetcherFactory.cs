using NHibernate.Persister.Entity;

namespace Breeze.NHibernate
{
    /// <summary>
    /// A factory definition for creating <see cref="IEntityBatchFetcher"/>.
    /// </summary>
    public interface IEntityBatchFetcherFactory
    {
        /// <summary>
        /// Creates a <see cref="IEntityBatchFetcher"/> for the given <see cref="AbstractEntityPersister"/>.
        /// </summary>
        /// <param name="persister">The entity persister.</param>
        /// <returns>The entity batch fetcher.</returns>
        IEntityBatchFetcher Create(AbstractEntityPersister persister);
    }
}
