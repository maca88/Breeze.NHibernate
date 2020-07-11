using System;

namespace Breeze.NHibernate
{
    /// <summary>
    /// A property attribute used by <see cref="ClientModelMetadataProvider"/> in order to detect whether property is
    /// a complex type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ComplexTypeAttribute : Attribute
    {
    }
}
