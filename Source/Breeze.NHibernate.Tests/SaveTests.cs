﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Breeze.NHibernate.Configuration;
using Breeze.NHibernate.Serialization;
using Breeze.NHibernate.Tests.Models;
using Breeze.NHibernate.Tests.Validators;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NHibernate;
using Xunit;

namespace Breeze.NHibernate.Tests
{
    public partial class SaveTests : BaseDatabaseTest
    {
        public SaveTests(Bootstrapper bootstrapper) : base(bootstrapper)
        {
        }

        [Fact]
        public void TestAddOrder()
        {
            AddOrder();
        }

        [Fact]
        public void TestAddOrderWithSelfFinalOrder()
        {
            AddOrder(true);
        }

        [Fact]
        public void TestUpdateOrderWithSelfFinalOrder()
        {
            var id = AddOrder();
            Test(
                em =>
                {
                    var order = em.Get<Order>(id);
                    em.SetModified(order);
                    foreach (var row in order.Products)
                    {
                        row.OrderFinal = order;
                        em.SetModified(row);
                    }
                },
                (em, result) =>
                {
                    Assert.Empty(result.KeyMappings);
                    Assert.Equal(2, result.Entities.Count);
                    Assert.Empty(result.DeletedKeys);
                });
        }

        private long AddOrder(bool selfFinalOrder = false)
        {
            long? id = null;
            Test(
                em =>
                {
                    var order = em.CreateEntity<Order>();
                    order.Name = "test";
                    order.Active = true;
                    order.Status = OrderStatus.Delivered;
                    order.TotalPrice = 10.3m;
                    order.Address.City = "City";
                    order.Address.Street = "Street";

                    var orderRow = em.CreateEntity<OrderProduct>();
                    orderRow.Order = order;
                    if (selfFinalOrder)
                    {
                        orderRow.OrderFinal = order;
                    }

                    orderRow.TotalPrice = 20.2m;
                    orderRow.Quantity = 15;
                    orderRow.Product = em.CreateEntity<Product>();
                    orderRow.Product.Name = "product";
                    orderRow.Product.Category = "unknown";
                },
                (em, result) =>
                {
                    Assert.Equal(3, result.KeyMappings.Count);
                    Assert.Equal(3, result.Entities.Count);
                    Assert.Empty(result.DeletedKeys);

                    // Order
                    var keyMapping = result.KeyMappings.First(o => o.EntityTypeName == typeof(Order).FullName);
                    Assert.NotNull(keyMapping.RealValue);

                    var localOrder = em.LocalQuery<Order>().First();
                    var order = em.Get<Order>(keyMapping.RealValue);
                    var resultOrder = result.Entities.OfType<Order>().First();

                    Assert.Equal(localOrder.Id, keyMapping.TempValue);
                    Assert.Equal(order.Id, keyMapping.RealValue);
                    Assert.Equal(order.Id, resultOrder.Id);

                    // OrderProduct
                    keyMapping = result.KeyMappings.First(o => o.EntityTypeName == typeof(OrderProduct).FullName);
                    Assert.NotNull(keyMapping.RealValue);

                    var localOrderProduct = em.LocalQuery<OrderProduct>().First();
                    var orderProduct = em.Get<OrderProduct>(keyMapping.RealValue);
                    var resultOrderProduct = result.Entities.OfType<OrderProduct>().First();

                    Assert.Equal(localOrderProduct.Id, keyMapping.TempValue);
                    Assert.Equal(orderProduct.Id, keyMapping.RealValue);
                    Assert.Equal(orderProduct.Id, resultOrderProduct.Id);

                    // Product
                    keyMapping = result.KeyMappings.First(o => o.EntityTypeName == typeof(Product).FullName);
                    Assert.NotNull(keyMapping.RealValue);

                    var localProduct = em.LocalQuery<Product>().First();
                    var product = em.Get<Product>(keyMapping.RealValue);
                    var resultProduct = result.Entities.OfType<Product>().First();

                    Assert.Equal(localProduct.Id, keyMapping.TempValue);
                    Assert.Equal(product.Id, keyMapping.RealValue);
                    Assert.Equal(product.Id, resultProduct.Id);

                    id = resultOrder.Id;
                });
            Assert.NotNull(id);

            return id.Value;
        }

        [Fact]
        public void TestValidSaveModelOrder()
        {
            var createEntityInfo = 0;
            var beforeFetchEntities = false;
            var beforeApplyChanges = false;
            var beforeSave = false;
            var afterSave = false;
            var afterFlush = false;
            Test<Order>(
                em =>
                {
                    var order = em.CreateEntity<Order>();
                    order.Name = "test";
                    order.Active = true;
                    order.Status = OrderStatus.Delivered;
                    order.TotalPrice = 10.3m;
                    order.Address.City = "City";
                    order.Address.Street = "Street";

                    var orderRow = em.CreateEntity<OrderProduct>();
                    orderRow.Order = order;
                    orderRow.TotalPrice = 20.2m;
                    orderRow.Quantity = 15;
                    orderRow.Product = em.Query<Product>().First();
                },
                config =>
                {
                    config
                        .AfterCreateEntityInfo((info, options) =>
                        {
                            createEntityInfo++;
                            return true;
                        })
                        .BeforeFetchEntities((context) =>
                        {
                            Assert.Equal(2, createEntityInfo);
                            beforeFetchEntities = true;
                        })
                        .BeforeApplyChanges((entity, info, ctx) =>
                        {
                            Assert.True(beforeFetchEntities);
                            Assert.Equal(0L, entity.Id);
                            Assert.Null(entity.Name);
                            beforeApplyChanges = true;
                        })
                        .BeforeSaveChanges((entity, info, order, ctx) =>
                        {
                            Assert.True(beforeApplyChanges);
                            Assert.Equal(0L, entity.Id);
                            Assert.Equal("test", entity.Name);
                            beforeSave = true;
                        })
                        .AfterSaveChanges((entity, info, order, ctx) =>
                        {
                            Assert.True(beforeSave);
                            Assert.NotEqual(0L, entity.Id);
                            afterSave = true;
                        })
                        .AfterFlushChanges((entity, info, context, mappings) =>
                        {
                            Assert.True(afterSave);
                            afterFlush = true;
                        })
                        ;
                },
                (em, result) =>
                {
                    Assert.Equal(2, result.KeyMappings.Count);
                    Assert.Equal(2, result.Entities.Count);
                    Assert.Empty(result.DeletedKeys);

                    Assert.True(afterFlush);
                });
        }

        [Fact]
        public async Task TestValidSaveModelOrderAsync()
        {
            var createEntityInfo = 0;
            var beforeFetchEntities = false;
            var beforeApplyChanges = false;
            var beforeSave = false;
            var afterSave = false;
            var afterFlush = false;
            await TestAsync<Order>(
                em =>
                {
                    var order = em.CreateEntity<Order>();
                    order.Name = "test";
                    order.Active = true;
                    order.Status = OrderStatus.Delivered;
                    order.TotalPrice = 10.3m;
                    order.Address.City = "City";
                    order.Address.Street = "Street";

                    var orderRow = em.CreateEntity<OrderProduct>();
                    orderRow.Order = order;
                    orderRow.TotalPrice = 20.2m;
                    orderRow.Quantity = 15;
                    orderRow.Product = em.Query<Product>().First();
                },
                config =>
                {
                    config
                        .AfterCreateEntityInfo((info, options) =>
                        {
                            createEntityInfo++;
                            return true;
                        })
                        .BeforeFetchEntities((context, token) =>
                        {
                            Assert.Equal(2, createEntityInfo);
                            beforeFetchEntities = true;
                            return Task.CompletedTask;
                        })
                        .BeforeApplyChanges((entity, info, ctx, token) =>
                        {
                            Assert.True(beforeFetchEntities);
                            Assert.Equal(0L, entity.Id);
                            Assert.Null(entity.Name);
                            beforeApplyChanges = true;
                            return Task.CompletedTask;
                        })
                        .BeforeSaveChanges((entity, info, order, ctx, token) =>
                        {
                            Assert.True(beforeApplyChanges);
                            Assert.Equal(0L, entity.Id);
                            Assert.Equal("test", entity.Name);
                            beforeSave = true;
                            return Task.CompletedTask;
                        })
                        .AfterSaveChanges((entity, info, order, ctx, token) =>
                        {
                            Assert.True(beforeSave);
                            Assert.NotEqual(0L, entity.Id);
                            afterSave = true;
                            return Task.CompletedTask;
                        })
                        .AfterFlushChanges((entity, info, context, mappings, token) =>
                        {
                            Assert.True(afterSave);
                            afterFlush = true;
                            return Task.CompletedTask;
                        })
                        ;
                },
                (em, result) =>
                {
                    Assert.Equal(2, result.KeyMappings.Count);
                    Assert.Equal(2, result.Entities.Count);
                    Assert.Empty(result.DeletedKeys);

                    Assert.True(afterFlush);
                });
        }

        [Fact]
        public void TestDeepSaveRootModel()
        {
            Test<Dog>(
                em =>
                {
                    var level0 = em.CreateEntity<Dog>();
                    level0.Name = "level0";

                    var level1 = em.CreateEntity<Dog>();
                    level1.Name = "level1";
                    level1.Parent = level0;
                    level0.Children.Add(level1);

                    var level2 = em.CreateEntity<Dog>();
                    level2.Name = "level2";
                    level2.Parent = level1;
                    level1.Children.Add(level2);

                    var level3 = em.CreateEntity<Dog>();
                    level3.Name = "level3";
                    level3.Parent = level2;
                    level2.Children.Add(level3);
                },
                config => { },
                (em, result) => { });

            Assert.Throws<InvalidOperationException>(() =>
            {
                Test<Dog>(
                    em =>
                    {
                        var root = em.Query<Dog>().First(o => o.Name == "level0");
                        em.SetModified(root);

                        var level3 = em.Query<Dog>().First(o => o.Name == "level3");
                        level3.Pregnant = true;
                        em.SetModified(level3);
                    },
                    config => { },
                    (em, result) => { });
            });

            Test<Dog>(
                em =>
                {
                    var root = em.Query<Dog>().First(o => o.Name == "level0");
                    em.SetModified(root);

                    var level3 = em.Query<Dog>().First(o => o.Name == "level3");
                    level3.Pregnant = true;
                    em.SetModified(level3);
                },
                config => { config.ModelSaveValidator(new AggregateRootSaveValidator()); },
                (em, result) => { });
        }

        [Fact]
        public void TestDeepSaveRootModels()
        {
            Test<Dog>(
                em =>
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var level0 = em.CreateEntity<Dog>();
                        level0.Name = $"level0{i}";

                        var level1 = em.CreateEntity<Dog>();
                        level1.Name = $"level1{i}";
                        level1.Parent = level0;
                        level0.Children.Add(level1);

                        var level2 = em.CreateEntity<Dog>();
                        level2.Name = $"level2{i}";
                        level2.Parent = level1;
                        level1.Children.Add(level2);

                        var level3 = em.CreateEntity<Dog>();
                        level3.Name = $"level3{i}";
                        level3.Parent = level2;
                        level2.Children.Add(level3);
                    }
                },
                config => { config.ModelSaveValidator(new ModelCollectionSaveValidator()); },
                (em, result) => { });

            Assert.Throws<InvalidOperationException>(() =>
            {
                Test<Dog>(
                    em =>
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            var level = $"level0{i}";
                            var root = em.Query<Dog>().First(o => o.Name == level);
                            em.SetModified(root);

                            level = $"level3{i}";
                            var level3 = em.Query<Dog>().First(o => o.Name == level);
                            level3.Pregnant = true;
                            em.SetModified(level3);
                        }
                    },
                    config => { config.ModelSaveValidator(new AggregateRootSaveValidator()); },
                    (em, result) => { });
            });

            Test<Dog>(
                em =>
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var level = $"level0{i}";
                        var root = em.Query<Dog>().First(o => o.Name == level);
                        em.SetModified(root);

                        level = $"level3{i}";
                        var level3 = em.Query<Dog>().First(o => o.Name == level);
                        level3.Pregnant = true;
                        em.SetModified(level3);
                    }
                    
                },
                config => { config.ModelSaveValidator(new AggregateRootCollectionSaveValidator()); },
                (em, result) => { });
        }

        [Fact]
        public void TestInvalidSaveModelOrder()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                Test<Order>(
                    em =>
                    {
                        var order = em.CreateEntity<Order>();
                        order.Name = "test";
                        order.Active = true;
                        order.Status = OrderStatus.Delivered;
                        order.TotalPrice = 10.3m;
                        order.Address.City = "City";
                        order.Address.Street = "Street";

                        var orderRow = em.CreateEntity<OrderProduct>();
                        orderRow.Order = order;
                        orderRow.TotalPrice = 20.2m;
                        orderRow.Quantity = 15;
                        orderRow.Product = em.CreateEntity<Product>();
                        orderRow.Product.Name = "product";
                        orderRow.Product.Category = "unknown";
                    },
                    config => { },
                    (em, result) => { });
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                Test<Order>(
                    em =>
                    {
                        var order = em.CreateEntity<Order>();
                        order.Name = "test";
                        order.Active = true;
                        order.Status = OrderStatus.Delivered;
                        order.TotalPrice = 10.3m;
                        order.Address.City = "City";
                        order.Address.Street = "Street";

                        var orderRow = em.CreateEntity<OrderProduct>();
                        orderRow.Order = order;
                        orderRow.TotalPrice = 20.2m;
                        orderRow.Quantity = 15;
                        orderRow.Product = em.Query<Product>().First();

                        order = em.CreateEntity<Order>();
                        order.Name = "test";
                        order.Active = true;
                        order.Status = OrderStatus.Delivered;
                        order.TotalPrice = 10.3m;
                        order.Address.City = "City";
                        order.Address.Street = "Street";
                    },
                    config => { },
                    (em, result) => { });
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                Test<Order>(
                    em =>
                    {
                        var order = em.CreateEntity<Order>();
                        order.Name = "test";
                        order.Active = true;
                        order.Status = OrderStatus.Delivered;
                        order.TotalPrice = 10.3m;
                        order.Address.City = "City";
                        order.Address.Street = "Street";

                        var orderRow = em.CreateEntity<OrderProduct>();
                        orderRow.Order = em.Query<Order>().First();
                        orderRow.TotalPrice = 20.2m;
                        orderRow.Quantity = 15;
                        orderRow.Product = em.Query<Product>().First();
                    },
                    config => { },
                    (em, result) => { });
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                Test<Order>(
                    em =>
                    {
                        var orderRow = em.CreateEntity<OrderProduct>();
                        orderRow.Order = em.Query<Order>().First();
                        orderRow.TotalPrice = 20.2m;
                        orderRow.Quantity = 15;
                        orderRow.Product = em.Query<Product>().First();
                    },
                    config => { },
                    (em, result) => { });
            });
        }

        [Fact]
        public void AddOrderProductFk()
        {
            Test(
                em =>
                {
                    var order = em.CreateEntity<Order>();
                    order.Name = "test";
                    order.Active = true;
                    order.Status = OrderStatus.Delivered;
                    order.TotalPrice = 10.3m;
                    order.Address.City = "City";
                    order.Address.Street = "Street";

                    var orderRow = em.CreateEntity<OrderProductFk>();
                    orderRow.Order = order;
                    orderRow.OrderId = order.Id;
                    orderRow.TotalPrice = 20.2m;
                    orderRow.Quantity = 15;
                    orderRow.Product = em.CreateEntity<Product>();
                    orderRow.ProductId = orderRow.Product.Id;
                    orderRow.Product.Name = "product";
                    orderRow.Product.Category = "unknown";
                },
                (em, result) =>
                {
                    Assert.Equal(3, result.KeyMappings.Count);
                    Assert.Equal(3, result.Entities.Count);
                    Assert.Empty(result.DeletedKeys);

                    // OrderProductFk
                    var keyMapping = result.KeyMappings.First(o => o.EntityTypeName == typeof(OrderProductFk).FullName);
                    Assert.NotNull(keyMapping.RealValue);

                    var orderProduct = em.Get<OrderProductFk>(keyMapping.RealValue);
                    var resultOrderProduct = result.Entities.OfType<OrderProductFk>().First();

                    Assert.Equal(orderProduct.Product.Id, resultOrderProduct.ProductId);
                    Assert.Equal(orderProduct.Order.Id, resultOrderProduct.OrderId);
                });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestRemoveOrder(bool deleteParentFirst)
        {
            var id = AddOrder();
            Test(
                em =>
                {
                    var order = em.Get<Order>(id);
                    if (deleteParentFirst)
                    {
                        em.SetDeleted(order);

                        foreach (var row in order.Products)
                        {
                            em.SetDeleted(row);
                            em.SetDeleted(row.Product);
                        }
                    }
                    else
                    {
                        foreach (var row in order.Products)
                        {
                            em.SetDeleted(row.Product);
                            em.SetDeleted(row);
                        }

                        em.SetDeleted(order);
                    }
                },
                (em, result) =>
                {
                    Assert.Empty(result.KeyMappings);
                    Assert.Equal(3, result.Entities.Count);
                    Assert.Equal(3, result.DeletedKeys.Count);
                });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestAdditionalEntities(bool delete)
        {
            var id = AddOrder();
            Test(
                em =>
                {
                    var order = em.Get<Order>(id);
                    em.SetModified(order);
                },
                (em, result) =>
                {
                    Assert.Empty(result.KeyMappings);
                    Assert.Equal(5, result.Entities.Count);
                    Assert.Equal(delete ? 4 : 0, result.DeletedKeys.Count);
                },
                (session, configurator) => configurator.BeforeSaveChanges((saveOrder, context) =>
                {
                    var order = session.Query<Order>().First(o => o.Id != id);
                    NHibernateUtil.Initialize(order.Products);
                    var orderProduct = session.Query<OrderProduct>().First();
                    NHibernateUtil.Initialize(orderProduct.Order);

                    var compositeOrder = session.Query<CompositeOrder>().First();
                    NHibernateUtil.Initialize(compositeOrder.CompositeOrderRows);
                    var compositeOrderRow = session.Query<CompositeOrderRow>().First(o => o.CompositeOrder.Number != compositeOrder.Number);
                    NHibernateUtil.Initialize(compositeOrderRow.CompositeOrder);

                    var entities = new List<object> {order, orderProduct, compositeOrder, compositeOrderRow};
                    if (delete)
                    {
                        context.AddAdditionalEntities(entities, EntityState.Deleted);
                    }
                    else
                    {
                        context.AddAdditionalEntities(entities, EntityState.Modified);
                    }
                }));
        }

        [Fact]
        public void TestAddAdditionalEntities()
        {
            Test(
                em =>
                {
                    var order = em.CreateEntity<Order>();
                    order.Name = "test";
                    order.Active = true;
                    order.Status = OrderStatus.Delivered;
                    order.TotalPrice = 10.3m;
                    order.Address.City = "City";
                    order.Address.Street = "Street";
                },
                (em, result) =>
                {
                    Assert.Single(result.KeyMappings);
                    Assert.Equal(3, result.Entities.Count);
                    Assert.Empty(result.DeletedKeys);
                },
                (session, configurator) => configurator
                    .BeforeApplyChanges(context =>
                    {
                        var orderInfo = Assert.Single(context.SaveMap[typeof(Order)]);
                        Assert.NotNull(orderInfo);
                        var order = (Order)orderInfo.Entity;
                        var product = new Product {Name = "product", Category = "unknown"};
                        var orderRow = new OrderProduct
                        {
                            Order = order, TotalPrice = 20.2m, Quantity = 15, Product = product
                        };

                        context.AddAdditionalEntity(product, EntityState.Added);
                        context.AddAdditionalEntity(orderRow, EntityState.Added);
                    })
                    .ValidateDependencyGraph((graph, context) =>
                    {
                        Assert.Equal(3, graph.Count());
                        var orderProductNode = Assert.Single(graph.Where(o => o.EntityInfo.EntityType == typeof(OrderProduct)));
                        Assert.NotNull(orderProductNode);
                        Assert.Equal(2, orderProductNode.Parents.Count);
                    })
                    .BeforeSaveChanges((saveOrder, context) =>
                    {
                        Assert.Equal(3, saveOrder.Count);
                    }));
        }

        [Fact]
        public void TestVersionConcurrency()
        {
            using var container = CreateServiceProvider();
            var em = container.GetService<BreezeEntityManager>();
            var saveBundle = GetSaveBundle(container, em, _ =>
            {
                var order = em.Query<Order>().First();
                order.Name = "update";
                em.SetModified(order);

            });

            SaveChanges(container, saveBundle, em);
            Assert.Throws<EntityErrorsException>(() => SaveChanges(container, saveBundle, em));
        }

        [Fact]
        public void TestIdentifierModification()
        {
            using var container = CreateServiceProvider();
            var em = container.GetService<BreezeEntityManager>();
            var saveBundle = GetSaveBundle(container, em, _ =>
            {
                var order = em.Query<Order>().First();
                em.SetModified(order);
                order.SetId(9999);
            });

            var ex = Assert.Throws<EntityErrorsException>(() => SaveChanges(container, saveBundle, em));
            var error = Assert.Single(ex.EntityErrors);
            Assert.NotNull(error);
            Assert.Equal("Id", error.PropertyName);
        }

        [Fact]
        public void TestCompositeIdentifierModification()
        {
            using var container = CreateServiceProvider();
            var em = container.GetService<BreezeEntityManager>();
            var saveBundle = GetSaveBundle(container, em, _ =>
            {
                var order = em.Query<CompositeOrder>().First();
                em.SetModified(order);
                order.SetStatus("New");
            });

            var ex = Assert.Throws<EntityErrorsException>(() => SaveChanges(container, saveBundle, em));
            var error = Assert.Single(ex.EntityErrors);
            Assert.NotNull(error);
            Assert.Equal("Status", error.PropertyName);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestRemoveOrderProduct(bool modifyOrder)
        {
            var id = AddOrder();
            Test(
                em =>
                {
                    var order = em.Get<Order>(id);
                    if (modifyOrder)
                    {
                        order.Name = "name";
                        em.SetModified(order);
                    }

                    foreach (var row in order.Products)
                    {
                        em.SetDeleted(row);
                    }
                },
                (em, result) =>
                {
                    var serverOrder = em.Get<Order>(id);
                    if (modifyOrder)
                    {
                        Assert.Equal(0, serverOrder.Products.Count);
                    }
                    else
                    {
                        Assert.False(NHibernateUtil.IsInitialized(serverOrder.Products));
                    }
                    
                    Assert.Empty(result.KeyMappings);
                    Assert.Equal(modifyOrder ? 2 : 1, result.Entities.Count);
                    Assert.Single(result.DeletedKeys);
                });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSwitchOrderProductParent(bool modifyOrders)
        {
            var id = AddOrder();
            var id2 = AddOrder();
            long orderProductId = -1;
            long orderProduct2Id = -1;
            Test(
                em =>
                {
                    var order = em.Get<Order>(id);
                    var order2 = em.Get<Order>(id2);
                    if (modifyOrders)
                    {
                        order.Name = "name";
                        order2.Name = "name2";
                        em.SetModified(order);
                        em.SetModified(order2);
                    }

                    var orderProduct = order.Products.First();
                    var orderProduct2 = order2.Products.First();
                    orderProductId = orderProduct.Id;
                    orderProduct2Id = orderProduct2.Id;

                    orderProduct.Order = order2;
                    orderProduct2.Order = order;

                    em.SetModified(orderProduct);
                    em.SetModified(orderProduct2);
                },
                (em, result) =>
                {
                    var serverOrder = em.Get<Order>(id);
                    var serverOrder2 = em.Get<Order>(id2);
                    if (modifyOrders)
                    {
                        var orderProduct = Assert.Single(serverOrder.Products);
                        Assert.NotNull(orderProduct);
                        var orderProduct2 = Assert.Single(serverOrder2.Products);
                        Assert.NotNull(orderProduct2);
                        Assert.Equal(orderProduct2Id, orderProduct.Id);
                        Assert.Equal(orderProductId, orderProduct2.Id);
                    }
                    else
                    {
                        Assert.False(NHibernateUtil.IsInitialized(serverOrder.Products));
                        Assert.False(NHibernateUtil.IsInitialized(serverOrder2.Products));
                    }

                    Assert.Empty(result.KeyMappings);
                    Assert.Equal(modifyOrders ? 4 : 2, result.Entities.Count);
                    Assert.Empty(result.DeletedKeys);
                });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSwitchOrderProductFkParent(bool modifyOrders)
        {
            long orderId = -1;
            long order2Id = -1;
            long orderProductId = -1;
            long orderProduct2Id = -1;
            Test(
                em =>
                {
                    var orders = em.Query<Order>().Where(o => o.FkProducts.Any()).Take(2).ToList();
                    var order = orders[0];
                    orderId = order.Id;
                    var order2 = orders[1];
                    order2Id = order2.Id;
                    if (modifyOrders)
                    {
                        order.Name = "name";
                        order2.Name = "name2";
                        em.SetModified(order);
                        em.SetModified(order2);
                    }

                    var orderProduct = order.FkProducts.First();
                    var orderProduct2 = order2.FkProducts.First();
                    orderProductId = orderProduct.Id;
                    orderProduct2Id = orderProduct2.Id;

                    orderProduct.Order = order2;
                    orderProduct.OrderId = order2.Id;
                    orderProduct2.Order = order;
                    orderProduct2.OrderId = order.Id;

                    em.SetModified(orderProduct);
                    em.SetModified(orderProduct2);
                },
                (em, result) =>
                {
                    Assert.Empty(result.KeyMappings);
                    Assert.Equal(modifyOrders ? 4 : 2, result.Entities.Count);
                    Assert.Empty(result.DeletedKeys);

                    var orderProduct = Assert.Single(result.Entities.OfType<OrderProductFk>().Where(o => o.Id == orderProductId));
                    Assert.NotNull(orderProduct);
                    Assert.Equal(order2Id, orderProduct.OrderId);
                    var orderProduct2 = Assert.Single(result.Entities.OfType<OrderProductFk>().Where(o => o.Id == orderProduct2Id));
                    Assert.NotNull(orderProduct2);
                    Assert.Equal(orderId, orderProduct2.OrderId);

                    var serverOrder = em.Get<Order>(orderId);
                    var serverOrder2 = em.Get<Order>(order2Id);
                    if (modifyOrders)
                    {
                        Assert.Empty(serverOrder.FkProducts.Where(o => o.Id == orderProductId));
                        orderProduct = Assert.Single(serverOrder.FkProducts.Where(o => o.Id == orderProduct2Id));
                        Assert.NotNull(orderProduct);
                        Assert.Empty(serverOrder2.FkProducts.Where(o => o.Id == orderProduct2Id));
                        orderProduct2 = Assert.Single(serverOrder2.FkProducts.Where(o => o.Id == orderProductId));
                        Assert.NotNull(orderProduct2);
                    }
                    else
                    {
                        Assert.False(NHibernateUtil.IsInitialized(serverOrder.FkProducts));
                        Assert.False(NHibernateUtil.IsInitialized(serverOrder2.FkProducts));
                    }
                });
        }

        #region CompositeKey

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestAddCompositeOrder(bool deleteParentFirst)
        {
            CompositeOrder order = null;
            Test(
                em =>
                {

                    order = new CompositeOrder(2000, 1, "A");
                    em.SetAdded(order);
                    order.TotalPrice = 10.3m;

                    var orderRow = em.CreateEntity<CompositeOrderRow>();
                    orderRow.CompositeOrder = order;
                    orderRow.Price = 20.2m;
                    orderRow.Quantity = 15;
                    orderRow.Product = em.CreateEntity<Product>();
                    orderRow.Product.Name = "product";
                    orderRow.Product.Category = "unknown";
                },
                (em, result) =>
                {
                    Assert.Equal(2, result.KeyMappings.Count);
                    Assert.Equal(3, result.Entities.Count);
                    Assert.Empty(result.DeletedKeys);

                    // CompositeOrder
                    var localOrder = em.LocalQuery<CompositeOrder>().First();
                    var resultOrder = result.Entities.OfType<CompositeOrder>().First();

                    Assert.Equal(localOrder.Year, resultOrder.Year);
                    Assert.Equal(localOrder.Number, resultOrder.Number);
                    Assert.Equal(localOrder.Status, resultOrder.Status);

                    // CompositeOrderRow
                    var keyMapping = result.KeyMappings.First(o => o.EntityTypeName == typeof(CompositeOrderRow).FullName);
                    Assert.NotNull(keyMapping.RealValue);

                    var localOrderRow = em.LocalQuery<CompositeOrderRow>().First();
                    var orderRow = em.Get<CompositeOrderRow>(keyMapping.RealValue);
                    var resultOrderRow = result.Entities.OfType<CompositeOrderRow>().First();

                    Assert.Equal(localOrderRow.Id, keyMapping.TempValue);
                    Assert.Equal(orderRow.Id, keyMapping.RealValue);
                    Assert.Equal(orderRow.Id, resultOrderRow.Id);

                    // Product
                    keyMapping = result.KeyMappings.First(o => o.EntityTypeName == typeof(Product).FullName);
                    Assert.NotNull(keyMapping.RealValue);

                    var localProduct = em.LocalQuery<Product>().First();
                    var product = em.Get<Product>(keyMapping.RealValue);
                    var resultProduct = result.Entities.OfType<Product>().First();

                    Assert.Equal(localProduct.Id, keyMapping.TempValue);
                    Assert.Equal(product.Id, keyMapping.RealValue);
                    Assert.Equal(product.Id, resultProduct.Id);
                });

            TestRemoveCompositeOrder(deleteParentFirst, order);
        }

        private void TestRemoveCompositeOrder(bool deleteParentFirst, CompositeOrder id)
        {
            Test(
                em =>
                {
                    var order = em.Get<CompositeOrder>(id);
                    if (deleteParentFirst)
                    {
                        em.SetDeleted(order);

                        foreach (var row in order.CompositeOrderRows)
                        {
                            em.SetDeleted(row);
                            em.SetDeleted(row.Product);
                        }
                    }
                    else
                    {
                        foreach (var row in order.CompositeOrderRows)
                        {
                            em.SetDeleted(row.Product);
                            em.SetDeleted(row);
                        }

                        em.SetDeleted(order);
                    }
                },
                (em, result) =>
                {
                    Assert.Empty(result.KeyMappings);
                    Assert.Equal(3, result.Entities.Count);
                    Assert.Equal(3, result.DeletedKeys.Count);
                });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestAddCompositeOrderProduct(bool deleteParentFirst)
        {
            CompositeOrder order = null;
            Test(
                em =>
                {
                    order = new CompositeOrder(2000, 9, "A");
                    em.SetAdded(order);
                    order.TotalPrice = 10.3m;

                    var orderProduct = new CompositeOrderProduct(order, em.Query<Product>().First());
                    em.SetAdded(orderProduct);
                    orderProduct.Price = 20.2m;
                    orderProduct.Quantity = 15;

                    var orderProductRemark = em.CreateEntity<CompositeOrderProductRemark>();
                    orderProductRemark.CompositeOrderProduct = orderProduct;
                    orderProductRemark.Remark = "test";
                },
                (em, result) =>
                {
                    var keyMapping = Assert.Single(result.KeyMappings);
                    Assert.Equal(3, result.Entities.Count);
                    Assert.Empty(result.DeletedKeys);

                    // CompositeOrder
                    var localOrder = em.LocalQuery<CompositeOrder>().First();
                    var resultOrder = result.Entities.OfType<CompositeOrder>().First();

                    Assert.Equal(localOrder.Year, resultOrder.Year);
                    Assert.Equal(localOrder.Number, resultOrder.Number);
                    Assert.Equal(localOrder.Status, resultOrder.Status);

                    // CompositeOrderProductRemark
                    Assert.NotNull(keyMapping);
                    Assert.NotNull(keyMapping.RealValue);

                    var localRemark = em.LocalQuery<CompositeOrderProductRemark>().First();
                    var remark = em.Get<CompositeOrderProductRemark>(keyMapping.RealValue);
                    var resultRemark = result.Entities.OfType<CompositeOrderProductRemark>().First();

                    Assert.Equal(localRemark.Id, keyMapping.TempValue);
                    Assert.Equal(remark.Id, keyMapping.RealValue);
                    Assert.Equal(remark.Id, resultRemark.Id);
                });

            TestRemoveCompositeOrderProduct(deleteParentFirst, order);
        }

        private void TestRemoveCompositeOrderProduct(bool deleteParentFirst, CompositeOrder id)
        {
            Test(
                em =>
                {
                    var order = em.Get<CompositeOrder>(id);
                    if (deleteParentFirst)
                    {
                        em.SetDeleted(order);

                        foreach (var row in order.CompositeOrderProducts)
                        {
                            em.SetDeleted(row);
                            foreach (var remark in row.Remarks)
                            {
                                em.SetDeleted(remark);
                            }
                        }
                    }
                    else
                    {
                        foreach (var row in order.CompositeOrderProducts)
                        {
                            em.SetDeleted(row);
                            foreach (var remark in row.Remarks)
                            {
                                em.SetDeleted(remark);
                            }
                        }

                        em.SetDeleted(order);
                    }
                },
                (em, result) =>
                {
                    Assert.Empty(result.KeyMappings);
                    Assert.Equal(3, result.Entities.Count);
                    Assert.Equal(3, result.DeletedKeys.Count);

                    var deletedKey = Assert.Single(result.DeletedKeys.Where(o => o.EntityTypeName == "CompositeOrderProduct:#Breeze.NHibernate.Tests.Models"));
                    Assert.NotNull(deletedKey);
                    Assert.Equal(new object[] {2000, 9L, "A", 1L}, deletedKey.KeyValue);

                    deletedKey = Assert.Single(result.DeletedKeys.Where(o => o.EntityTypeName == "CompositeOrder:#Breeze.NHibernate.Tests.Models"));
                    Assert.NotNull(deletedKey);
                    Assert.Equal(new object[] {2000, 9L, "A"}, deletedKey.KeyValue);

                    var localRemark = em.LocalQuery<CompositeOrderProductRemark>().First();
                    deletedKey = Assert.Single(result.DeletedKeys.Where(o => o.EntityTypeName == "CompositeOrderProductRemark:#Breeze.NHibernate.Tests.Models"));
                    Assert.NotNull(deletedKey);
                    Assert.Equal(new object[] {localRemark.Id}, deletedKey.KeyValue);

                });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestAddCompositeOrderProductWithProduct(bool deleteParentFirst)
        {
            CompositeOrder order = null;
            Test(
                em =>
                {
                    order = new CompositeOrder(2000, 12, "A");
                    em.SetAdded(order);
                    order.TotalPrice = 10.3m;

                    var product = em.CreateEntity<Product>();
                    product.Name = "Test";

                    var orderProduct = new CompositeOrderProduct(order, product);
                    em.SetAdded(orderProduct);
                    orderProduct.Price = 20.2m;
                    orderProduct.Quantity = 15;

                    product = em.CreateEntity<Product>();
                    product.Name = "Test2";

                    orderProduct = new CompositeOrderProduct(order, product);
                    em.SetAdded(orderProduct);
                    orderProduct.Price = 40m;
                    orderProduct.Quantity = 5;

                },
                (em, result) =>
                {
                    Assert.Equal(2, result.KeyMappings.Count);
                    Assert.Equal(5, result.Entities.Count);
                    Assert.Empty(result.DeletedKeys);

                    // CompositeOrder
                    var localOrder = em.LocalQuery<CompositeOrder>().First();
                    var resultOrder = result.Entities.OfType<CompositeOrder>().First();

                    Assert.Equal(localOrder.Year, resultOrder.Year);
                    Assert.Equal(localOrder.Number, resultOrder.Number);
                    Assert.Equal(localOrder.Status, resultOrder.Status);
                });

            TestRemoveCompositeOrderProductWithProduct(deleteParentFirst, order);
        }

        private void TestRemoveCompositeOrderProductWithProduct(bool deleteParentFirst, CompositeOrder id)
        {
            Test(
                em =>
                {
                    var order = em.Get<CompositeOrder>(id);
                    if (deleteParentFirst)
                    {
                        em.SetDeleted(order);

                        foreach (var row in order.CompositeOrderProducts)
                        {
                            em.SetDeleted(row);
                            em.SetDeleted(row.Product);
                        }
                    }
                    else
                    {
                        foreach (var row in order.CompositeOrderProducts)
                        {
                            em.SetDeleted(row);
                            em.SetDeleted(row.Product);
                        }

                        em.SetDeleted(order);
                    }
                },
                (em, result) =>
                {
                    Assert.Empty(result.KeyMappings);
                    Assert.Equal(5, result.Entities.Count);
                    Assert.Equal(5, result.DeletedKeys.Count);
                });
        }

        #endregion

        #region OneToOne

        [Fact]
        public void TestAddPerson()
        {
            Test(
                em =>
                {
                    var person = em.CreateEntity<Person>();
                    person.Name = "test";
                    person.Surname = "test2";

                    var identityCard = em.CreateEntity<IdentityCard>();
                    identityCard.SetOwner(person);
                    identityCard.Code = "code";
                    person.IdentityCard = identityCard;

                    var passport = em.CreateEntity<Passport>();
                    passport.Owner = person;
                    passport.ExpirationDate = DateTime.UtcNow.AddYears(4);
                    passport.Country = "US";
                    passport.Number = 1234;
                    person.Passport = passport;
                },
                (em, result) =>
                {
                    Assert.Equal(2, result.KeyMappings.Count);
                    Assert.Equal(3, result.Entities.Count);
                    Assert.Empty(result.DeletedKeys);

                    // Person
                    var keyMapping = result.KeyMappings.First(o => o.EntityTypeName == typeof(Person).FullName);
                    Assert.NotNull(keyMapping.RealValue);

                    var localPerson = em.LocalQuery<Person>().First();
                    var serverPerson = em.Get<Person>(keyMapping.RealValue);
                    var resultPerson = result.Entities.OfType<Person>().First();

                    Assert.Equal(localPerson.Id, keyMapping.TempValue);
                    Assert.Equal(serverPerson.Id, keyMapping.RealValue);
                    Assert.Equal(serverPerson.Id, resultPerson.Id);

                    // IdentityCard
                    var serverCard = em.Get<IdentityCard>(keyMapping.RealValue);
                    var resultCard = result.Entities.OfType<IdentityCard>().First();

                    Assert.Equal(serverCard.Id, resultCard.Id);

                    // Passport
                    keyMapping = result.KeyMappings.First(o => o.EntityTypeName == typeof(Passport).FullName);
                    Assert.NotNull(keyMapping.RealValue);

                    var localPassport = em.LocalQuery<Passport>().First();
                    var serverPassport = em.Get<Passport>(keyMapping.RealValue);
                    var resultPassport = result.Entities.OfType<Passport>().First();

                    Assert.Equal(localPassport.Id, keyMapping.TempValue);
                    Assert.Equal(serverPassport.Id, keyMapping.RealValue);
                    Assert.Equal(serverPassport.Id, resultPassport.Id);
                });
        }

        [Fact]
        public void TestRemovePerson()
        {
            var personId = new object[1];
            var passportId = new object[1];
            var identityCardId = new object[1];
            Test(
                em =>
                {
                    var person = em.Query<Person>().First(o => o.IdentityCard != null && o.Passport != null);
                    personId[0] = person.Id;
                    identityCardId[0] = person.IdentityCard.Id;
                    passportId[0] = person.Passport.Id;
                    em.SetDeleted(person);
                    em.SetDeleted(person.Passport);
                    em.SetDeleted(person.IdentityCard);
                },
                (em, result) =>
                {
                    Assert.Empty(result.KeyMappings);
                    Assert.Equal(3, result.Entities.Count);
                    Assert.Equal(3, result.DeletedKeys.Count);

                    // Person
                    var key = result.DeletedKeys.First(o => o.EntityTypeName == "Person:#Breeze.NHibernate.Tests.Models");
                    Assert.Equal(personId, key.KeyValue);

                    // IdentityCard
                    key = result.DeletedKeys.First(o => o.EntityTypeName == "IdentityCard:#Breeze.NHibernate.Tests.Models");
                    Assert.Equal(identityCardId, key.KeyValue);

                    // Passport
                    key = result.DeletedKeys.First(o => o.EntityTypeName == "Passport:#Breeze.NHibernate.Tests.Models");
                    Assert.Equal(passportId, key.KeyValue);
                });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestRemoveIdentityCard(bool modifyParent)
        {
            var personId = new object[1];
            var identityCardId = new object[1];
            Test(
                em =>
                {
                    var person = em.Query<Person>().First(o => o.IdentityCard != null);
                    if (modifyParent)
                    {
                        em.SetModified(person);
                    }

                    personId[0] = person.Id;
                    identityCardId[0] = person.IdentityCard.Id;
                    em.SetDeleted(person.IdentityCard);
                },
                (em, result) =>
                {
                    Assert.Empty(result.KeyMappings);
                    Assert.Equal(modifyParent ? 2 : 1, result.Entities.Count);
                    Assert.Single(result.DeletedKeys);

                    var serverPerson = em.Get<Person>(personId[0]);
                    Assert.Null(serverPerson.IdentityCard);

                    // IdentityCard
                    var key = result.DeletedKeys.First(o => o.EntityTypeName == "IdentityCard:#Breeze.NHibernate.Tests.Models");
                    Assert.Equal(identityCardId, key.KeyValue);
                });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestRemovePassport(bool modifyParent)
        {
            var personId = new object[1];
            var passportId = new object[1];
            Test(
                em =>
                {
                    var person = em.Query<Person>().First(o => o.Passport != null);
                    if (modifyParent)
                    {
                        em.SetModified(person);
                    }

                    personId[0] = person.Id;
                    passportId[0] = person.Passport.Id;
                    em.SetDeleted(person.Passport);
                },
                (em, result) =>
                {
                    Assert.Empty(result.KeyMappings);
                    Assert.Equal(modifyParent ? 2 : 1, result.Entities.Count);
                    Assert.Single(result.DeletedKeys);

                    var serverPerson = em.Get<Person>(personId[0]);
                    Assert.Null(serverPerson.Passport);

                    // Passport
                    var key = result.DeletedKeys.First(o => o.EntityTypeName == "Passport:#Breeze.NHibernate.Tests.Models");
                    Assert.Equal(passportId, key.KeyValue);
                });
        }

        #endregion

        private void Test(
            Action<BreezeEntityManager> crudAction,
            Action<BreezeEntityManager, SaveResult> validateAction)
        {
            Test(null, crudAction, validateAction);
        }

        private void Test(
            Action<BreezeEntityManager> crudAction,
            Action<BreezeEntityManager, SaveResult> validateAction,
            Action<ISession, SaveChangesOptionsConfigurator> optionConfigureAction)
        {
            Test(null, crudAction, validateAction, optionConfigureAction);
        }

        private void Test(
            Action<ServiceProvider> configureAction,
            Action<BreezeEntityManager> crudAction,
            Action<BreezeEntityManager, SaveResult> validateAction)
        {
            using (var container = CreateServiceProvider())
            {
                configureAction?.Invoke(container);
                var em = container.GetService<BreezeEntityManager>();
                var saveBundle = GetSaveBundle(container, em, crudAction);

                SaveChanges(container, saveBundle, em, validateAction);
            }
        }

        private void Test(
            Action<ServiceProvider> configureAction,
            Action<BreezeEntityManager> crudAction,
            Action<BreezeEntityManager, SaveResult> validateAction,
            Action<ISession, SaveChangesOptionsConfigurator> optionConfigureAction)
        {
            using (var container = CreateServiceProvider())
            {
                configureAction?.Invoke(container);
                var em = container.GetService<BreezeEntityManager>();
                var saveBundle = GetSaveBundle(container, em, crudAction);

                SaveChanges(container, saveBundle, em, validateAction, optionConfigureAction);
            }
        }

        private Task TestAsync(
            Action<BreezeEntityManager> crudAction,
            Action<BreezeEntityManager, SaveResult> validateAction,
            Action<ISession, AsyncSaveChangesOptionsConfigurator> optionConfigureAction)
        {
            return TestAsync(null, crudAction, validateAction, optionConfigureAction);
        }

        private async Task TestAsync(
            Action<ServiceProvider> configureAction,
            Action<BreezeEntityManager> crudAction,
            Action<BreezeEntityManager, SaveResult> validateAction,
            Action<ISession, AsyncSaveChangesOptionsConfigurator> optionConfigureAction)
        {
            using (var container = CreateServiceProvider())
            {
                configureAction?.Invoke(container);
                var em = container.GetService<BreezeEntityManager>();
                var saveBundle = GetSaveBundle(container, em, crudAction);

                await SaveChangesAsync(container, saveBundle, em, validateAction, optionConfigureAction);
            }
        }

        private void Test<T>(
            Action<BreezeEntityManager> crudAction,
            Action<SaveChangesOptionsConfigurator<T>> optionConfigureAction,
            Action<BreezeEntityManager, SaveResult> validateAction)
        {
            Test(null, crudAction, optionConfigureAction, validateAction);
        }

        private Task TestAsync<T>(
            Action<BreezeEntityManager> crudAction,
            Action<AsyncSaveChangesOptionsConfigurator<T>> optionConfigureAction,
            Action<BreezeEntityManager, SaveResult> validateAction)
        {
            return TestAsync(null, crudAction, optionConfigureAction, validateAction);
        }

        private async Task TestAsync<T>(
            Action<ServiceProvider> configureAction,
            Action<BreezeEntityManager> crudAction,
            Action<AsyncSaveChangesOptionsConfigurator<T>> optionConfigureAction,
            Action<BreezeEntityManager, SaveResult> validateAction)
        {
            using var container = CreateServiceProvider();
            configureAction?.Invoke(container);

            var breezeContext = container.GetService<TestPersistenceManager>();
            var em = container.GetService<BreezeEntityManager>();
            var saveBundle = GetSaveBundle(container, em, crudAction);

            using var scope = container.CreateScope();
            using var session = scope.ServiceProvider.GetService<ISession>();
            using var transaction = session.BeginTransaction();
            var result = await breezeContext.SaveChangesAsync(saveBundle, optionConfigureAction);
            ValidateSaveResult(result, container);
            validateAction?.Invoke(em, result);

            transaction.Commit();
        }

        private void Test<T>(
            Action<ServiceProvider> configureAction,
            Action<BreezeEntityManager> crudAction,
            Action<SaveChangesOptionsConfigurator<T>> optionConfigureAction,
            Action<BreezeEntityManager, SaveResult> validateAction)
        {
            using var container = CreateServiceProvider();
            configureAction?.Invoke(container);

            var breezeContext = container.GetService<TestPersistenceManager>();
            var em = container.GetService<BreezeEntityManager>();
            var saveBundle = GetSaveBundle(container, em, crudAction);

            using (var scope = container.CreateScope())
            using (var session = scope.ServiceProvider.GetService<ISession>())
            using (var transaction = session.BeginTransaction())
            {
                var result = breezeContext.SaveChanges(saveBundle, optionConfigureAction);
                ValidateSaveResult(result, container);
                validateAction?.Invoke(em, result);

                transaction.Commit();
            }
        }

        private static SaveBundle GetSaveBundle(
            ServiceProvider container,
            BreezeEntityManager em,
            Action<BreezeEntityManager> crudAction = null)
        {
            SaveBundle saveBundle;
            using (var scope = container.CreateScope())
            using (var session = scope.ServiceProvider.GetService<ISession>())
            using (var transaction = session.BeginTransaction())
            {
                em.SetSessionProvider(scope.ServiceProvider.GetService<ISessionProvider>());
                crudAction?.Invoke(em);
                saveBundle = em.GetSaveBundle();

                transaction.Rollback();
            }

            return saveBundle;
        }

        private static void SaveChanges(
            ServiceProvider container,
            SaveBundle saveBundle,
            BreezeEntityManager em,
            Action<BreezeEntityManager, SaveResult> validateAction = null)
        {
            using (var scope = container.CreateScope())
            using (var session = scope.ServiceProvider.GetService<ISession>())
            using (var transaction = session.BeginTransaction())
            {
                em.SetSessionProvider(scope.ServiceProvider.GetService<ISessionProvider>());
                var persistenceManager = scope.ServiceProvider.GetService<TestPersistenceManager>();
                var result = persistenceManager.SaveChanges(saveBundle);
                ValidateSaveResult(result, container);
                validateAction?.Invoke(em, result);

                transaction.Commit();
            }
        }

        private static async Task SaveChangesAsync(
            ServiceProvider container,
            SaveBundle saveBundle,
            BreezeEntityManager em,
            Action<BreezeEntityManager, SaveResult> validateAction = null)
        {
            using (var scope = container.CreateScope())
            using (var session = scope.ServiceProvider.GetService<ISession>())
            using (var transaction = session.BeginTransaction())
            {
                em.SetSessionProvider(scope.ServiceProvider.GetService<ISessionProvider>());
                var persistenceManager = scope.ServiceProvider.GetService<TestPersistenceManager>();
                var result = await persistenceManager.SaveChangesAsync(saveBundle);
                ValidateSaveResult(result, container);
                validateAction?.Invoke(em, result);

                transaction.Commit();
            }
        }

        private static void SaveChanges(
            ServiceProvider container,
            SaveBundle saveBundle,
            BreezeEntityManager em,
            Action<BreezeEntityManager, SaveResult> validateAction,
            Action<ISession, SaveChangesOptionsConfigurator> optionConfigureAction)
        {
            using (var scope = container.CreateScope())
            using (var session = scope.ServiceProvider.GetService<ISession>())
            using (var transaction = session.BeginTransaction())
            {
                em.SetSessionProvider(scope.ServiceProvider.GetService<ISessionProvider>());
                var configurator = optionConfigureAction != null
                    ? c => optionConfigureAction(session, c)
                    : (Action<SaveChangesOptionsConfigurator>) null;
                var persistenceManager = scope.ServiceProvider.GetService<TestPersistenceManager>();
                var result = persistenceManager.SaveChanges(saveBundle, configurator);
                ValidateSaveResult(result, container);
                validateAction?.Invoke(em, result);

                transaction.Commit();
            }
        }

        private static async Task SaveChangesAsync(
            ServiceProvider container,
            SaveBundle saveBundle,
            BreezeEntityManager em,
            Action<BreezeEntityManager, SaveResult> validateAction,
            Action<ISession, AsyncSaveChangesOptionsConfigurator> optionConfigureAction)
        {
            using (var scope = container.CreateScope())
            using (var session = scope.ServiceProvider.GetService<ISession>())
            using (var transaction = session.BeginTransaction())
            {
                em.SetSessionProvider(scope.ServiceProvider.GetService<ISessionProvider>());
                var configurator = optionConfigureAction != null
                    ? c => optionConfigureAction(session, c)
                    : (Action<AsyncSaveChangesOptionsConfigurator>)null;
                var persistenceManager = scope.ServiceProvider.GetService<TestPersistenceManager>();
                var result = await persistenceManager.SaveChangesAsync(saveBundle, configurator);
                ValidateSaveResult(result, container);
                validateAction?.Invoke(em, result);

                transaction.Commit();
            }
        }

        private static void ValidateSaveResult(SaveResult saveResult, IServiceProvider serviceProvider)
        {
            var jsonSettings = serviceProvider.GetService<IJsonSerializerSettingsProvider>().GetDefault();
            var json = JsonConvert.SerializeObject(saveResult, jsonSettings);
            var jResult = JsonConvert.DeserializeObject<JObject>(json, jsonSettings);
            var entities = jResult.Property("Entities").Value.Value<JArray>();
            foreach (var entity in entities)
            {
                var jEntity = Assert.IsType<JObject>(entity);
                foreach (var property in jEntity.Properties())
                {
                    switch (property.Value)
                    {
                        case JArray _:
                            throw new InvalidOperationException("JArray in entity save result");
                        case JObject _ when property.Name != "Address":
                            throw new InvalidOperationException("JObject in entity save result");
                    }
                }
            }

            var deletedKeys = jResult.Property("DeletedKeys").Value.Value<JArray>();
            foreach (var deletedKey in deletedKeys)
            {
                var jDeletedKey = Assert.IsType<JObject>(deletedKey);
                var jKey = Assert.IsType<JArray>(jDeletedKey.Property("KeyValue").Value);
                foreach (var key in jKey)
                {
                    switch (key)
                    {
                        case JArray _:
                            throw new InvalidOperationException("JArray in deleted key");
                        case JObject _:
                            throw new InvalidOperationException("JObject in deleted key");
                    }
                }
            }
        }
    }
}
