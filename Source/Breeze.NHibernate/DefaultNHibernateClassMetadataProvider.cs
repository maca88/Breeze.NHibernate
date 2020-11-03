using System.Collections.Generic;
using NHibernate;
using NHibernate.Metadata;

namespace Breeze.NHibernate
{
    /// <summary>
    /// The default implementation of <see cref="INHibernateClassMetadataProvider"/> which supports one <see cref="ISessionFactory"/> for application.
    /// </summary>
    public class DefaultNHibernateClassMetadataProvider : INHibernateClassMetadataProvider
    {
        private readonly ISessionFactory _sessionFactory;

        /// <summary>
        /// Constructs an instance of <see cref="DefaultNHibernateClassMetadataProvider"/>.
        /// </summary>
        public DefaultNHibernateClassMetadataProvider(ISessionFactory sessionFactory)
        {
            _sessionFactory = sessionFactory;
        }

        /// <inheritdoc />
        public IClassMetadata Get(string entityName)
        {
            return _sessionFactory.GetClassMetadata(entityName);
        }

        /// <inheritdoc />
        public IEnumerable<IClassMetadata> GetAll()
        {
            return _sessionFactory.GetAllClassMetadata().Values;
        }
    }
}
