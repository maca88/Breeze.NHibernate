using System;
using System.Collections.Generic;
using System.Linq;
using Breeze.NHibernate.Configuration;
using Breeze.NHibernate.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using NHibernate;
using NHibernate.Engine;
using NHibernate.Linq;
using NHibernate.Proxy;
using Xunit;
using Breeze.NHibernate.Tests.Extensions;
using Breeze.NHibernate.Tests.Models;
using Newtonsoft.Json;

namespace Breeze.NHibernate.Tests
{
    public class SerializationTests : BaseDatabaseTest
    {
        public SerializationTests(Bootstrapper bootstrapper) : base(bootstrapper)
        {
        }

        [Fact]
        public void TestSyntheticForeignKeyProperties()
        {
            TestObject(
                s => s.Query<OrderProduct>().First(),
                (jObject, orderProduct) =>
                {
                    Assert.NotNull(jObject.Property("OrderId"));
                    Assert.Equal(orderProduct.Order.Id, jObject.Property("OrderId").Value.Value<long>());
                    Assert.Null(jObject.Property("Order"));

                    Assert.NotNull(jObject.Property("ProductId"));
                    Assert.Equal(orderProduct.Product.Id, jObject.Property("ProductId").Value.Value<long>());
                    Assert.Null(jObject.Property("Product"));
                });
        }

        [Fact]
        public void TestSyntheticPrimaryKeyProperties()
        {
            TestObject(
                s => s.Query<CompositeOrderProduct>().First(),
                (jObject, orderProduct) =>
                {
                    Assert.NotNull(jObject.Property("ProductId"));
                    Assert.Equal(orderProduct.Product.Id, jObject.Property("ProductId").Value.Value<long>());

                    var order = orderProduct.CompositeOrder;
                    Assert.Equal(order.Year, jObject.Property("CompositeOrderYear").Value.Value<int>());
                    Assert.NotNull(jObject.Property("CompositeOrderStatus"));
                    Assert.Equal(order.Status, jObject.Property("CompositeOrderStatus").Value.Value<string>());
                    Assert.NotNull(jObject.Property("CompositeOrderNumber"));
                    Assert.Equal(order.Number, jObject.Property("CompositeOrderNumber").Value.Value<long>());
                });
        }

        [Fact]
        public void TestSyntheticCompositeForeignKeyProperties()
        {
            TestObject(
                s => s.Query<CompositeOrderRow>().First(),
                (jObject, value) =>
                {
                    // Avoid load composite order
                    var order = (CompositeOrder)((INHibernateProxy)value.CompositeOrder).HibernateLazyInitializer.Identifier;

                    Assert.Null(jObject.Property("CompositeOrder"));
                    Assert.NotNull(jObject.Property("CompositeOrderYear"));
                    Assert.Equal(order.Year, jObject.Property("CompositeOrderYear").Value.Value<int>());
                    Assert.NotNull(jObject.Property("CompositeOrderStatus"));
                    Assert.Equal(order.Status, jObject.Property("CompositeOrderStatus").Value.Value<string>());
                    Assert.NotNull(jObject.Property("CompositeOrderNumber"));
                    Assert.Equal(order.Number, jObject.Property("CompositeOrderNumber").Value.Value<long>());

                    Assert.NotNull(jObject.Property("ProductId"));
                    Assert.Equal(value.Product.Id, jObject.Property("ProductId").Value.Value<long>());
                    Assert.Null(jObject.Property("Product"));
                });
        }

        [Fact]
        public void TestSyntheticCompositeForeignKeyOnSyntheticPrimaryKeyProperties()
        {
            TestObject(
                s => s.Query<CompositeOrderProductRemark>().First(),
                (jObject, remark) =>
                {
                    Assert.Null(jObject.Property("CompositeOrderProduct"));
                    var orderProduct = (CompositeOrderProduct)((INHibernateProxy)remark.CompositeOrderProduct).HibernateLazyInitializer.Identifier;
                    Assert.NotNull(jObject.Property("CompositeOrderProductProductId"));
                    Assert.Equal(orderProduct.Product.Id, jObject.Property("CompositeOrderProductProductId").Value.Value<long>());

                    var order = orderProduct.CompositeOrder;
                    Assert.Equal(order.Year, jObject.Property("CompositeOrderProductCompositeOrderYear").Value.Value<int>());
                    Assert.NotNull(jObject.Property("CompositeOrderProductCompositeOrderStatus"));
                    Assert.Equal(order.Status, jObject.Property("CompositeOrderProductCompositeOrderStatus").Value.Value<string>());
                    Assert.NotNull(jObject.Property("CompositeOrderProductCompositeOrderNumber"));
                    Assert.Equal(order.Number, jObject.Property("CompositeOrderProductCompositeOrderNumber").Value.Value<long>());
                });
        }

        [Fact]
        public void TestNotMappedGetter()
        {
            var configurators = new List<Action<IBreezeConfigurator>>
            {
                c => c.ConfigureModel<OrderProduct>().ForMember(o => o.ProductCategory, o => o.Include()),
                c => c.ConfigureModel<OrderProduct>().ForMember(o => o.ProductCategory, o => o.Serialize(true)),
                c => c.ConfigureModelMembers(o => true, (info, configurator) => configurator.Include()),
                c => c.ConfigureModelMembers(o => true, (info, configurator) => configurator.Serialize(true)),
                c => c.ConfigureModels(o => o == typeof(OrderProduct), (type, configurator) => configurator.ForMember("ProductCategory", o => o.Include())),
                c => c.ConfigureModels(o => o == typeof(OrderProduct), (type, configurator) => configurator.ForMember("ProductCategory", o => o.Serialize(true)))
            };

            foreach (var configurator in configurators)
            {
                TestObject(
                    s => s.Query<OrderProduct>().First(),
                    container => configurator(container.GetService<IBreezeConfigurator>()),
                    serializeAction =>
                    {
                        var e = Assert.Throws<InvalidOperationException>(serializeAction);
                        Assert.StartsWith(
                            "A lazy load occurred when serializing property 'ProductCategory' from type Breeze.NHibernate.Tests.Models.OrderProduct",
                            e.Message);
                    });
            }


            

            TestObject(
                s => s.Query<OrderProduct>().First(),
                container => container.GetService<IBreezeConfigurator>()
                    .ConfigureModelMembers(o => true, (info, configurator) => configurator.Include()),
                serializeAction =>
                {
                    var e = Assert.Throws<InvalidOperationException>(serializeAction);
                    Assert.StartsWith(
                        "A lazy load occurred when serializing property 'ProductCategory' from type Breeze.NHibernate.Tests.Models.OrderProduct",
                        e.Message);
                });

            TestObject(
                s => s.Query<OrderProduct>().First(),
                container => container.GetService<IBreezeConfigurator>()
                    .ConfigureModels(o => o == typeof(OrderProduct), (type, configurator) => configurator.ForMember("ProductCategory", o => o.Include())),
                serializeAction =>
                {
                    var e = Assert.Throws<InvalidOperationException>(serializeAction);
                    Assert.StartsWith(
                        "A lazy load occurred when serializing property 'ProductCategory' from type Breeze.NHibernate.Tests.Models.OrderProduct",
                        e.Message);
                });

            TestObject(
                s => s.Query<OrderProduct>().Include(o => o.Product).First(),
                container => container.GetService<IBreezeConfigurator>()
                    .ConfigureModel<OrderProduct>().ForMember(o => o.ProductCategory, o => o.Include()),
                (jObject, value) =>
                {
                    Assert.NotNull(jObject.Property("ProductCategory"));
                    Assert.Equal(value.Product.Category, jObject.Property("ProductCategory").Value.Value<string>());

                    Assert.NotNull(jObject.Property("ProductId"));
                    Assert.Equal(value.Product.Id, jObject.Property("ProductId").Value.Value<long>());
                    Assert.NotNull(jObject.Property("Product"));
                    var product = Assert.IsType<JObject>(jObject.Property("Product").Value);
                    Assert.NotNull(product.Property("Name"));
                    Assert.NotNull(product.Property("Name").Value.Value<string>());
                });
        }

        [Fact]
        public void TestLazyProperty()
        {
            TestObject(
                s => s.Query<Attachment>().First(),
                (jObject, value) =>
                {
                    Assert.NotNull(jObject.Property("Id"));
                    Assert.Null(jObject.Property("Content"));
                });
        }

        [Fact]
        public void TestFetchedLazyProperty()
        {
            TestObject(
                s => s.Query<Attachment>().Fetch(o => o.Content).First(),
                (jObject, value) =>
                {
                    Assert.NotNull(jObject.Property("Id"));
                    Assert.NotNull(jObject.Property("Content"));
                });
        }

        [Fact]
        public void TestAnonymousSelectionEntityProxy()
        {
            TestObject(
                s => s.Query<Order>().Select(o => new {Product = o.Products.Select(p => p.Product).First()}).First(),
                (jObject, value) =>
                {
                    Assert.Null(jObject.Property("ProductId"));
                    Assert.Null(jObject.Property("Product"));
                });
        }

        [Fact]
        public void TestDtoSelectionEntityProxy()
        {
            TestObject(
                s => s.Query<Order>().Select(o => new ProductDto { Product = o.Products.Select(p => p.Product).First() }).First(),
                serializeAction =>
                {
                    var e = Assert.Throws<InvalidOperationException>(serializeAction);
                    Assert.StartsWith(
                        "A lazy load occurred when serializing property 'ProductName' from type Breeze.NHibernate.Tests.SerializationTests+ProductDto",
                        e.Message);
                });

            TestObject(
                s => s.Query<Order>().Select(o => new ProductDto { Product = o.Products.Select(p => p.Product).First() }).First(),
                container => container.GetService<IBreezeConfigurator>()
                    .ConfigureModel<ProductDto>().ForMember(o => o.ProductName, o => o.Ignore()),
                (jObject, value) =>
                {
                    Assert.Null(jObject.Property("ProductId"));
                    Assert.Null(jObject.Property("ProductName"));
                    Assert.Null(jObject.Property("Product"));
                });
        }

        [Fact]
        public void TestDtoSelectionCollection()
        {
            TestObject(
                s => s.Query<Order>().Select(o => new ProductDto { OrderProducts = o.Products }).First(),
                serializeAction =>
                {
                    var e = Assert.Throws<InvalidOperationException>(serializeAction);
                    Assert.StartsWith(
                        "A lazy load occurred when serializing property 'Order' from type Breeze.NHibernate.Tests.SerializationTests+ProductDto",
                        e.Message);
                });

            TestObject(
                s => s.Query<Order>().Select(o => new ProductDto {OrderProducts = o.Products}).First(),
                container => container.GetService<IBreezeConfigurator>()
                    .ConfigureModel<ProductDto>().ForMember(o => o.Order, o => o.Ignore()),
                (jObject, value) =>
                {
                    Assert.Null(jObject.Property("Order"));
                    Assert.NotNull(jObject.Property("Product"));
                    Assert.Null(jObject.Property("Product").Value.Value<Product>());
                    Assert.NotNull(jObject.Property("OrderProducts"));
                    var items = jObject.Property("OrderProducts").Value.Value<JArray>();
                    Assert.Single(items);
                    var item = Assert.IsType<JObject>(items.First);
                    Assert.NotNull(item.Property("ProductId"));
                    Assert.NotNull(item.Property("OrderId"));
                });
        }

        [Fact]
        public void TestCollectionProxy()
        {
            TestObject(
                s => new {OrderProducts = s.Query<Order>().First().Products},
                (jObject, value) =>
                {
                    Assert.Null(jObject.Property("OrderProducts"));
                });
        }

        [Fact]
        public void TestSyntheticProperty()
        {
            TestObject(
                s => s.Query<Order>().First(),
                container => container.GetService<IBreezeConfigurator>()
                    .ConfigureModel<Order>().AddSyntheticMember("Test", o => o.Id),
                (jObject, value) =>
                {
                    Assert.NotNull(jObject.Property("Status"));
                    Assert.NotNull(jObject.Property("Name"));
                    Assert.Null(jObject.Property("OrderProducts"));
                    Assert.NotNull(jObject.Property("Test"));
                    Assert.Equal(value.Id, jObject.Property("Test").Value.Value<long>());
                });
        }

        [Fact]
        public void TestIgnoreAssociationProperty()
        {
            TestObject(
                s => s.Query<OrderProduct>().First(),
                container => container.GetService<IBreezeConfigurator>()
                    .ConfigureModel<OrderProduct>().ForMember(o => o.Order, o => o.Ignore()),
                (jObject, value) =>
                {
                    Assert.Null(jObject.Property("Order"));
                    Assert.Null(jObject.Property("OrderId"));
                });

            TestObject(
                s => s.Query<OrderProductFk>().First(),
                container => container.GetService<IBreezeConfigurator>()
                    .ConfigureModel<OrderProductFk>().ForMember(o => o.Order, o => o.Ignore()),
                (jObject, value) =>
                {
                    Assert.Null(jObject.Property("Order"));
                    Assert.NotNull(jObject.Property("OrderId"));
                });
        }

        [Fact]
        public void TestIgnoreAssociationKeepFkProperty()
        {
            TestObject(
                s => s.Query<OrderProduct>().First(),
                container => container.GetService<IBreezeConfigurator>()
                    .ConfigureModel<OrderProduct>()
                    .ForMember(o => o.Order, o => o.Ignore())
                    .ForSyntheticMember<long>("OrderId", o => o.Include()),
                (jObject, value) =>
                {
                    Assert.Null(jObject.Property("Order"));
                    Assert.NotNull(jObject.Property("OrderId"));
                });
        }

        [Fact]
        public void TestEntityProxyAndImplementation()
        {
            TestObject(
                s =>
                {
                    var order = s.Load<Order>(1L);
                    var order2 = ((INHibernateProxy) order).HibernateLazyInitializer.GetImplementation();
                    return new {Order1 = order, Order2 = order2};
                },
                (jObject, value) =>
                {
                    Assert.NotNull(jObject.Property("Order1"));
                    Assert.NotNull(jObject.Property("Order2"));
                    var order1 = Assert.IsType<JObject>(jObject.Property("Order1").Value);
                    var order2 = Assert.IsType<JObject>(jObject.Property("Order2").Value);

                    Assert.NotNull(order2.Property("$ref"));
                    Assert.Equal(order1.Property("$id").Value.Value<int>(), order2.Property("$ref").Value.Value<int>());
                });
        }

        private class ProductDto
        {
            public Product Product { get; internal set; }

            public ISet<OrderProduct> OrderProducts { get; set; }

            public string ProductName => Product?.Name;

            public Order Order => OrderProducts?.FirstOrDefault(o => o.Product.Category != null)?.Order;
        }

        private void TestObject<T>(Func<ISession, T> getValue, Action<JObject, T> validateAction)
        {
            TestObject(getValue, null, validateAction, null);
        }

        private void TestObject<T>(Func<ISession, T> getValue, Action<ServiceProvider> configureAction, Action<JObject, T> validateAction)
        {
            TestObject(getValue, configureAction, validateAction, null);
        }

        private void TestObject<T>(Func<ISession, T> getValue, Action<Action> serializationAction)
        {
            TestObject(getValue, null, null, serializationAction);
        }

        private void TestObject<T>(Func<ISession, T> getValue, Action<ServiceProvider> configureAction, Action<Action> serializationAction)
        {
            TestObject(getValue, configureAction, null, serializationAction);
        }

        private void TestObject<T>(Func<ISession, T> getValue,
            Action<ServiceProvider> configureAction,
            Action<JObject, T> validateAction,
            Action<Action> serializationAction)
        {
            using var container = CreateServiceProvider();
            using var scope = container.CreateScope();
            configureAction?.Invoke(container);
            using var session = scope.ServiceProvider.GetService<ISession>();
            using var transaction = session.BeginTransaction();

            var serializerSettingsProvider = container.GetService<IJsonSerializerSettingsProvider>();
            var serializer = JsonSerializer.Create(serializerSettingsProvider.GetDefault());

            var value = getValue(session);
            if (serializationAction != null)
            {
                serializationAction(() => serializer.Serialize(value));
                return;
            }

            using var guard = GuardForLazyLoad(session);
            var json = serializer.Serialize(value);
            var jObject = serializer.Deserialize<JObject>(json);
            validateAction(jObject, value);
        }

        private static IDisposable GuardForLazyLoad(ISession session)
        {
            return new LazyLoadGuard(session.GetSessionImplementation());
        }

        private class LazyLoadGuard : IDisposable
        {
            private readonly ISessionImplementor _session;
            private readonly int _totalObjects;

            public LazyLoadGuard(ISessionImplementor session)
            {
                _session = session;
                _totalObjects = session.PersistenceContext.CollectionsByKey.Count +
                                session.PersistenceContext.EntitiesByKey.Count;
            }

            public void Dispose()
            {
                var totalObjects = _session.PersistenceContext.CollectionsByKey.Count +
                                   _session.PersistenceContext.EntitiesByKey.Count;

                Assert.Equal(_totalObjects, totalObjects);
            }
        }
    }
}
