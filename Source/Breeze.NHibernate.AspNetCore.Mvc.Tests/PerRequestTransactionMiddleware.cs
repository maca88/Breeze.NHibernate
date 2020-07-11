using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NHibernate;

namespace Breeze.NHibernate.AspNetCore.Mvc.Tests
{
    /// <summary>
    /// A middleware that opens and commits/rollbacks a transaction for each http request.
    /// </summary>
    public class PerRequestTransactionMiddleware
    {
        private readonly RequestDelegate _next;

        public PerRequestTransactionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext, ITransaction transaction)
        {
            try
            {
                await _next(httpContext);
                if (httpContext.Response.StatusCode >= 200 && httpContext.Response.StatusCode < 300)
                {
                    await transaction.CommitAsync();
                }
                else
                {
                    await transaction.RollbackAsync();
                }
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    public static class PerRequestTransactionMiddlewareExtensions
    {
        /// <summary>
        /// A middleware that opens and commits/rollbacks a transaction for each http request.
        /// </summary>
        public static IApplicationBuilder UsePerRequestTransaction(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PerRequestTransactionMiddleware>();
        }
    }
}
