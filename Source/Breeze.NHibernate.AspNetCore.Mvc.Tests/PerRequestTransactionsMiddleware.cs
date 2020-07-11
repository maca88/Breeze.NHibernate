using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NHibernate;

namespace Breeze.NHibernate.AspNetCore.Mvc.Tests
{
    /// <summary>
    /// Commits or Rollbacks transactions that were registered during a http request.
    /// </summary>
    public class PerRequestTransactionsMiddleware
    {
        private readonly RequestDelegate _next;

        public const string ItemsKey = "NHTransactions";

        public PerRequestTransactionsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
                if (!TryGetTransactions(httpContext, out var openedTransactions))
                {
                    return;
                }

                if (httpContext.Response.StatusCode >= 200 && httpContext.Response.StatusCode < 300)
                {
                    await CommitTransactions(openedTransactions);
                }
                else
                {
                    await RollbackTransactions(openedTransactions);
                }
            }
            catch (Exception)
            {
                if (TryGetTransactions(httpContext, out var openedTransactions))
                {
                    await RollbackTransactions(openedTransactions);
                }

                throw;
            }
        }

        public static void RegisterTransaction(HttpContext httpContext, ITransaction transaction)
        {
            var items = httpContext.Items;
            List<ITransaction> transactions;
            if (!items.TryGetValue(ItemsKey, out var value))
            {
                transactions = new List<ITransaction>();
                items.Add(ItemsKey, transactions);
            }
            else
            {
                transactions = (List<ITransaction>)value;
            }

            transactions.Add(transaction);
        }

        private static bool TryGetTransactions(HttpContext httpContext, out List<ITransaction> transactions)
        {
            if (!httpContext.Items.TryGetValue(ItemsKey, out var value))
            {
                transactions = null;
                return false;
            }

            transactions = (List<ITransaction>)value;
            return true;
        }

        private static async Task RollbackTransactions(List<ITransaction> transactions)
        {
            foreach (var transaction in transactions)
            {
                await transaction.RollbackAsync();
            }
        }

        private static async Task CommitTransactions(List<ITransaction> transactions)
        {
            foreach (var transaction in transactions)
            {
                await transaction.CommitAsync();
            }
        }
    }

    public static class TransactionsHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UsePerRequestTransactions(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PerRequestTransactionsMiddleware>();
        }
    }
}
