using System.Linq;
using Breeze.Core;

namespace Breeze.NHibernate
{
    /// <summary>
    /// Applies the query string to the given NHibernate linq query and executes it.
    /// </summary>
    public partial interface IEntityQueryExecutor
    {
        /// <summary>
        /// Whether the query string should be applied to the given queryable.
        /// </summary>
        /// <param name="queryable">The queryable to check.</param>
        /// <param name="queryString">The query string to apply.</param>
        /// <returns>Whether the query string should be applied.</returns>
        bool ShouldApplyAndExecute(IQueryable queryable, string queryString);

        /// <summary>
        /// Applies the <see cref="EntityQuery"/> data to the given NHibernate linq query and executes it.
        /// </summary>
        /// <param name="queryable">The NHibernate linq query.</param>
        /// <param name="queryString">The query string to apply.</param>
        /// <returns>The applied query result.</returns>
        QueryResult ApplyAndExecute(IQueryable queryable, string queryString);
    }
}
