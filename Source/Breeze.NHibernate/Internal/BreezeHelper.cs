using System;
using System.Globalization;
using NHibernate.Engine;
using NHibernate.Intercept;
using NHibernate.Proxy;
using NHibernate.Type;

namespace Breeze.NHibernate.Internal
{
    internal static class BreezeHelper
    {
        private static readonly string[] TypeDelimiter = {":#"};

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

        public static bool IsNullable(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null || !type.IsValueType;
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
