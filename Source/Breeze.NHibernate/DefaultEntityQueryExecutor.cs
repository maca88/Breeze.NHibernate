using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Breeze.Core;
using NHibernate.Linq;

namespace Breeze.NHibernate
{
    /// <summary>
    /// The default <see cref="IEntityQueryExecutor"/> implementation.
    /// </summary>
    public partial class DefaultEntityQueryExecutor : IEntityQueryExecutor
    {
        private readonly IEntityMetadataProvider _entityMetadataProvider;
        private readonly IProxyInitializer _proxyInitializer;

        /// <summary>
        /// Constructs an instance of <see cref="DefaultEntityQueryExecutor"/>.
        /// </summary>
        public DefaultEntityQueryExecutor(IEntityMetadataProvider entityMetadataProvider, IProxyInitializer proxyInitializer)
        {
            _entityMetadataProvider = entityMetadataProvider;
            _proxyInitializer = proxyInitializer;
        }

        /// <inheritdoc />
        public bool ShouldApplyAndExecute(IQueryable queryable, string queryString)
        {
            if (EntityQuery.NeedsExecution != null)
            {
                return EntityQuery.NeedsExecution(queryString, queryable);
            }

            return queryable != null && (queryString != null || queryable.Provider is INhQueryProvider);
        }

        /// <inheritdoc />
        public QueryResult ApplyAndExecute(IQueryable queryable, string queryString)
        {
            var entityQuery = new EntityQuery(queryString);
            var elementType = TypeFns.GetElementType(queryable.GetType());
            entityQuery.Validate(elementType, _entityMetadataProvider);

            int? inlineCount = null;
            queryable = entityQuery.ApplyWhere(queryable, elementType);
            if (entityQuery.IsInlineCountEnabled)
            {
                inlineCount = (int)Queryable.Count((dynamic)queryable);
            }

            queryable = EntityQuery.ApplyCustomLogic?.Invoke(entityQuery, queryable, elementType) ?? queryable;
            queryable = entityQuery.ApplyOrderBy(queryable, elementType);
            queryable = entityQuery.ApplySkip(queryable, elementType);
            queryable = entityQuery.ApplyTake(queryable, elementType);
            queryable = entityQuery.ApplySelect(queryable, elementType);
            queryable = ApplyExpand(queryable, entityQuery, elementType, out var postExecuteExpandPaths);
            var listResult = ToList((dynamic)queryable);
            if (postExecuteExpandPaths?.Count > 0)
            {
                _proxyInitializer.Initialize(listResult, postExecuteExpandPaths);
            }

            listResult = EntityQuery.AfterExecution?.Invoke(entityQuery, queryable, listResult) ?? listResult;

            return new QueryResult(listResult, inlineCount);
        }

        /// <summary>
        /// Applies expand for the given <see cref="IQueryable"/>.
        /// </summary>
        /// <param name="queryable">The queryable to expand.</param>
        /// <param name="entityQuery">The entity query containing the expand information.</param>
        /// <param name="elementType">The element type of the queryable.</param>
        /// <param name="postExecuteExpandPaths">An output parameter with expand paths that have to be expanded after the query execution.</param>
        /// <returns>The expanded queryable.</returns>
        protected virtual IQueryable ApplyExpand(IQueryable queryable, EntityQuery entityQuery, Type elementType, out List<string> postExecuteExpandPaths)
        {
            postExecuteExpandPaths = null;
            if (EntityQuery.ApplyExpand != null)
            {
                return EntityQuery.ApplyExpand(entityQuery, queryable, elementType);
            }

            if (entityQuery.ExpandClause == null)
            {
                return queryable;
            }

            if (!_entityMetadataProvider.IsEntityType(elementType))
            {
                throw new InvalidOperationException($"Unknown entity type {elementType}.");
            }

            var metadata = _entityMetadataProvider.GetMetadata(elementType);
            if (!CanApplyQueryExpands(queryable, metadata, entityQuery))
            {
                postExecuteExpandPaths = entityQuery.ExpandClause.PropertyPaths.ToList();
                return queryable;
            }

            foreach (var propertyPath in entityQuery.ExpandClause.PropertyPaths)
            {
                queryable = ApplyQueryExpand(queryable, propertyPath.Replace('/', '.'));
            }

            return queryable;
        }

        /// <summary>
        /// Whether the expands can be applied to an <see cref="IQueryable"/>.
        /// </summary>
        /// <param name="queryable">The query.</param>
        /// <param name="elementMetadata">The entity metadata.</param>
        /// <param name="entityQuery">The entity query.</param>
        /// <returns>Whether the expands can be applied to an <see cref="IQueryable"/>.</returns>
        protected virtual bool CanApplyQueryExpands(IQueryable queryable, EntityMetadata elementMetadata,
            EntityQuery entityQuery)
        {
            if (!(queryable.Provider is INhQueryProvider))
            {
                return false;
            }

            return (!entityQuery.SkipCount.HasValue && !entityQuery.TakeCount.HasValue) || !elementMetadata.HasCompositeKey;
        }

        /// <summary>
        /// Applies an expand to the given <see cref="IQueryable"/>.
        /// </summary>
        /// <param name="queryable">The queryable to expand.</param>
        /// <param name="propertyPath">The path to expand.</param>
        /// <returns>The expanded queryable.</returns>
        protected virtual IQueryable ApplyQueryExpand(IQueryable queryable, string propertyPath)
        {
            return queryable.Include(propertyPath);
        }

        private IList ToList(dynamic queryable)
        {
            return Enumerable.ToList(queryable);
        }

        private Task<IList> ToListAsync(dynamic queryable, CancellationToken cancellationToken)
        {
            return queryable is INhQueryProvider ? ToListAsyncInternal() : Task.FromResult<IList>(ToList(queryable));

            async Task<IList> ToListAsyncInternal()
            {
                return await LinqExtensionMethods.ToListAsync(queryable, cancellationToken);
            }
        }
    }
}
