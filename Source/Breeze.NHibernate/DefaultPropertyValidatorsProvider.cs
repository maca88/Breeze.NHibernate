using System;
using System.Collections.Generic;
using Breeze.NHibernate.Metadata;
using Breeze.NHibernate.Validation;

namespace Breeze.NHibernate
{
    /// <summary>
    /// The default implementation for <see cref="IPropertyValidatorsProvider"/>.
    /// </summary>
    public class DefaultPropertyValidatorsProvider : IPropertyValidatorsProvider
    {
        private static readonly Dictionary<DataType, string> DataTypeValidators = new Dictionary<DataType, string>
        {
            {DataType.Int16, "int16"},
            {DataType.Int32, "int32"},
            {DataType.Int64, "integer"},
            {DataType.Single, "number"},
            {DataType.Decimal, "number"},
            {DataType.DateTime, "date"},
            {DataType.DateTimeOffset, "date"},
            {DataType.Time, "duration"},
            {DataType.Boolean, "bool"},
            {DataType.Guid, "guid"},
            {DataType.Byte, "byte"}
        };

        /// <summary>
        /// Gets the validators for the given <see cref="DataProperty"/>.
        /// </summary>
        /// <param name="dataProperty">The data property.</param>
        /// <param name="type">The type containing the data property.</param>
        /// <returns>An enumerable of validators.</returns>
        public virtual IEnumerable<Validator> GetValidators(DataProperty dataProperty, Type type)
        {
            if (!dataProperty.IsNullable)
            {
                yield return new RequiredValidator();
            }

            if (dataProperty.MaxLength.HasValue)
            {
                yield return new MaxLengthValidator(dataProperty.MaxLength.Value);
            }

            if (dataProperty.DataType.HasValue && TryGetDataTypeValidator(dataProperty.DataType.Value, out var validator))
            {
                yield return validator;
            }
        }

        /// <summary>
        /// Tries to get a breeze validator for the given <see cref="DataType"/>.
        /// </summary>
        /// <param name="dataType">The data type.</param>
        /// <param name="validator">An output parameter for the found validator.</param>
        /// <returns>Whether the breeze validator was found.</returns>
        protected bool TryGetDataTypeValidator(DataType dataType, out Validator validator)
        {
            if (DataTypeValidators.TryGetValue(dataType, out var validatorName))
            {
                validator = new Validator(validatorName);
                return true;
            }

            validator = null;
            return false;
        }
    }
}
