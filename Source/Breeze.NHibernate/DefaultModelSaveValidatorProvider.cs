using System;

namespace Breeze.NHibernate
{
    /// <summary>
    /// The default implementation of <see cref="IModelSaveValidatorProvider"/>, which uses <see cref="ModelSaveValidator"/> as a validator.
    /// </summary>
    public class DefaultModelSaveValidatorProvider : IModelSaveValidatorProvider
    {
        private readonly ModelSaveValidator _instance = new ModelSaveValidator();

        /// <inheritdoc />
        public IModelSaveValidator Get(Type modelType)
        {
            return _instance;
        }
    }
}
