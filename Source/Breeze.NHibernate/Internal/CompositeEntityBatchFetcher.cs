using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Persister.Entity;

namespace Breeze.NHibernate.Internal
{
    internal class CompositeEntityBatchFetcher<TEntity> : IEntityBatchFetcher
    {
        private readonly ParameterExpression _parameter;

        public CompositeEntityBatchFetcher(AbstractEntityPersister persister)
        {
            // Embedded id
            _parameter = Expression.Parameter(typeof(TEntity));
        }

        public IDictionary<object, object> BatchFetch(ISession session, IReadOnlyCollection<object> keys, int batchSize)
        {
            var result = new Dictionary<object, object>(keys.Count);
            var currentBatchSize = 0;
            Expression expression = null;
            foreach (var key in keys)
            {
                var equal = Expression.Equal(_parameter, Expression.Constant(key, typeof(TEntity)));
                if (currentBatchSize == 0 || expression == null)
                {
                    AddToResult();
                    expression = equal;
                }
                else
                {
                    expression = Expression.OrElse(expression, equal);
                }

                currentBatchSize++;
                currentBatchSize %= batchSize;
            }

            AddToResult();

            return result;

            void AddToResult()
            {
                if (expression == null)
                {
                    return;
                }

                var items = session.Query<TEntity>().Where(Expression.Lambda<Func<TEntity, bool>>(expression, _parameter)).ToList();
                foreach (var item in items)
                {
                    result.Add(item, item);
                }
            }
        }

        public async Task<IDictionary<object, object>> BatchFetchAsync(ISession session, IReadOnlyCollection<object> keys, int batchSize, CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<object, object>(keys.Count);
            var currentBatchSize = 0;
            Expression expression = null;
            foreach (var key in keys)
            {
                var equal = Expression.Equal(_parameter, Expression.Constant(key, typeof(TEntity)));
                if (currentBatchSize == 0 || expression == null)
                {
                    await AddToResult().ConfigureAwait(false);
                    expression = equal;
                }
                else
                {
                    expression = Expression.OrElse(expression, equal);
                }

                currentBatchSize++;
                currentBatchSize %= batchSize;
            }

            await AddToResult().ConfigureAwait(false);

            return result;

            async Task AddToResult()
            {
                if (expression == null)
                {
                    return;
                }

                var items = await session.Query<TEntity>().Where(Expression.Lambda<Func<TEntity, bool>>(expression, _parameter)).ToListAsync(cancellationToken).ConfigureAwait(false);
                foreach (var item in items)
                {
                    result.Add(item, item);
                }
            }
        }
    }
}
