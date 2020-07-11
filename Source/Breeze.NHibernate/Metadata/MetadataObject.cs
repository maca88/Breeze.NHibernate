using System;
using System.Collections.Generic;
using Breeze.NHibernate.Extensions;

namespace Breeze.NHibernate.Metadata
{
    public abstract class MetadataObject : Dictionary<string, object>
    {
        protected T Get<T>(string name, T defaultValue = default)
        {
            name = name.ToLowerFirstChar();
            if (!TryGetValue(name, out var value))
            {
                return defaultValue;
            }

            return (T)value;
        }

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
