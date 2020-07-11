using System.Linq;
using Breeze.NHibernate.Tests.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NHibernate;
using NHibernate.Mapping.ByCode;
using Environment = NHibernate.Cfg.Environment;
using NHConfiguration = NHibernate.Cfg.Configuration;

namespace Breeze.NHibernate.AspNetCore.Mvc.Tests
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Configure controllers to use Newtonsoft Json.NET serializer
            services.AddControllers().AddNewtonsoftJson();

            // Configure Breeze.NHibernate by using the default global exception filter for handling EntityErrorsException
            services.AddBreezeNHibernate(options => options.WithGlobalExceptionFilter());

            // Configure NHibernate
            services.AddSingleton(s => CreateNHibernateConfiguration());
            services.AddSingleton(s => s.GetService<NHConfiguration>().BuildSessionFactory());
            services.AddScoped(s => s.GetService<ISessionFactory>().OpenSession());
            services.AddScoped(s => s.GetService<ISession>().BeginTransaction());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            app.UseAuthorization();

            // Configure to have one transaction per http call
            app.UsePerRequestTransaction();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private NHConfiguration CreateNHibernateConfiguration()
        {
            var nhOptions = Configuration.GetSection("NHibernate").Get<NHibernateOptions>();

            // Configure NHibernate
            var configuration = new NHConfiguration();
            configuration.SetProperty(Environment.Dialect, nhOptions.Dialect);
            configuration.SetProperty(Environment.ConnectionDriver, nhOptions.ConnectionDriver);
            configuration.SetProperty(Environment.ConnectionString, nhOptions.ConnectionString);
            configuration.SetProperty(Environment.DefaultBatchFetchSize, "20");
            configuration.SetProperty(Environment.BatchSize, "20");
            configuration.SetProperty(Environment.Hbm2ddlKeyWords, "auto-quote");

            // Configure NHibernate mappings from Breeze.NHibernate.Tests.Models project
            var modelMapper = new ModelMapper();
            var types = typeof(Order).Assembly.GetExportedTypes()
                .Where(t => typeof(IConformistHoldersProvider).IsAssignableFrom(t) && !t.IsAbstract)
                .ToList();
            modelMapper.AddMappings(types);
            configuration.AddMapping(modelMapper.CompileMappingForAllExplicitlyAddedEntities());

            return configuration;
        }
    }
}
