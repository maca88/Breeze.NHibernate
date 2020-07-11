using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace Breeze.NHibernate
{
    /// <summary>
    /// The default implementation of <see cref="ITypeMembersProvider"/> that uses <see cref="DefaultContractResolver.GetSerializableMembers"/> to
    /// get members for the given type.
    /// </summary>
    public class DefaultTypeMembersProvider : ITypeMembersProvider
    {
        private readonly TypeMembersProvider _typeMembersProvider = new TypeMembersProvider();

        private class TypeMembersProvider : DefaultContractResolver
        {
            public List<MemberInfo> GetMembers(Type type)
            {
                return GetSerializableMembers(type);
            }
        }

        /// <inheritdoc />
        public IEnumerable<MemberInfo> GetMembers(Type type)
        {
            return _typeMembersProvider.GetMembers(type);
        }
    }
}
