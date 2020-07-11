using System;
using System.Linq;
using Breeze.NHibernate.Tests.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;
using Environment = NHibernate.Cfg.Environment;
using NHConfiguration = NHibernate.Cfg.Configuration;

namespace Breeze.NHibernate.Tests
{
    public abstract class BaseDatabaseTest : BaseTest
    {
        protected static readonly NHibernateOptions NHibernateOptions;

        static BaseDatabaseTest()
        {
            var environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{environment}.json", true)
                .Build();
            NHibernateOptions = config.GetSection("NHibernate").Get<NHibernateOptions>();
        }

        protected BaseDatabaseTest(Bootstrapper bootstrapper) : base(bootstrapper)
        {
        }

        protected override void RegisterTypes(ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(s => CreateNHibernateConfiguration());
            serviceCollection.AddSingleton(s => s.GetService<NHConfiguration>().BuildSessionFactory());
            serviceCollection.AddScoped(s => s.GetService<ISessionFactory>().OpenSession());
        }

        private NHConfiguration CreateNHibernateConfiguration()
        {
            var config = new NHConfiguration();
            config.SetProperty(Environment.Dialect, NHibernateOptions.Dialect);
            config.SetProperty(Environment.ConnectionDriver, NHibernateOptions.ConnectionDriver);
            config.SetProperty(Environment.ConnectionString, NHibernateOptions.ConnectionString);
            config.SetProperty(Environment.DefaultBatchFetchSize, "20");
            config.SetProperty(Environment.BatchSize, "20");
            config.SetProperty(Environment.Hbm2ddlKeyWords, "auto-quote");
            ConfigureNHibernate(config);

            var modelMapper = new ModelMapper();
            ConfigureMappings(modelMapper);
            config.AddMapping(modelMapper.CompileMappingForAllExplicitlyAddedEntities());

            return config;
        }

        protected override void SetUp()
        {
            var schemaExport = new SchemaExport(ServiceProvider.GetService<NHConfiguration>());
            schemaExport.Create(true, true);

            using var scope = ServiceProvider.CreateScope();
            using var session = scope.ServiceProvider.GetService<ISession>();
            using var tx = session.BeginTransaction();
            FillDatabase(session);

            tx.Commit();
        }

        protected override void Cleanup()
        {
            var schemaExport = new SchemaExport(ServiceProvider.GetService<NHConfiguration>());
            schemaExport.Drop(true, true);
        }

        protected virtual void ConfigureNHibernate(NHConfiguration configuration)
        {
        }

        protected virtual void ConfigureMappings(ModelMapper modelMapper)
        {
            var types = typeof(Order).Assembly.GetExportedTypes()
                .Where(t => typeof(IConformistHoldersProvider).IsAssignableFrom(t) && !t.IsAbstract)
                .ToList();
            modelMapper.AddMappings(types);
        }

        protected virtual void FillDatabase(ISession session)
        {
            var products = new Product[10];
            for (var i = 0; i < 10; i++)
            {
                var product = new Product { Name = $"Product{i}", Category = i % 2 == 0 ? "paper" : null };
                products[i] = product;
                session.Save(product);
            }

            var attachments = new Attachment[10];
            for (var i = 0; i < 10; i++)
            {
                var attachment = new Attachment {Name = $"Attachment{i}", Content = new byte[10]};
                attachments[i] = attachment;
                session.Save(attachment);
            }

            for (var i = 0; i < 10; i++)
            {
                var person = new Person { Name = $"Name{i}", Surname = $"Surname{i}"};
                var passport = new Passport
                {
                    Country = $"Country{i}",
                    ExpirationDate = DateTime.UtcNow.AddYears(10),
                    Number = 123456 + i,
                    Owner = person
                };
                person.Passport = passport;
                var card = new IdentityCard { Code = $"Code{i}", Owner = person };
                person.IdentityCard = card;

                session.Save(person);
                session.Save(card);
            }

            for (var i = 0; i < 10; i++)
            {
                var order = new Order
                {
                    Name = $"Order{i}",
                    Active = true,
                    Status = OrderStatus.Delivered
                };
                for (var j = 0; j < 10; j++)
                {
                    order.Products.Add(new OrderProduct
                    {
                        Order = order,
                        Product = products[(i + j) % 10],
                        TotalPrice = 10,
                        Quantity = 1
                    });
                    order.FkProducts.Add(new OrderProductFk
                    {
                        Order = order,
                        Product = products[(i + j) % 10],
                        TotalPrice = 10,
                        Quantity = 1
                    });
                }

                session.Save(order);

                var compositeOrder = new CompositeOrder(2000, i, $"Status{i}")
                {
                    TotalPrice = 15.8m
                };
                for (var j = 0; j < 10; j++)
                {
                    compositeOrder.CompositeOrderRows.Add(new CompositeOrderRow
                    {
                        CompositeOrder = compositeOrder,
                        Product = products[(i + j) % 10],
                        Price = i * j,
                        Quantity = i + j
                    });
                    var compositeOrderProduct = new CompositeOrderProduct(compositeOrder, products[(i + j) % 10])
                    {
                        Price = i * j, Quantity = i + j
                    };
                    compositeOrder.CompositeOrderProducts.Add(compositeOrderProduct);
                    compositeOrderProduct.Remarks.Add(new CompositeOrderProductRemark()
                    {
                        CompositeOrderProduct = compositeOrderProduct, Remark = "test"
                    });
                }

                session.Save(compositeOrder);

                var parentDog = new Dog
                {
                    Name = $"Dog{i}",
                    BirthDate = DateTime.UtcNow.AddYears(-10),
                    BodyWeight = 14.8,
                    Pregnant = true,
                };
                session.Save(parentDog);
                var parentCat = new Cat
                {
                    Name = $"Cat{i}",
                    BirthDate = DateTime.UtcNow.AddYears(-10),
                    BodyWeight = 19.8,
                    Pregnant = false,
                };
                session.Save(parentCat);

                for (var j = 0; j < 10; j++)
                {
                    var hasParent = j % 2 == 0;
                    var dog = new Dog
                    {
                        Name = $"Dog{j}",
                        BirthDate = DateTime.UtcNow.AddYears(-(i + j)),
                        BodyWeight = 14.8,
                        Parent = hasParent ? parentDog : null
                    };
                    session.Save(dog);
                    if (hasParent)
                    {
                        parentDog.Children.Add(dog);
                    }

                    var cat = new Cat
                    {
                        Name = $"Cat{j}",
                        BirthDate = DateTime.UtcNow.AddYears(-(i + j)),
                        BodyWeight = 14.8,
                        Parent = hasParent ? parentCat : null
                    };
                    session.Save(cat);
                    if (hasParent)
                    {
                        parentCat.Children.Add(cat);
                    }
                }
            }
        }
    }
}
