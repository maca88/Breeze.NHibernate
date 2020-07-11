using System.Reflection;
using Breeze.NHibernate.Configuration;
using Newtonsoft.Json.Serialization;

namespace Breeze.NHibernate.Serialization
{
    internal class BreezeValueProvider : IValueProvider
    {
        private readonly IValueProvider _valueProvider;
        private readonly MemberInfo _memberInfo;
        private readonly SerializeMemberDelegate _serializeMemberDelegate;
        private readonly DeserializeMemberDelegate _deserializeMemberDelegate;

        public BreezeValueProvider(
            IValueProvider valueProvider,
            MemberInfo memberInfo,
            SerializeMemberDelegate serializeMemberDelegate,
            DeserializeMemberDelegate deserializeMemberDelegate)
        {
            _valueProvider = valueProvider;
            _memberInfo = memberInfo;
            _serializeMemberDelegate = serializeMemberDelegate;
            _deserializeMemberDelegate = deserializeMemberDelegate;
        }

        public void SetValue(object target, object value)
        {
            _valueProvider.SetValue(target, _deserializeMemberDelegate == null
                ? value
                : _deserializeMemberDelegate(value, _memberInfo));
        }

        public object GetValue(object target)
        {
            return _serializeMemberDelegate == null
                ? _valueProvider.GetValue(target)
                : _serializeMemberDelegate(target, _memberInfo);
        }
    }
}
