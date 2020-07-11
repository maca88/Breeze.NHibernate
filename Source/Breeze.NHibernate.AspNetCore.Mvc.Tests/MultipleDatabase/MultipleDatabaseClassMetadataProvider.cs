using System.Collections.Generic;
using System.Linq;
using NHibernate;
using NHibernate.Metadata;

namespace Breeze.NHibernate.AspNetCore.Mvc.Tests.MultipleDatabase
{
    public class MultipleDatabaseClassMetadataProvider : INHibernateClassMetadataProvider
    {
        private readonly ISessionFactory _fooSessionFactory;
        private readonly ISessionFactory _barSessionFactory;

        public MultipleDatabaseClassMetadataProvider(FooSessionFactory fooSessionFactory, BarSessionFactory barSessionFactory)
        {
            _fooSessionFactory = fooSessionFactory.SessionFactory;
            _barSessionFactory = barSessionFactory.SessionFactory;
        }

        public IClassMetadata Get(string entityName)
        {
            return _fooSessionFactory.GetClassMetadata(entityName) ??
                   _barSessionFactory.GetClassMetadata(entityName);
        }

        public IEnumerable<IClassMetadata> GetAll()
        {
            return _fooSessionFactory.GetAllClassMetadata().Values
                .Concat(_barSessionFactory.GetAllClassMetadata().Values);
        }
    }
}
