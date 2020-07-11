using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Breeze.NHibernate.AspNetCore.Mvc.Tests;
using Breeze.NHibernate.Tests.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Breeze.NHibernate.Tests
{
    public class AspNetCoreMultipleDatabaseTests : BaseAspNetCoreTest<MultipleDatabaseStartup>
    {
        public AspNetCoreMultipleDatabaseTests(Bootstrapper bootstrapper, WebApplicationFactory<MultipleDatabaseStartup> factory) : base(bootstrapper, factory)
        {
        }

        protected override string ControllerName => "MultipleDatabase";

        [Fact]
        public async Task TestMetadata()
        {
            var serviceProvider = CreateServiceProvider();
            var json = await GetAsync<JObject>("Metadata");
            var metadata = serviceProvider.GetService<BreezeMetadataBuilder>()
                .WithIncludeFilter(o => o.Name.StartsWith("Composite") || o.Name.StartsWith("Order") || o.Name == "Product")
                .Build();

            Assert.Equal(metadata.StructuralTypes.Count, json.Property("structuralTypes").Value.Value<JArray>().Count);
        }

        [Fact]
        public async Task TestOrders()
        {
            var jObject = await GetAsync<JObject>("Orders");
            var jArray = jObject.Property("Results").Value.Value<JArray>();

            Assert.Equal(10, jArray.Count);
        }

        [Fact]
        public async Task TestCompositeOrders()
        {
            var jObject = await GetAsync<JObject>("CompositeOrders");
            var jArray = jObject.Property("Results").Value.Value<JArray>();

            Assert.Equal(10, jArray.Count);
        }

        [Theory]
        [InlineData("SaveChanges")]
        [InlineData("AsyncSaveChanges")]
        public async Task TestSaveChanges(string actionName)
        {
            var serviceProvider = CreateServiceProvider();
            var em = serviceProvider.GetService<BreezeEntityManager>();
            em.SetSessionProvider(serviceProvider.GetService<ISessionProvider>());
            var order = em.CreateEntity<Order>();
            order.Name = "test";
            order.Active = true;
            order.Status = OrderStatus.Delivered;
            order.TotalPrice = 10.3m;
            order.Address.City = "City";
            order.Address.Street = "Street";

            var compositeOrder = new CompositeOrder(1999, 1, "A") {TotalPrice = 10.8m};
            em.SetAdded(compositeOrder);

            var jObject = await PostAsync<JObject>(actionName, em.GetSaveBundle());
            var entities = jObject.Property("Entities").Value.Value<JArray>();
            Assert.Equal(2, entities.Count);

            var keyMappings = jObject.Property("KeyMappings").Value.Value<JArray>().ToObject<List<KeyMapping>>();
            var keyMapping = Assert.Single(keyMappings);
            Assert.NotNull(keyMapping);
            order = em.Get<Order>(keyMapping.RealValue);
            Assert.NotNull(order);

            em.Clear();
            em.SetDeleted(order);
            em.SetDeleted(compositeOrder);

            jObject = await PostAsync<JObject>(actionName, em.GetSaveBundle());
            var deletedKeys = jObject.Property("DeletedKeys").Value.Value<JArray>().ToObject<List<EntityKey>>();
            Assert.Equal(2, deletedKeys.Count);
        }
    }
}
