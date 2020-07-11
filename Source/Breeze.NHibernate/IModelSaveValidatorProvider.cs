using System;

namespace Breeze.NHibernate
{
    /// <summary>
    /// A provider for <see cref="IModelSaveValidator"/> used by <see cref="PersistenceManager.SaveChanges{TModel}"/> to validate the dependency graph.
    /// </summary>
    public interface IModelSaveValidatorProvider
    {
        /// <summary>
        /// Gets the model save validator for the given type.
        /// </summary>
        /// <param name="modelType">The model type.</param>
        /// <returns>The model save validator.</returns>
        IModelSaveValidator Get(Type modelType);
    }
}
