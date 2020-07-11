using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Breeze.NHibernate.Configuration
{
    /// <summary>
    /// The default <see cref="IBreezeConfigurator"/> implementation.
    /// </summary>
    public class BreezeConfigurator : IBreezeConfigurator
    {
        private readonly ConcurrentDictionary<Type, ModelConfiguration> _modelsConfiguration = new ConcurrentDictionary<Type, ModelConfiguration>();
        private readonly ConcurrentDictionary<Type, ModelConfiguration> _mergedConfiguration = new ConcurrentDictionary<Type, ModelConfiguration>();
        private readonly ConcurrentBag<ModelPredicateConfigurator> _modelPredicateConfigurators = new ConcurrentBag<ModelPredicateConfigurator>();
        private readonly ConcurrentBag<MemberPredicateConfigurator> _memberPredicateConfigurators = new ConcurrentBag<MemberPredicateConfigurator>();
        private readonly ITypeMembersProvider _typeMembersProvider;
        private bool _isLocked;

        public BreezeConfigurator(ITypeMembersProvider typeMembersProvider)
        {
            _typeMembersProvider = typeMembersProvider;
        }

        private class ModelPredicateConfigurator
        {
            public ModelPredicateConfigurator(Predicate<Type> predicate, Action<Type, IModelConfigurator> configurationAction)
            {
                Predicate = predicate;
                ConfigurationAction = configurationAction;
            }

            public Predicate<Type> Predicate { get; }

            public Action<Type, IModelConfigurator> ConfigurationAction { get; }
        }

        private class MemberPredicateConfigurator
        {
            public MemberPredicateConfigurator(Predicate<Type> predicate, Action<MemberInfo, IMemberConfigurator> configurationAction)
            {
                Predicate = predicate;
                ConfigurationAction = configurationAction;
            }

            public Predicate<Type> Predicate { get; }

            public Action<MemberInfo, IMemberConfigurator> ConfigurationAction { get; }
        }

        /// <inheritdoc />
        public ModelConfiguration GetModelConfiguration<TModel>() where TModel : class
        {
            return GetModelConfiguration(typeof(TModel));
        }

        /// <inheritdoc />
        public ModelConfiguration GetModelConfiguration(Type modelType)
        {
            if (!_isLocked)
            {
                _isLocked = true;
            }

            return _mergedConfiguration.GetOrAdd(modelType, CreateMergedModelConfiguration);
        }

        /// <inheritdoc />
        public IModelConfigurator<TModel> ConfigureModel<TModel>()
        {
            ThrowIfLocked();

            return new ModelConfigurator<TModel>(
                _modelsConfiguration.GetOrAdd(typeof(TModel), CreateModelConfiguration)
            );
        }

        /// <inheritdoc />
        public IModelConfigurator ConfigureModel(Type type)
        {
            ThrowIfLocked();

            return new ModelConfigurator(
                _modelsConfiguration.GetOrAdd(type, CreateModelConfiguration)
            );
        }

        /// <inheritdoc />
        public void ConfigureModels(Predicate<Type> predicate, Action<Type, IModelConfigurator> configurationAction)
        {
            ThrowIfLocked();

            _modelPredicateConfigurators.Add(new ModelPredicateConfigurator(predicate, configurationAction));
        }

        /// <inheritdoc />
        public void ConfigureModelMembers(Predicate<Type> modelPredicate, Action<MemberInfo, IMemberConfigurator> memberConfigurationAction)
        {
            ThrowIfLocked();

            _memberPredicateConfigurators.Add(new MemberPredicateConfigurator(modelPredicate, memberConfigurationAction));
        }

        private void ConfigureMemberConfiguration(Type modelType, MemberConfiguration configuration)
        {
            // Run predicate configurators
            var configurator = new MemberConfigurator(configuration);
            foreach (var memberPredicateConfigurator in _memberPredicateConfigurators)
            {
                if (memberPredicateConfigurator.Predicate(modelType))
                {
                    memberPredicateConfigurator.ConfigurationAction(configuration.MemberInfo, configurator);
                }
            }
        }

        private ModelConfiguration CreateModelConfiguration(Type modelType)
        {
            var members = _typeMembersProvider.GetMembers(modelType).Select(o => new MemberConfiguration(o)).ToDictionary(o => o.MemberName);
            return new ModelConfiguration(modelType, members);
        }

        private ModelConfiguration CreateMergedModelConfiguration(Type modelType)
        {
            var configuration = CreateModelConfiguration(modelType);
            // Run model predicate configurators
            foreach (var modelPredicateConfigurator in _modelPredicateConfigurators)
            {
                if (modelPredicateConfigurator.Predicate(modelType))
                {
                    modelPredicateConfigurator.ConfigurationAction(modelType, new ModelConfigurator(configuration));
                }
            }

            // Run member predicate configurators
            foreach (var memberConfiguration in configuration.Members.Values)
            {
                ConfigureMemberConfiguration(modelType, memberConfiguration);
            }

            // For interfaces we want to match only interfaces that are assignable from modelType
            var baseConfigurations = modelType.IsInterface
                ? _modelsConfiguration.Where(o => o.Key.IsInterface && o.Key.IsAssignableFrom(modelType)).ToList()
                : _modelsConfiguration.Where(o => o.Key.IsAssignableFrom(modelType)).ToList();
            // Subclasses have higher priority
            baseConfigurations.Sort((pair, valuePair) => pair.Key.IsAssignableFrom(valuePair.Key) ? -1 : 1);
            foreach (var baseConfiguration in baseConfigurations.Select(o => o.Value))
            {
                configuration.MergeWith(baseConfiguration);
                foreach (var pair in baseConfiguration.Members)
                {
                    configuration.GetMember(pair.Key).MergeWith(pair.Value);
                }

                foreach (var pair in baseConfiguration.SyntheticMembers)
                {
                    var member = pair.Value;
                    configuration.GetOrAdd(pair.Key, t => new SyntheticMemberConfiguration(
                            member.MemberName,
                            member.MemberType,
                            member.DeclaringType,
                            member.SerializeFunction,
                            member.Added))
                        .MergeWith(pair.Value);
                }
            }

            return configuration;
        }

        private void ThrowIfLocked()
        {
            if (_isLocked)
            {
                throw new InvalidOperationException("Cannot configure model as breeze configurator is locked. " +
                                                    "Assure that all configurations for models are done at startup of the application");
            }
        }
    }
}
