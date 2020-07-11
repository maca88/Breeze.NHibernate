using System;
using Breeze.NHibernate.Configuration;
using Breeze.NHibernate.Serialization;
using Breeze.NHibernate.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NHibernate;

namespace Breeze.NHibernate.AspNetCore.Mvc {

  public static class ServiceCollectionExtensions {

    public static void AddBreezeNHibernate(this IServiceCollection serviceCollection, Action<BreezeNHibernateOptions> action = null) {
      serviceCollection.AddSingleton<IConfigureOptions<MvcNewtonsoftJsonOptions>, MvcNewtonsoftJsonOptionsConfigurator>();
      serviceCollection.AddSingleton<IConfigureOptions<MvcOptions>, MvcOptionsConfigurator>();

      serviceCollection.TryAddSingleton<ISyntheticPropertyNameConvention, DefaultSyntheticPropertyNameConvention>();
      serviceCollection.TryAddSingleton<INHibernateClassMetadataProvider, DefaultNHibernateClassMetadataProvider>();
      serviceCollection.TryAddSingleton<IPropertyValidatorsProvider, DefaultPropertyValidatorsProvider>();
      serviceCollection.TryAddSingleton<IJsonSerializerSettingsProvider, DefaultJsonSerializerSettingsProvider>();
      serviceCollection.TryAddSingleton<IEntityQueryExecutor, DefaultEntityQueryExecutor>();
      serviceCollection.TryAddSingleton<ILazyLoadGuardProvider, DefaultLazyLoadGuardProvider>();
      serviceCollection.TryAddSingleton<IEntityMetadataProvider, EntityMetadataProvider>();
      serviceCollection.TryAddSingleton<IClientModelMetadataProvider, ClientModelMetadataProvider>();
      serviceCollection.TryAddSingleton<IProxyInitializer, ProxyInitializer>();
      serviceCollection.TryAddSingleton<IEntityBatchFetcherFactory, EntityBatchFetcherFactory>();
      serviceCollection.TryAddSingleton<IModelSaveValidatorProvider, DefaultModelSaveValidatorProvider>();
      serviceCollection.TryAddSingleton<ITypeMembersProvider, DefaultTypeMembersProvider>();
      serviceCollection.TryAddSingleton<ISaveWorkStateFactory, SaveWorkStateFactory>();
      serviceCollection.TryAddSingleton<BreezeContractResolver>();
      serviceCollection.TryAddSingleton<BreezeEntityValidator>();
      serviceCollection.TryAddSingleton<IBreezeConfigurator>(provider => {
        var configurator = new BreezeConfigurator(provider.GetRequiredService<ITypeMembersProvider>());
        var options = provider.GetRequiredService<IOptions<BreezeNHibernateOptions>>();
        options.Value.BreezeConfigurator?.Invoke(configurator);

        return configurator;
      });
      serviceCollection.TryAddSingleton(provider => {
        var builder = provider.GetRequiredService<BreezeMetadataBuilder>();
        var options = provider.GetRequiredService<IOptions<BreezeNHibernateOptions>>();
        options.Value.MetadataConfigurator?.Invoke(builder);

        return builder.Build();
      });
      serviceCollection.TryAddSingleton<BreezeQueryFilter>();

      serviceCollection.TryAddScoped<EntityUpdater>();
      serviceCollection.TryAddScoped<PersistenceManager>();
      serviceCollection.TryAddScoped<ISessionProvider>(provider => new DefaultSessionProvider(type => provider.GetRequiredService<ISession>()));

      serviceCollection.TryAddTransient<BreezeMetadataBuilder>();

      if (action != null) {
        serviceCollection.Configure(action);
      }
    }
  }
}
