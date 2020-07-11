using System;
using Breeze.NHibernate.Configuration;
using Newtonsoft.Json.Serialization;

namespace Breeze.NHibernate.Serialization
{
    internal class SyntheticMemberValueProvider : IValueProvider
    {
        private readonly SerializeSyntheticMemberDelegate _serializeMemberDelegate;
        private readonly string _memberName;

        public SyntheticMemberValueProvider(string memberName, SerializeSyntheticMemberDelegate serializeMemberDelegate)
        {
            _memberName = memberName ?? throw new ArgumentNullException(nameof(memberName));
            _serializeMemberDelegate = serializeMemberDelegate ?? throw new ArgumentNullException(nameof(serializeMemberDelegate));
        }

        public void SetValue(object target, object value)
        {
            throw new NotSupportedException();
        }

        public object GetValue(object target)
        {
            return _serializeMemberDelegate?.Invoke(target, _memberName);
        }
    }
}
