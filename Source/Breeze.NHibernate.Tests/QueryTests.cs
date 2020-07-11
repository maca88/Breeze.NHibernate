using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Breeze.Core;
using Breeze.NHibernate.Metadata;
using Breeze.NHibernate.Tests.Models;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NHibernate;
using Xunit;

namespace Breeze.NHibernate.Tests
{
    public partial class QueryTests : BaseDatabaseTest
    {
        public QueryTests(Bootstrapper bootstrapper) : base(bootstrapper)
        {
        }


        [Fact]
        public void TestWhereSyntheticProperty()
        {
            Test(
                session => session.Query<OrderProduct>(),
                () => new QueryString
                {
                    Where = new Dictionary<string, Predicate> {{"OrderId", new Predicate {Equal = 1}}}
                },
                (session, queryResult) =>
                {
                    Assert.Equal(session.Query<OrderProduct>().Where(o => o.Order.Id == 1).ToList(), queryResult.Results);
                });
        }

        [Fact]
        public void TestOrderBySyntheticProperty()
        {
            Test(
                session => session.Query<OrderProduct>(),
                () => new QueryString {OrderBy = new List<string> {"OrderId desc"}},
                (session, queryResult) =>
                {
                    Assert.Equal(session.Query<OrderProduct>().OrderByDescending(o => o.Order.Id).ToList(), queryResult.Results);
                });
        }

        [Fact]
        public void TestSelectSyntheticProperty()
        {
            Test(
                session => session.Query<OrderProduct>(),
                () => new QueryString {Select = new List<string> {"OrderId"}},
                (session, queryResult) =>
                {
                    dynamic results = queryResult.Results;
                    var expected = session.Query<OrderProduct>().Select(o => new { OrderId = o.Order.Id }).ToList();
                    for (var i = 0; i < expected.Count; i++)
                    {
                        Assert.Equal(expected[i].OrderId, results[i].OrderId);
                    }
                });
        }

        [Fact]
        public void TestExpandManyToOne()
        {
            Test(
                session => session.Query<OrderProduct>(),
                () => new QueryString {Expand = new List<string> {"Order"}},
                (session, queryResult) =>
                {
                    foreach (OrderProduct orderProduct in queryResult.Results)
                    {
                        Assert.False(NHibernateUtil.IsInitialized(orderProduct.Product));
                        Assert.True(NHibernateUtil.IsInitialized(orderProduct.Order));
                    }
                });
        }

        [Fact]
        public void TestExpandOneToMany()
        {
            Test(
                session => session.Query<Order>(),
                () => new QueryString {Expand = new List<string> {"Products"}},
                (session, queryResult) =>
                {
                    foreach (Order order in queryResult.Results)
                    {
                        Assert.True(NHibernateUtil.IsInitialized(order.Products));
                        Assert.Equal(10, order.Products.Count);
                        foreach (var orderProduct in order.Products)
                        {
                            Assert.False(NHibernateUtil.IsInitialized(orderProduct.Product));
                        }

                        Assert.False(NHibernateUtil.IsInitialized(order.FkProducts));
                    }
                });

            Test(
                session => session.Query<CompositeOrder>(),
                () => new QueryString {Expand = new List<string> {"CompositeOrderRows"}},
                (session, queryResult) =>
                {
                    foreach (CompositeOrder order in queryResult.Results)
                    {
                        Assert.True(NHibernateUtil.IsInitialized(order.CompositeOrderRows));
                        Assert.Equal(10, order.CompositeOrderRows.Count);
                        foreach (var row in order.CompositeOrderRows)
                        {
                            Assert.False(NHibernateUtil.IsInitialized(row.Product));
                        }
                    }
                });
        }

        [Fact]
        public void TestNestedExpand()
        {
            Test(
                session => session.Query<Order>(),
                () => new QueryString {Expand = new List<string> {"Products.Product"}},
                (session, queryResult) =>
                {
                    foreach (Order order in queryResult.Results)
                    {
                        Assert.True(NHibernateUtil.IsInitialized(order.Products));
                        Assert.Equal(10, order.Products.Count);
                        foreach (var orderProduct in order.Products)
                        {
                            Assert.True(NHibernateUtil.IsInitialized(orderProduct.Product));
                        }

                        Assert.False(NHibernateUtil.IsInitialized(order.FkProducts));
                    }
                });

            Test(
                session => session.Query<CompositeOrder>(),
                () => new QueryString {Expand = new List<string> {"CompositeOrderRows.Product"}},
                (session, queryResult) =>
                {
                    foreach (CompositeOrder order in queryResult.Results)
                    {
                        Assert.True(NHibernateUtil.IsInitialized(order.CompositeOrderRows));
                        Assert.Equal(10, order.CompositeOrderRows.Count);
                        foreach (var row in order.CompositeOrderRows)
                        {
                            Assert.True(NHibernateUtil.IsInitialized(row.Product));
                        }
                    }
                });
        }

        [Fact]
        public void TestExpandOneToManyWithTake()
        {
            Test(
                session => session.Query<Order>(),
                () => new QueryString {Expand = new List<string> {"Products"}, Take = 5},
                (session, queryResult) =>
                {
                    var orders = (List<Order>)queryResult.Results;
                    Assert.Equal(5, orders.Count);
                    foreach (Order order in orders)
                    {
                        Assert.True(NHibernateUtil.IsInitialized(order.Products));
                        Assert.Equal(10, order.Products.Count);
                        Assert.False(NHibernateUtil.IsInitialized(order.FkProducts));
                    }
                });

            Test(
                session => session.Query<CompositeOrder>(),
                () => new QueryString {Expand = new List<string> {"CompositeOrderRows"}, Take = 5},
                (session, queryResult) =>
                {
                    var orders = (List<CompositeOrder>)queryResult.Results;
                    Assert.Equal(5, orders.Count);
                    foreach (CompositeOrder order in orders)
                    {
                        Assert.True(NHibernateUtil.IsInitialized(order.CompositeOrderRows));
                        Assert.Equal(10, order.CompositeOrderRows.Count);
                    }
                });
        }

        private void Test(
            Func<ISession, IQueryable> getQuery,
            Func<QueryString> getQueryString,
            Action<ISession, QueryResult> validateAction)
        {
            using var container = CreateServiceProvider();
            var entityQueryExecutor = container.GetService<IEntityQueryExecutor>();
            using (var scope = container.CreateScope())
            using (var session = scope.ServiceProvider.GetService<ISession>())
            using (var transaction = session.BeginTransaction())
            {
                var query = getQuery(session);
                var qs = JsonConvert.SerializeObject(getQueryString());
                var queryResult = entityQueryExecutor.ApplyAndExecute(query, qs);
                validateAction(session, queryResult);

                transaction.Commit();
            }
        }
        
        private async Task TestAsync(
            Func<ISession, IQueryable> getQuery,
            Func<QueryString> getQueryString,
            Action<ISession, QueryResult> validateAction)
        {
            using var container = CreateServiceProvider();
            var entityQueryExecutor = container.GetService<IEntityQueryExecutor>();
            using (var scope = container.CreateScope())
            using (var session = scope.ServiceProvider.GetService<ISession>())
            using (var transaction = session.BeginTransaction())
            {
                var query = getQuery(session);
                var qs = JsonConvert.SerializeObject(getQueryString());
                var queryResult = await entityQueryExecutor.ApplyAndExecuteAsync(query, qs);
                validateAction(session, queryResult);

                await transaction.CommitAsync();
            }
        }

        public class QueryString : MetadataObject
        {
            public Dictionary<string, Predicate> Where
            {
                get => Get<Dictionary<string, Predicate>>(nameof(Where));
                set => Set(nameof(Where), value);
            }

            public List<string> OrderBy
            {
                get => Get<List<string>>(nameof(OrderBy));
                set => Set(nameof(OrderBy), value);
            }

            public List<string> Select
            {
                get => Get<List<string>>(nameof(Select));
                set => Set(nameof(Select), value);
            }

            public List<string> Expand
            {
                get => Get<List<string>>(nameof(Expand));
                set => Set(nameof(Expand), value);
            }

            public int? Take
            {
                get => Get<int?>(nameof(Take));
                set => Set(nameof(Take), value);
            }
        }

        public class Predicate : MetadataObject
        {
            public object Equal
            {
                get => Get<object>("eq");
                set => Set("eq", value);
            }
        }
    }
}
