using System;
using System.Collections.Generic;
using System.Reflection;
using Breeze.NHibernate.Configuration;

namespace Breeze.NHibernate
{
    /// <summary>
    /// Provides all <see cref="MemberInfo"/> that can be serialized and configured via <see cref="IBreezeConfigurator"/>.
    /// </summary>
    public interface ITypeMembersProvider
    {
        /// <summary>
        /// Gets all <see cref="MemberInfo"/> that can be serialized and configured via <see cref="IBreezeConfigurator"/> for the given type.
        /// </summary>
        /// <param name="type">The type to get the member for.</param>
        /// <returns>All <see cref="MemberInfo"/> that can be serialized and configured via <see cref="IBreezeConfigurator"/>.</returns>
        IEnumerable<MemberInfo> GetMembers(Type type);
    }
}
