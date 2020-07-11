using System;
using System.Linq;
using Breeze.NHibernate.AspNetCore.Mvc.Tests.MultipleDatabase;
using Breeze.NHibernate.Tests.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NHibernate;
using NHibernate.Mapping.ByCode;
using Environment = NHibernate.Cfg.Environment;
using ISession = NHibernate.ISession;
using NHConfiguration = NHibernate.Cfg.Configuration;

namespace Breeze.NHibernate.AspNetCore.Mvc.Tests
{
    public class MultipleDatabaseStartup
    {
        public MultipleDatabaseStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Register IHttpContextAccessor in order be able to get the current http context when creating NHibernate session
            services.AddHttpContextAccessor();

            // Configure foo database with NHibernate
            services.AddSingleton(s => new FooConfiguration(CreateNHibernateConfiguration(t => t.Name.StartsWith("Composite") || t.Name.StartsWith("Product"))));
            services.AddSingleton(s => new FooSessionFactory(s.GetService<FooConfiguration>().Configuration.BuildSessionFactory()));
            services.AddScoped(s => new FooSession(GetSession<FooSessionFactory>(s)));

            // Configure bar database with NHibernate
            services.AddSingleton(s => new BarConfiguration(CreateNHibernateConfiguration(t => t.Name.StartsWith("Order") || t.Name.StartsWith("Product"))));
            services.AddSingleton(s => new BarSessionFactory(s.GetService<BarConfiguration>().Configuration.BuildSessionFactory()));
            services.AddScoped(s => new BarSession(GetSession<BarSessionFactory>(s)));

            // Configure controllers to use Newtonsoft Json.NET serializer
            services.AddControllers().AddNewtonsoftJson();

            // Override Breeze.NHibernate default implementations in order to enable support for multiple databases
            services.AddScoped<ISessionProvider, MultipleDatabaseSessionProvider>();
            services.AddSingleton<INHibernateClassMetadataProvider, MultipleDatabaseClassMetadataProvider>();

            // Configure Breeze.NHibernate by using the default global exception filter for handling EntityErrorsException
            services.AddBreezeNHibernate(options => options.WithGlobalExceptionFilter());
        }

        private static ISession GetSession<TSessionFactory>(IServiceProvider serviceProvider) where TSessionFactory : AbstractSessionFactory
        {
            var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
            var session = serviceProvider.GetRequiredService<TSessionFactory>().SessionFactory.OpenSession();
            // Open a transaction only when a http context is available
            if (httpContextAccessor.HttpContext != null)
            {
                PerRequestTransactionsMiddleware.RegisterTransaction(httpContextAccessor.HttpContext, session.BeginTransaction());
            }

            return session;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            app.UseAuthorization();

            // Use a middleware that will commit/rollback transactions that were opened during the request
            app.UsePerRequestTransactions();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private NHConfiguration CreateNHibernateConfiguration(Func<Type, bool> typePredicate)
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
                .Where(typePredicate)
                .ToList();
            modelMapper.AddMappings(types);
            configuration.AddMapping(modelMapper.CompileMappingForAllExplicitlyAddedEntities());

            return configuration;
        }
    }

    public abstract class AbstractConfiguration
    {
        protected AbstractConfiguration(NHConfiguration configuration)
        {
            Configuration = configuration;
        }

        public NHConfiguration Configuration { get; }
    }

    public abstract class AbstractSessionFactory
    {
        protected AbstractSessionFactory(ISessionFactory sessionFactory)
        {
            SessionFactory = sessionFactory;
        }

        public ISessionFactory SessionFactory { get; }
    }

    public abstract class AbstractSession
    {
        protected AbstractSession(ISession session)
        {
            Session = session;
        }

        public ISession Session { get; }
    }

    public class FooConfiguration : AbstractConfiguration
    {
        public FooConfiguration(NHConfiguration configuration) : base(configuration)
        {
        }
    }

    public class FooSessionFactory : AbstractSessionFactory
    {
        public FooSessionFactory(ISessionFactory sessionFactory) : base(sessionFactory)
        {
        }
    }

    public class FooSession : AbstractSession
    {
        public FooSession(ISession session) : base(session)
        {
        }
    }

    public class BarConfiguration : AbstractConfiguration
    {
        public BarConfiguration(NHConfiguration configuration) : base(configuration)
        {
        }
    }

    public class BarSessionFactory : AbstractSessionFactory
    {
        public BarSessionFactory(ISessionFactory sessionFactory) : base(sessionFactory)
        {
        }
    }

    public class BarSession : AbstractSession
    {
        public BarSession(ISession session) : base(session)
        {
        }
    }
}
