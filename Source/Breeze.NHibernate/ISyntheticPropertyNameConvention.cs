
namespace Breeze.NHibernate
{
    /// <summary>
    /// Defines a naming convention for synthetic foreign key property names.
    /// </summary>
    public interface ISyntheticPropertyNameConvention
    {
        /// <summary>
        /// Gets the name of the synthetic foreign key property
        /// </summary>
        /// <param name="associationPropertyName">The association property name</param>
        /// <param name="associationPkPropertyName">The association primary key property name</param>
        /// <returns>The synthetic foreign key property name.</returns>
        string GetName(string associationPropertyName, string associationPkPropertyName);
    }
}
