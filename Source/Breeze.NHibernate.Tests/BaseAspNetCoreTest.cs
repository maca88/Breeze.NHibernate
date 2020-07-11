using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Xunit;

namespace Breeze.NHibernate.Tests
{
    public abstract class BaseAspNetCoreTest<TStartup> : BaseDatabaseTest, IClassFixture<WebApplicationFactory<TStartup>> where TStartup : class
    {
        protected readonly WebApplicationFactory<TStartup> Factory;

        protected BaseAspNetCoreTest(Bootstrapper bootstrapper, WebApplicationFactory<TStartup> factory) : base(bootstrapper)
        {
            Factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseStartup<TStartup>();
            });
        }

        protected abstract string ControllerName { get; }

        protected async Task<T> GetAsync<T>(string actionName)
        {
            var response = await GetAsync(actionName);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (json is T str)
            {
                return str;
            }

            return JsonConvert.DeserializeObject<T>(json);
        }

        protected Task<HttpResponseMessage> GetAsync(string actionName)
        {
            var client = Factory.CreateClient();
            return client.GetAsync($"/{ControllerName}/{actionName}");
        }

        protected async Task<T> PostAsync<T>(string actionName, object data)
        {
            var response = await PostAsync(actionName, data);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            if (json is T str)
            {
                return str;
            }

            return JsonConvert.DeserializeObject<T>(json);
        }

        protected Task<HttpResponseMessage> PostAsync(string actionName, object data)
        {
            var client = Factory.CreateClient();
            return client.PostAsync($"/{ControllerName}/{actionName}", new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
        }
    }
}
