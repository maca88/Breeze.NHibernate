using System;
using Breeze.NHibernate.Metadata;

namespace Breeze.NHibernate.Validation
{
    /// <summary>
    /// Server side maxLength validator.
    /// </summary>
    public class MaxLengthValidator : Validator, IBreezeValidator
    {
        public MaxLengthValidator(int maxLength) : base("maxLength")
        {
            MaxLength = maxLength;
        }

        /// <summary>
        /// The string max length.
        /// </summary>
        public int MaxLength
        {
            get => Get<int>(nameof(MaxLength));
            private set => Set(nameof(MaxLength), value);
        }

        /// <inheritdoc />
        public string Validate(object value)
        {
            if (MaxLength < 0)
            {
                throw new Exception("Validator maxLength must be >= 0");
            }

            if (!(value is string str))
            {
                return null;
            }

            return str.Length > MaxLength ? $"Must be a string with {MaxLength} characters or less" : null;
        }
    }
}
