using System;
using Newtonsoft.Json.Serialization;
using NHibernate.Proxy;

namespace Breeze.NHibernate.Serialization
{
    internal class NHibernateProxyValueProvider : IValueProvider
    {
        private readonly Func<object, object> _getValueFunction;
        private readonly Action<object, object> _setValueFunction;

        public NHibernateProxyValueProvider(IValueProvider valueProvider)
        {
            _getValueFunction = valueProvider.GetValue;
            _setValueFunction = valueProvider.SetValue;
        }

        public void SetValue(object target, object value)
        {
            _setValueFunction(target, value);
        }

        public object GetValue(object target)
        {
            var value = _getValueFunction(target);
            if (value is INHibernateProxy nhProxy)
            {
                return nhProxy.HibernateLazyInitializer.GetImplementation();
            }

            return value;
        }
    }
}
