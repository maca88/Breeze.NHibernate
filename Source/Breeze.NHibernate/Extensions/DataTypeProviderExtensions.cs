using System;
using Breeze.NHibernate.Metadata;
using NHibernate.Type;

namespace Breeze.NHibernate.Extensions
{
    internal static class DataTypeProviderExtensions
    {
        public static DataType GetDataType(this IDataTypeProvider provider, IType type)
        {
            return provider.TryGetDataType(type, out var dataType) ? dataType : provider.GetDefaultType();
        }

        public static DataType GetDataType(this IDataTypeProvider provider, Type type)
        {
            return provider.TryGetDataType(type, out var dataType) ? dataType : provider.GetDefaultType();
        }
    }
}
