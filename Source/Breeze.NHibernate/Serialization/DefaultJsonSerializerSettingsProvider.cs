using System;
using System.Threading;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Breeze.NHibernate.Serialization
{
    /// <summary>
    /// The default implementation of <see cref="IJsonSerializerSettingsProvider"/>
    /// </summary>
    public class DefaultJsonSerializerSettingsProvider : IJsonSerializerSettingsProvider
    {
        private readonly BreezeContractResolver _breezeContractResolver;
        private readonly Lazy<JsonSerializerSettings> _serializerSettings;
        private readonly Lazy<JsonSerializerSettings> _serializerSettingsForSave;

        public DefaultJsonSerializerSettingsProvider(BreezeContractResolver breezeContractResolver)
        {
            _breezeContractResolver = breezeContractResolver;
            _serializerSettings = new Lazy<JsonSerializerSettings>(CreateJsonSerializerSettings, LazyThreadSafetyMode.PublicationOnly);
            _serializerSettingsForSave = new Lazy<JsonSerializerSettings>(CreateJsonSerializerSettingsForSave, LazyThreadSafetyMode.PublicationOnly);
        }

        /// <inheritdoc />
        public JsonSerializerSettings GetDefault()
        {
            return _serializerSettings.Value;
        }

        /// <inheritdoc />
        public JsonSerializerSettings GetForSave()
        {
            return _serializerSettingsForSave.Value;
        }

        /// <summary>
        /// Creates the <see cref="JsonSerializerSettings"/> for saving.
        /// </summary>
        /// <returns>The <see cref="JsonSerializerSettings"/> for saving.</returns>
        protected virtual JsonSerializerSettings CreateJsonSerializerSettingsForSave()
        {
            var settings = CreateJsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.None; // For security reasons, to prevent instantiating unwanted types

            return settings;
        }

        /// <summary>
        /// Creates the default <see cref="JsonSerializerSettings"/> used to serializing query results.
        /// </summary>
        /// <returns>The <see cref="JsonSerializerSettings"/> for saving.</returns>
        protected virtual JsonSerializerSettings CreateJsonSerializerSettings()
        {
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = _breezeContractResolver,
                NullValueHandling = NullValueHandling.Include,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Objects,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
            };

            // Hack is for the issue described in this post:
            // http://stackoverflow.com/questions/11789114/internet-explorer-json-net-javascript-date-and-milliseconds-issue
            jsonSerializerSettings.Converters.Add(new IsoDateTimeConverter
            {
                DateTimeFormat = "yyyy-MM-dd\\THH:mm:ss.fffK"
            });
            // Needed because JSON.NET does not natively support I8601 Duration formats for TimeSpan
            jsonSerializerSettings.Converters.Add(new TimeSpanConverter());
            jsonSerializerSettings.Converters.Add(new StringEnumConverter());

            return jsonSerializerSettings;
        }
    }
}
