using System;
using System.Reflection;
using Breeze.NHibernate.Serialization;

namespace Breeze.NHibernate.Configuration
{
    /// <summary>
    /// Provides a way to configure breeze metadata and serialization when using <see cref="BreezeContractResolver"/>.
    /// </summary>
    public interface IBreezeConfigurator
    {
        /// <summary>
        /// Gets the model configuration.
        /// </summary>
        /// <param name="modelType">The type of the model.</param>
        /// <returns>The configuration for the given type.</returns>
        ModelConfiguration GetModelConfiguration(Type modelType);

        /// <summary>
        /// Gets the model configuration.
        /// </summary>
        /// <returns>The configuration for the given type.</returns>
        ModelConfiguration GetModelConfiguration<TModel>() where TModel : class;

        /// <summary>
        /// Gets the model configurator.
        /// </summary>
        /// <returns>The configurator to configure the given type.</returns>
        IModelConfigurator<TModel> ConfigureModel<TModel>();

        /// <summary>
        /// Gets the model configurator.
        /// </summary>
        /// <returns>The configurator to configure the given type.</returns>
        IModelConfigurator ConfigureModel(Type type);

        /// <summary>
        /// Configure multiple models by using a predicate.
        /// </summary>
        /// <param name="predicate">The predicate to filter which types to configure.</param>
        /// <param name="configurationAction">The configuration action.</param>
        void ConfigureModels(Predicate<Type> predicate, Action<Type, IModelConfigurator> configurationAction);

        /// <summary>
        ///  Configure multiple models members by using a predicate.
        /// </summary>
        /// <param name="modelPredicate">The predicate to filter which types to configure.</param>
        /// <param name="memberConfigurationAction">The member configuration action.</param>
        void ConfigureModelMembers(Predicate<Type> modelPredicate, Action<MemberInfo, IMemberConfigurator> memberConfigurationAction);
    }
}
