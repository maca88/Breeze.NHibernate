using System;

namespace Breeze.NHibernate
{
    /// <summary>
    /// A property attribute used by <see cref="ClientModelMetadataProvider"/> to find the inverse property of an association.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class InversePropertyAttribute : Attribute
    {
        public InversePropertyAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }

        /// <summary>
        /// The inverse property name.
        /// </summary>
        public string PropertyName { get; }
    }
}
