using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Breeze.NHibernate.AspNetCore.Mvc;
using Breeze.NHibernate.AspNetCore.Mvc.Tests;
using Breeze.NHibernate.Tests.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NHibernate.Transaction;
using Xunit;

namespace Breeze.NHibernate.Tests
{
    public class AspNetCoreBreezeTests : BaseAspNetCoreTest<Startup>
    {
        public AspNetCoreBreezeTests(Bootstrapper bootstrapper, WebApplicationFactory<Startup> factory) : base(bootstrapper, factory)
        {
        }

        protected override string ControllerName => "Breeze";

        [Fact]
        public async Task TestMetadata()
        {
            var serviceProvider = CreateServiceProvider();
            var json = await GetAsync<string>("Metadata");
            var metadata = serviceProvider.GetService<BreezeMetadataBuilder>().Build();

            Assert.Equal(metadata.ToJson(), json);
        }

        [Fact]
        public async Task TestOrders()
        {
            var jObject = await GetAsync<JObject>("Orders");
            var jArray = jObject.Property("Results").Value.Value<JArray>();

            Assert.Equal(10, jArray.Count);
        }

        [Fact]
        public async Task TestLazyLoad()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => GetAsync("LazyLoad"));
        }

        [Fact]
        public async Task TestGlobalFilter()
        {
            var response = await GetAsync("EntityErrorsException");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var json = await response.Content.ReadAsStringAsync();
            var errorDto = JsonConvert.DeserializeObject<ErrorDto>(json);
            var error = Assert.Single(errorDto.EntityErrors);
            Assert.NotNull(error);
            Assert.Equal("Test", error.PropertyName);
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

            var jObject = await PostAsync<JObject>(actionName, em.GetSaveBundle());
            var keyMappings = jObject.Property("KeyMappings").Value.Value<JArray>().ToObject<List<KeyMapping>>();
            var keyMapping = Assert.Single(keyMappings);
            Assert.NotNull(keyMapping);

            order = em.Get<Order>(keyMapping.RealValue);
            Assert.NotNull(order);

            em.Clear();
            em.SetDeleted(order);

            jObject = await PostAsync<JObject>(actionName, em.GetSaveBundle());
            var deletedKeys = jObject.Property("DeletedKeys").Value.Value<JArray>().ToObject<List<EntityKey>>();
            var deletedKey = Assert.Single(deletedKeys);
            Assert.NotNull(deletedKey);
            Assert.Equal(keyMapping.RealValue, ((JArray)deletedKey.KeyValue)[0].Value<long>());
        }
    }
}
