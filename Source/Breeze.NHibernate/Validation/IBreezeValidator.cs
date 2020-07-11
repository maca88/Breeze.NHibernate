namespace Breeze.NHibernate.Validation
{
    /// <summary>
    /// Defines a breeze validator that simulates the same validation done by the client.
    /// </summary>
    public interface IBreezeValidator
    {
        /// <summary>
        /// The validation name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Validates the value.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <returns>The validation error message or <see langword="null"/> otherwise.</returns>
        string Validate(object value);
    }
}
