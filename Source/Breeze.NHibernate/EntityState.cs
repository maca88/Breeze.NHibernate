using System;

namespace Breeze.NHibernate
{
    /// <summary>
    /// An enum of entity states.
    /// </summary>
    [Flags]
    public enum EntityState
    {
        /// <summary>
        /// Detached
        /// </summary>
        Detached = 1,
        /// <summary>
        /// Unchanged
        /// </summary>
        Unchanged = 2,
        /// <summary>
        /// Added
        /// </summary>
        Added = 4,
        /// <summary>
        /// Deleted
        /// </summary>
        Deleted = 8,
        /// <summary>
        /// Modified
        /// </summary>
        Modified = 16,
    }
}
