using System;
using Microsoft.Extensions.DependencyInjection;
using NHibernate;

namespace Breeze.NHibernate.AspNetCore.Mvc.Tests.MultipleDatabase
{
    public class MultipleDatabaseSessionProvider : ISessionProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ISessionFactory _fooSessionFactory;
        private readonly ISessionFactory _barSessionFactory;

        public MultipleDatabaseSessionProvider(
            IServiceProvider serviceProvider,
            FooSessionFactory fooSessionFactory,
            BarSessionFactory barSessionFactory)
        {
            _serviceProvider = serviceProvider;
            _fooSessionFactory = fooSessionFactory.SessionFactory;
            _barSessionFactory = barSessionFactory.SessionFactory;
        }

        public ISession Get(Type modelType)
        {
            if (_fooSessionFactory.GetClassMetadata(modelType) != null)
            {
                return _serviceProvider.GetService<FooSession>().Session;
            }

            if (_barSessionFactory.GetClassMetadata(modelType) != null)
            {
                return _serviceProvider.GetService<BarSession>().Session;
            }

            return null;
        }
    }
}
