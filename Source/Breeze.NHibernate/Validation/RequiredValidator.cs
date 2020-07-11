using Breeze.NHibernate.Metadata;

namespace Breeze.NHibernate.Validation
{
    /// <summary>
    /// Server side required validator.
    /// </summary>
    public class RequiredValidator : Validator, IBreezeValidator
    {
        public RequiredValidator(bool allowEmptyStrings = false) : base("required")
        {
            AllowEmptyStrings = allowEmptyStrings;
        }

        /// <summary>
        /// Whether empty strings are allowed.
        /// </summary>
        public bool AllowEmptyStrings
        {
            get => Get<bool>(nameof(AllowEmptyStrings));
            private set => Set(nameof(AllowEmptyStrings), value);
        }

        /// <inheritdoc />
        public string Validate(object value)
        {
            return value switch
            {
                null => "Is required.",
                string str when !AllowEmptyStrings && string.IsNullOrWhiteSpace(str) => "Is required.",
                _ => null
            };
        }
    }
}
