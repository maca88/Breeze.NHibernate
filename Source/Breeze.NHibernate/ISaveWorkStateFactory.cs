using System;

namespace Breeze.NHibernate
{
    /// <summary>
    /// A factory for creating <see cref="SaveWorkState"/>.
    /// </summary>
    public interface ISaveWorkStateFactory
    {
        /// <summary>
        /// Creates a <see cref="SaveWorkState"/> for the given <see cref="SaveBundle"/>.
        /// </summary>
        /// <param name="saveBundle">The save bundle.</param>
        /// <param name="includePredicate">The predicate that will determine whether the <see cref="EntityInfo"/> should be added to the save map.</param>
        /// <returns>The save work state.</returns>
        SaveWorkState Create(SaveBundle saveBundle, Predicate<EntityInfo> includePredicate);
    }
}
