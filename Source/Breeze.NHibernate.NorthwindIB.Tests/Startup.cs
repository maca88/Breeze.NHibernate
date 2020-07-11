using System;
using System.IO;
using System.Text;
using Breeze.NHibernate.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Models.NorthwindIB.NH;
using NHibernate;
using Environment = NHibernate.Cfg.Environment;
using ISession = NHibernate.ISession;
using NHConfiguration = NHibernate.Cfg.Configuration;

namespace Breeze.NHibernate.NorthwindIB.Tests
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
            services.AddScoped<NorthwindPersistenceManager>();
            services.AddBreezeNHibernate(options => options
                .WithGlobalExceptionFilter()
                .WithBreezeConfigurator(c => c.ConfigureModel<Customer>()
                    .ForMember(o => o.ExtraDouble, o => o.Serialize(true))
                    .ForMember(o => o.ExtraString, o => o.Serialize(true))
                    .ForMember(o => o.RowVersion, o => o.IsNullable(true)) // Override this so that test "query - basic: size test property change" passes
                ));

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

            // Setup static files for breeze
            var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var breezeJsDirectory = Path.GetFullPath(Path.Combine(currentDirectory, @"..\..\..\..\Breeze.js\"));
            app.Use((context, middleware) =>
            {
                if (context.Request.Path.HasValue && context.Request.Path.Value == "/breeze/breeze.debug.js")
                {
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "application/javascript";
                    var path = Path.Combine(breezeJsDirectory, @"build\breeze.debug.js");
                    return context.Response.WriteAsync(File.ReadAllText(path), Encoding.UTF8);
                }

                return middleware();
            });
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(breezeJsDirectory, @"test")),
                RequestPath = new PathString(""),
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private NHConfiguration CreateNHibernateConfiguration()
        {
            // Configure NHibernate
            var configuration = new NHConfiguration();
            configuration.SetProperty(Environment.Dialect, "NHibernate.Dialect.MsSql2008Dialect");
            configuration.SetProperty(Environment.ConnectionDriver, "NHibernate.Driver.Sql2008ClientDriver");
            configuration.SetProperty(Environment.ConnectionString, "Data Source=.;Initial Catalog=NorthwindIB;Integrated Security=True;MultipleActiveResultSets=True");
            configuration.SetProperty(Environment.DefaultBatchFetchSize, "20");
            configuration.SetProperty(Environment.BatchSize, "20");
            configuration.SetProperty(Environment.Hbm2ddlKeyWords, "auto-quote");

            // Configure NHibernate mappings from Breeze.NHibernate.NorthwindIB.Tests.Models
            configuration.AddAssembly(typeof(Order).Assembly);

            return configuration;
        }
    }
}
