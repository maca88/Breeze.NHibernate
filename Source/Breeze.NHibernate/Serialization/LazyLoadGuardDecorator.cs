using System;
using Newtonsoft.Json.Serialization;

namespace Breeze.NHibernate.Serialization
{
    internal class LazyLoadGuardDecorator : IValueProvider
    {
        private readonly Func<object, object> _guardedGetValueFunction;
        private readonly Action<object, object> _setValueFunction;

        public LazyLoadGuardDecorator(Func<object, object> guardedGetValueFunction, Action<object, object> setValueFunction)
        {
            _guardedGetValueFunction = guardedGetValueFunction;
            _setValueFunction = setValueFunction;
        }

        public void SetValue(object target, object value)
        {
            _setValueFunction(target, value);
        }

        public object GetValue(object target)
        {
            return _guardedGetValueFunction(target);
        }
    }
}
