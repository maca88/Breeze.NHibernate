using System;
using Newtonsoft.Json.Serialization;
using NHibernate.Proxy;

namespace Breeze.NHibernate.Serialization
{
    internal class SyntheticForeignKeyPropertyValueProvider : IValueProvider
    {
        private readonly Func<object, object> _getIdentifierFunction;
        private readonly Func<object, object> _getAssociationFunction;
        private readonly bool _compositeKey;

        public SyntheticForeignKeyPropertyValueProvider(
            Func<object, object> getAssociationFunction,
            Func<object, object> getIdentifierFunction,
            bool compositeKey)
        {
            _getAssociationFunction = getAssociationFunction ?? throw new ArgumentNullException(nameof(getAssociationFunction));
            _getIdentifierFunction = getIdentifierFunction ?? throw new ArgumentNullException(nameof(getIdentifierFunction));
            _compositeKey = compositeKey;
        }

        public void SetValue(object target, object value)
        {
            throw new NotSupportedException();
        }

        public object GetValue(object target)
        {
            if (target == null)
            {
                return null;
            }

            target = _getAssociationFunction(target);
            // When the association is a proxy, we have to retrieve the id via LazyInitializer
            // in order to avoid initializing the proxy when GetHashCode and Equals methods are overriden.
            if (target is INHibernateProxy nhProxy)
            {
                target = nhProxy.HibernateLazyInitializer.Identifier;
                return _compositeKey
                    ? _getIdentifierFunction(target)
                    : target;
            }

            return target == null
                ? null
                : _getIdentifierFunction(target);
        }
    }
}
