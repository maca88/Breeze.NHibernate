using System;
using System.Collections.Generic;
using Breeze.NHibernate.Metadata;

namespace Breeze.NHibernate
{
    /// <summary>
    /// A provider that provides an enumerable of <see cref="Validator"/> based on the <see cref="DataProperty"/>.
    /// </summary>
    public interface IPropertyValidatorsProvider
    {
        /// <summary>
        /// Gets the validators for the given <see cref="DataProperty"/>.
        /// </summary>
        /// <param name="dataProperty">The data property.</param>
        /// <param name="entityType">The entity type containing the data property.</param>
        /// <returns>An enumerable of property validators.</returns>
        IEnumerable<Validator> GetValidators(DataProperty dataProperty, Type entityType);
    }
}
