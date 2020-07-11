
namespace Breeze.NHibernate
{
    /// <summary>
    /// The default implementation for <see cref="ISyntheticPropertyNameConvention"/>.
    /// </summary>
    public class DefaultSyntheticPropertyNameConvention : ISyntheticPropertyNameConvention
    {
        /// <inheritdoc />
        public string GetName(string associationPropertyName, string associationPkPropertyName)
        {
            return $"{associationPropertyName}{associationPkPropertyName}";
        }
    }
}
