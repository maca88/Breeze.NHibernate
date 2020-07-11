using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NHibernate;

namespace Breeze.NHibernate
{
    /// <summary>
    /// Provides a api to batch fetch existing entities by their keys from the database.
    /// </summary>
    public interface IEntityBatchFetcher // TODO: generate async
    {
        /// <summary>
        /// Batch fetches entities by the given keys using the provided batch size.
        /// </summary>
        /// <param name="session">The NHibernate session.</param>
        /// <param name="keys">The entity keys to fetch.</param>
        /// <param name="batchSize">The batch size to fetch.</param>
        /// <returns>A dictionaries of keys and entities.</returns>
        IDictionary<object, object> BatchFetch(ISession session, IReadOnlyCollection<object> keys, int batchSize);

        /// <summary>
        /// Batch fetches entities by the given keys using the provided batch size.
        /// </summary>
        /// <param name="session">The NHibernate session.</param>
        /// <param name="keys">The entity keys to fetch.</param>
        /// <param name="batchSize">The batch size to fetch.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A dictionaries of keys and entities.</returns>
        Task<IDictionary<object, object>> BatchFetchAsync(ISession session, IReadOnlyCollection<object> keys, int batchSize, CancellationToken cancellationToken = default);
    }
}
