using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Persister.Entity;
using NHibernate.Util;

namespace Breeze.NHibernate.Internal
{
    internal class EntityBatchFetcher<TEntity, TId> : IEntityBatchFetcher
    {
        private readonly ParameterExpression _parameter;
        private readonly Expression<Func<TEntity, TId>> _idExpression;
        private readonly Func<TEntity, object> _idSelector;
        private readonly MethodInfo _containsMethod;

        public EntityBatchFetcher(AbstractEntityPersister persister)
        {
            _parameter = Expression.Parameter(typeof(TEntity));
            var property = Expression.Property(_parameter, persister.IdentifierPropertyName);
            _idExpression = Expression.Lambda<Func<TEntity, TId>>(property, _parameter);
            _idSelector = Expression.Lambda<Func<TEntity, object>>(Expression.Convert(property, typeof(object)), _parameter).Compile();
            _containsMethod = ReflectHelper.GetMethod(() => Enumerable.Contains(null, default(TId)));
        }

        public IDictionary<object, object> BatchFetch(ISession session, IReadOnlyCollection<object> keys, int batchSize)
        {
            var result = new Dictionary<object, object>(keys.Count);
            var currentBatch = new List<TId>(batchSize);
            foreach (TId key in keys)
            {
                currentBatch.Add(key);
                if (currentBatch.Count % batchSize == 0)
                {
                    AddToResult();
                    currentBatch.Clear();
                }
            }

            AddToResult();

            return result;

            void AddToResult()
            {
                if (currentBatch.Count == 0)
                {
                    return;
                }

                var value = Expression.Constant(currentBatch, typeof(IEnumerable<TId>));
                var containsMethodCall = Expression.Call(_containsMethod, value, _idExpression.Body);
                var predicate = Expression.Lambda<Func<TEntity, bool>>(containsMethodCall, _parameter);
                var items = session.Query<TEntity>().Where(predicate).ToList();
                foreach (var item in items)
                {
                    result.Add(_idSelector(item), item);
                }
            }
        }

        public async Task<IDictionary<object, object>> BatchFetchAsync(ISession session, IReadOnlyCollection<object> keys, int batchSize, CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<object, object>(keys.Count);
            var currentBatch = new List<TId>(batchSize);
            foreach (TId key in keys)
            {
                currentBatch.Add(key);
                if (currentBatch.Count % batchSize == 0)
                {
                    await AddToResult().ConfigureAwait(false);
                    currentBatch.Clear();
                }
            }

            await AddToResult().ConfigureAwait(false);

            return result;

            async Task AddToResult()
            {
                if (currentBatch.Count == 0)
                {
                    return;
                }

                var value = Expression.Constant(currentBatch, typeof(IEnumerable<TId>));
                var containsMethodCall = Expression.Call(_containsMethod, value, _idExpression.Body);
                var predicate = Expression.Lambda<Func<TEntity, bool>>(containsMethodCall, _parameter);
                var items = await session.Query<TEntity>().Where(predicate).ToListAsync(cancellationToken).ConfigureAwait(false);
                foreach (var item in items)
                {
                    result.Add(_idSelector(item), item);
                }
            }
        }
    }
}
