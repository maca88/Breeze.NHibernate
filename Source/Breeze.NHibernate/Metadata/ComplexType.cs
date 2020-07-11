
using System;

namespace Breeze.NHibernate.Metadata
{
    public class ComplexType : StructuralType
    {
        public ComplexType(Type type) : base(type)
        {
            IsComplexType = true;
        }

        /// <summary>
        /// This must be 'true'. This field is what distinguishes an entityType from a complexType.
        /// </summary>
        public bool IsComplexType
        {
            get => Get<bool>(nameof(IsComplexType));
            set => Set(nameof(IsComplexType), value);
        }
    }
}
