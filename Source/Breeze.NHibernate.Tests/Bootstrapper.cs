using System;
using Microsoft.Extensions.DependencyInjection;

namespace Breeze.NHibernate.Tests
{
    public class Bootstrapper : IDisposable
    {
        private readonly ServiceCollection _serviceCollection;

        public Bootstrapper()
        {
            _serviceCollection = new ServiceCollection();
        }

        public ServiceProvider ServiceProvider { get; private set; }

        public bool IsInitialized { get; private set; }

        public event Action<ServiceCollection> Configure;

        public event Action<ServiceProvider> Initialized;

        public event Action Cleanup;

        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            Configure?.Invoke(_serviceCollection);
            ServiceProvider = _serviceCollection.BuildServiceProvider();
            IsInitialized = true;
            Initialized?.Invoke(ServiceProvider);
        }
        
        public void Dispose()
        {
            Cleanup?.Invoke();
            ServiceProvider?.Dispose();
        }
    }
}
