
using System;
using System.Linq;
using System.Reflection;

namespace Breeze.NHibernate.Extensions
{
    internal static class TypeExtensions
    {
        public static bool TryGetGenericType(this Type type, Type openGenericType, out Type genericType)
        {
            genericType = GetGenericType(type, openGenericType);
            return genericType != null;
        }

        public static Type GetGenericType(this Type givenType, Type genericType)
        {
            while (true)
            {
                var typeInfo = givenType.GetTypeInfo();
                if (typeInfo.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
                {
                    return givenType;
                }

                var type = givenType.GetInterfaces().FirstOrDefault(it =>
                    it.GetTypeInfo().IsGenericType && it.GetGenericTypeDefinition() == genericType);
                if (type != null)
                {
                    return type;
                }

                var baseType = typeInfo.BaseType;
                if (baseType == null)
                {
                    return null;
                }

                givenType = baseType;
            }
        }
    }
}
