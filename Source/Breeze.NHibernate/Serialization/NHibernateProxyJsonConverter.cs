using System;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Proxy;

namespace Breeze.NHibernate.Serialization
{
    // Copied from Breeze.Persistence.NH
    internal class NHibernateProxyJsonConverter : JsonConverter
    {
        internal static readonly NHibernateProxyJsonConverter Instance = new NHibernateProxyJsonConverter();

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (NHibernateUtil.IsInitialized(value))
            {
                if (value is INHibernateProxy proxy)
                {
                    value = proxy.HibernateLazyInitializer.GetImplementation();
                }

                var resolver = serializer.ReferenceResolver;
                if (resolver.IsReferenced(serializer, value))
                {
                    // we've already written the object once; this time, just write the reference
                    // We have to do this manually because we have our own JsonConverter.
                    var valueRef = resolver.GetReference(serializer, value);
                    writer.WriteStartObject();
                    writer.WritePropertyName("$ref");
                    writer.WriteValue(valueRef);
                    writer.WriteEndObject();
                }
                else
                {
                    serializer.Serialize(writer, value);
                }
            }
            else
            {
                serializer.Serialize(writer, null);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(INHibernateProxy).IsAssignableFrom(objectType);
        }
    }
}
