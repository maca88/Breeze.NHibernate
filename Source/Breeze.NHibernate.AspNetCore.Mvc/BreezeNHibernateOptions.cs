using System;
using Breeze.NHibernate.Configuration;

namespace Breeze.NHibernate.AspNetCore.Mvc {
  public class BreezeNHibernateOptions {
    /// <summary>
    /// Whether to use the default <see cref="GlobalExceptionFilter"/> to handle global exceptions.
    /// Default is false.
    /// </summary>
    public bool UseGlobalExceptionFilter { get; set; }

    public Action<BreezeMetadataBuilder> MetadataConfigurator { get; set; }

    public Action<IBreezeConfigurator> BreezeConfigurator { get; set; }

    /// <summary>
    /// Set the default <see cref="GlobalExceptionFilter"/> to handle global exceptions.
    /// </summary>
    /// <returns></returns>
    public BreezeNHibernateOptions WithGlobalExceptionFilter() {
      UseGlobalExceptionFilter = true;
      return this;
    }

    public BreezeNHibernateOptions WithBreezeConfigurator(Action<IBreezeConfigurator> configurator) {
      BreezeConfigurator = configurator;
      return this;
    }

    public BreezeNHibernateOptions WithMetadataConfigurator(Action<BreezeMetadataBuilder> configurator) {
      MetadataConfigurator = configurator;
      return this;
    }
  }
}
