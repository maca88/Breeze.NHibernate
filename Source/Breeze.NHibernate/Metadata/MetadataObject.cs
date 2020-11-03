using System;
using System.Collections.Generic;
using Breeze.NHibernate.Extensions;

namespace Breeze.NHibernate.Metadata
{
    /// <summary>
    /// The base metadata object.
    /// </summary>
    public abstract class MetadataObject : Dictionary<string, object>
    {
        /// <summary>
        /// Gets a property value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="name">The property name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The property value.</returns>
        protected T Get<T>(string name, T defaultValue = default)
        {
            name = name.ToLowerFirstChar();
            if (!TryGetValue(name, out var value))
            {
                return defaultValue;
            }

            return (T)value;
        }

        /// <summary>
        /// Gets or creates a property value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="name">The property name.</param>
        /// <param name="newValue">The initial value.</param>
        /// <returns>The property value.</returns>
        protected T GetOrCreate<T>(string name, Func<T> newValue)
        {
            var value = Get<T>(name);
            if (!Equals(value, default))
            {
                return value;
            }

            value = newValue();
            Set(name, value);

            return value;
        }

        /// <summary>
        /// Sets a value for a property name.
        /// </summary>
        /// <typeparam name="T">The type to set.</typeparam>
        /// <param name="name">The property name to set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="defaultValue">The default value.</param>
        protected void Set<T>(string name, T value, T defaultValue = default)
        {
            name = name.ToLowerFirstChar();
            if (Equals(value, defaultValue))
            {
                Remove(name);
                return;
            }

            this[name] = value;
        }
    }
}
