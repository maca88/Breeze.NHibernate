﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


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
    public partial class DefaultEntityQueryExecutor : IEntityQueryExecutor
    {

        /// <inheritdoc />
        public async Task<QueryResult> ApplyAndExecuteAsync(IQueryable queryable, string queryString, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
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
            var listResult = await (ToListAsync((dynamic)queryable, cancellationToken)).ConfigureAwait(false);
            if (postExecuteExpandPaths?.Count > 0)
            {
                _proxyInitializer.Initialize(listResult, postExecuteExpandPaths);
            }

            listResult = EntityQuery.AfterExecution?.Invoke(entityQuery, queryable, listResult) ?? listResult;

            return new QueryResult(listResult, inlineCount);
        }
    }
}
