using System;
using System.Collections.Generic;
using System.Globalization;
using Breeze.NHibernate.Metadata;
using NHibernate;
using NHibernate.Engine;
using NHibernate.Intercept;
using NHibernate.Proxy;
using NHibernate.Type;

namespace Breeze.NHibernate.Internal
{
    internal static class BreezeHelper
    {
        private static readonly string[] TypeDelimiter = {":#"};
        // Map of NH datatype to Breeze datatype.
        private static readonly Dictionary<string, DataType> BreezeTypeMap = new Dictionary<string, DataType>
        {
            {NHibernateUtil.Binary.Name, DataType.Binary},
            {NHibernateUtil.BinaryBlob.Name, DataType.Binary},
#pragma warning disable 618
            {NHibernateUtil.Timestamp.Name, DataType.DateTime},
#pragma warning restore 618
            {NHibernateUtil.TimeAsTimeSpan.Name, DataType.Time},
            {NHibernateUtil.UtcDateTime.Name, DataType.DateTime},
            {NHibernateUtil.LocalDateTime.Name, DataType.DateTime}
        };

        // List of DataTypes that have getNext method implemented in BreezeJS
        private static readonly HashSet<DataType> SupportedClientDataTypeGenerators = new HashSet<DataType>
        {
            DataType.String,
            DataType.Int64,
            DataType.Int32,
            DataType.Int16,
            DataType.Decimal,
            DataType.Double,
            DataType.Single,
            DataType.DateTime,
            DataType.DateTimeOffset,
            DataType.Guid,
        };

        public static bool SupportsClientGenerator(IType type)
        {
            return TryGetDataType(type, out var dataType) && SupportedClientDataTypeGenerators.Contains(dataType);
        }

        public static bool TryGetDataType(IType type, out DataType dataType)
        {
            if (type.IsComponentType)
            {
                dataType = DataType.Undefined;
                return false;
            }

            if (BreezeTypeMap.TryGetValue(type.Name, out dataType))
            {
                return true;
            }

            if (Enum.TryParse(type.Name, out dataType))
            {
                return true;
            }

            if (type is AbstractEnumType)
            {
                dataType = DataType.String;
                return true;
            }

            dataType = DataType.Undefined;
            return false;
        }

        public static object ConvertToType(object value, Type toType)
        {
            if (value == null)
            {
                return null;
            }

            if ((Nullable.GetUnderlyingType(toType) ?? toType) == typeof(Guid))
            {
                return Guid.Parse(value.ToString());
            }

            return Convert.ChangeType(value, toType, CultureInfo.InvariantCulture);
        }

        public static DataType GetDataType(IType type)
        {
            if (TryGetDataType(type, out var dataType))
            {
                return dataType;
            }

            throw new NotSupportedException($"Unknown NHibernate type {type.Name}");
        }

        public static int? GetTypeLength(IType type, ISessionFactoryImplementor sessionFactory)
        {
            if (type.IsComponentType)
            {
                return null;
            }

            var sqlType = type.SqlTypes(sessionFactory)[0];
            return sqlType.LengthDefined ? sqlType.Length : (int?) null;
        }

        public static string GetBreezeTypeFullName(Type type)
        {
            return $"{type.Name}:#{type.Namespace}";
        }

        public static string GetEntityName(string breezeTypeName)
        {
            var parts = breezeTypeName.Split(TypeDelimiter, StringSplitOptions.None);

            return $"{parts[1]}.{parts[0]}";
        }

        public static Type GetEntityType(object entity, bool allowInitialization = true)
        {
            if (entity is INHibernateProxy nhProxy)
            {
                if (nhProxy.HibernateLazyInitializer.IsUninitialized && !allowInitialization)
                {
                    return nhProxy.HibernateLazyInitializer.PersistentClass;
                }

                // We have to initialize in case of a subclass to get the concrete type
                entity = nhProxy.HibernateLazyInitializer.GetImplementation();
            }

            switch (entity)
            {
                case IFieldInterceptorAccessor interceptorAccessor:
                    return interceptorAccessor.FieldInterceptor.MappedClass;
                default:
                    return entity.GetType();
            }
        }
    }
}
