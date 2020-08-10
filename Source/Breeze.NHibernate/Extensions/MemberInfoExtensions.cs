﻿using System;
using System.Linq;
using System.Reflection;

namespace Breeze.NHibernate.Extensions
{
    internal static class MemberInfoExtensions
    {
        /// <summary>
        /// Gets the member's underlying type.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns>The underlying type of the member.</returns>
        public static Type GetUnderlyingType(this MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
                case MemberTypes.Event:
                    return ((EventInfo)member).EventHandlerType;
                default:
                    throw new ArgumentException("MemberInfo must be of type FieldInfo, PropertyInfo or EventInfo", nameof(member));
            }
        }

        /// <summary>
        /// Determines whether the specified MemberInfo can be read.
        /// </summary>
        /// <param name="member">The MemberInfo to determine whether can be read.</param>
        /// /// <param name="nonPublic">if set to <c>true</c> then allow the member to be gotten non-publicly.</param>
        /// <returns>
        /// 	<c>true</c> if the specified MemberInfo can be read; otherwise, <c>false</c>.
        /// </returns>
        public static bool CanReadMemberValue(this MemberInfo member, bool nonPublic)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    var fieldInfo = (FieldInfo)member;

                    if (nonPublic)
                        return true;
                    else if (fieldInfo.IsPublic)
                        return true;
                    return false;
                case MemberTypes.Property:
                    var propertyInfo = (PropertyInfo)member;

                    if (!propertyInfo.CanRead)
                        return false;
                    if (nonPublic)
                        return true;
                    return (propertyInfo.GetGetMethod(nonPublic) != null);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines whether the specified MemberInfo can be set.
        /// </summary>
        /// <param name="member">The MemberInfo to determine whether can be set.</param>
        /// <param name="nonPublic">if set to <c>true</c> then allow the member to be set non-publicly.</param>
        /// <param name="canSetReadOnly">if set to <c>true</c> then allow the member to be set if read-only.</param>
        /// <returns>
        /// 	<c>true</c> if the specified MemberInfo can be set; otherwise, <c>false</c>.
        /// </returns>
        public static bool CanSetMemberValue(this MemberInfo member, bool nonPublic, bool canSetReadOnly)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    var fieldInfo = (FieldInfo)member;
                    if (fieldInfo.IsInitOnly && !canSetReadOnly)
                        return false;
                    return nonPublic || fieldInfo.IsPublic;
                case MemberTypes.Property:
                    var propertyInfo = (PropertyInfo)member;

                    if (!propertyInfo.CanWrite)
                        return false;

                    return (propertyInfo.GetSetMethod(nonPublic) != null);
                default:
                    return false;
            }
        }

        public static bool IsProperty(this MemberInfo member)
        {
            return member is PropertyInfo;
        }

        public static bool IsAutoProperty(this MemberInfo member, bool includeReadOnly)
        {
            if (!(member is PropertyInfo propertyInfo))
            {
                return false;
            }

            return IsAutoProperty(propertyInfo, includeReadOnly);
        }

        public static bool IsAutoProperty(this PropertyInfo property, bool includeReadOnly)
        {
            if ((!includeReadOnly && !property.CanWrite) || !property.CanRead)
            {
                return false;
            }

            var search = "<" + property.Name + ">";
            return property.DeclaringType?
                       .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                       .Any(f => f.Name.StartsWith(search)) == true;
        }
    }
}
