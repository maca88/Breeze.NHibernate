using System.Reflection;
using Breeze.NHibernate.Internal;
using NHibernate.Persister.Entity;

namespace Breeze.NHibernate
{
    /// <summary>
    /// The default implementation of <see cref="IEntityBatchFetcherFactory"/>.
    /// </summary>
    public class EntityBatchFetcherFactory : IEntityBatchFetcherFactory
    {
        /// <inheritdoc />
        public IEntityBatchFetcher Create(AbstractEntityPersister persister)
        {
            ConstructorInfo constructor;
            if (persister.IdentifierPropertyName != null)
            {
                constructor = typeof(EntityBatchFetcher<,>)
                    .MakeGenericType(persister.MappedClass, persister.IdentifierType.ReturnedClass)
                    .GetConstructor(new[] { typeof(AbstractEntityPersister) });
            }
            else
            {
                constructor = typeof(CompositeEntityBatchFetcher<>)
                    .MakeGenericType(persister.MappedClass)
                    .GetConstructor(new[] { typeof(AbstractEntityPersister) });
            }

            return (IEntityBatchFetcher)constructor.Invoke(new object[] { persister });
        }
    }
}
