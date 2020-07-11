using System;

namespace Breeze.NHibernate.Serialization
{
    /// <summary>
    /// Provides a way to guard against lazy loads when retrieving the member value.
    /// </summary>
    public interface ILazyLoadGuardProvider
    {
        /// <summary>
        /// Adds a guard around the given member getter to guard against lazy loads.
        /// </summary>
        /// <param name="getValueFunction">The member getter.</param>
        /// <param name="memberReflectedType">The member reflected type.</param>
        /// <param name="memberName">The member name.</param>
        /// <returns>A wrapped member getter that guards against lazy loads.</returns>
        Func<object, object> AddGuard(Func<object, object> getValueFunction, Type memberReflectedType, string memberName);
    }
}
