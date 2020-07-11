using System;
using System.Collections.Generic;

namespace Breeze.NHibernate
{
    /// <summary>
    /// A validator that validates whether the provided <see cref="DependencyGraph"/> is valid for the given root model type.
    /// </summary>
    public interface IModelSaveValidator
    {
        /// <summary>
        /// Validates whether the provided <see cref="DependencyGraph"/> is valid for the given model type.
        /// </summary>
        /// <param name="rootModelType">The root model type that is being saved.</param>
        /// <param name="dependencyGraph">The dependency graph of <see cref="EntityInfo"/> being saved.</param>
        /// <returns>An enumerable of <see cref="EntityInfo"/> of root model type.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the <see cref="DependencyGraph"/> is not valid for the given root model type.</exception>
        IEnumerable<EntityInfo> Validate(Type rootModelType, DependencyGraph dependencyGraph);
    }
}
