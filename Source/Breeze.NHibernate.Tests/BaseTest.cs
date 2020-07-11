using Breeze.NHibernate.Configuration;
using Breeze.NHibernate.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NHibernate;
using Xunit;

namespace Breeze.NHibernate.Tests
{
    public abstract class BaseTest : IClassFixture<Bootstrapper>
    {
        private readonly Bootstrapper _bootstrapper;

        protected BaseTest(Bootstrapper bootstrapper)
        {
            _bootstrapper = bootstrapper;
            if (_bootstrapper.IsInitialized)
            {
                return;
            }

            _bootstrapper.Configure += Configure;
            _bootstrapper.Cleanup += Cleanup;
            _bootstrapper.Initialized += Initialize;
            _bootstrapper.Initialize();
        }

        protected ServiceProvider ServiceProvider => _bootstrapper.ServiceProvider;

        protected ServiceProvider CreateServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IEntityMetadataProvider, EntityMetadataProvider>();
            serviceCollection.AddSingleton<IClientModelMetadataProvider, ClientModelMetadataProvider>();
            serviceCollection.AddSingleton<IBreezeConfigurator, BreezeConfigurator>();
            serviceCollection.AddTransient<BreezeMetadataBuilder>();
            serviceCollection.AddSingleton<BreezeContractResolver>();
            serviceCollection.AddSingleton<ISyntheticPropertyNameConvention, DefaultSyntheticPropertyNameConvention>();
            serviceCollection.AddSingleton<INHibernateClassMetadataProvider, DefaultNHibernateClassMetadataProvider>();
            serviceCollection.AddSingleton<IPropertyValidatorsProvider, DefaultPropertyValidatorsProvider>();
            serviceCollection.AddSingleton<IJsonSerializerSettingsProvider, DefaultJsonSerializerSettingsProvider>();
            serviceCollection.AddSingleton<IProxyInitializer, ProxyInitializer>();
            serviceCollection.AddSingleton<IEntityQueryExecutor, DefaultEntityQueryExecutor>();
            serviceCollection.AddSingleton<IEntityBatchFetcherFactory, EntityBatchFetcherFactory>();
            serviceCollection.AddSingleton<IModelSaveValidatorProvider, DefaultModelSaveValidatorProvider>();
            serviceCollection.AddSingleton<ITypeMembersProvider, DefaultTypeMembersProvider>();
            serviceCollection.AddSingleton<ISaveWorkStateFactory, SaveWorkStateFactory>();

            serviceCollection.AddSingleton<ILazyLoadGuardProvider, DefaultLazyLoadGuardProvider>();
            serviceCollection.AddScoped<EntityUpdater>();
            serviceCollection.AddScoped<PersistenceManager>();
            serviceCollection.AddScoped<ISessionProvider>(s => new DefaultSessionProvider(t => s.GetService<ISession>()));

            serviceCollection.AddTransient<BreezeEntityManager>();
            serviceCollection.AddScoped<TestPersistenceManager>();

            RegisterTypes(serviceCollection);

            return serviceCollection.BuildServiceProvider();
        }

        protected virtual void RegisterTypes(ServiceCollection serviceCollection)
        {
            
        }

        protected virtual void SetUp()
        {
        }

        protected virtual void Cleanup()
        {
        }

        private void Configure(ServiceCollection container)
        {
            RegisterTypes(container);
        }

        private void Initialize(ServiceProvider container)
        {
            SetUp();
        }
    }
}
