using System;
using System.Xml;
using Newtonsoft.Json;

namespace Breeze.NHibernate.Serialization
{
    // http://www.w3.org/TR/xmlschema-2/#duration
    /// <summary>
    /// A <see cref="TimeSpan"/> converter that uses <see cref="XmlConvert"/> for reading and writing.
    /// </summary>
    public class TimeSpanConverter : JsonConverter
    {
        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var ts = (TimeSpan)value;
            var tsString = XmlConvert.ToString(ts);
            serializer.Serialize(writer, tsString);
        }

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var value = serializer.Deserialize<string>(reader);

            return XmlConvert.ToTimeSpan(value);
        }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TimeSpan) || objectType == typeof(TimeSpan?);
        }
    }
}
