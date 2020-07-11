using Breeze.NHibernate.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Breeze.NHibernate.AspNetCore.Mvc {
  internal class MvcNewtonsoftJsonOptionsConfigurator : IConfigureOptions<MvcNewtonsoftJsonOptions> {
    private readonly IJsonSerializerSettingsProvider _jsonSerializerSettingsProvider;

    public MvcNewtonsoftJsonOptionsConfigurator(IJsonSerializerSettingsProvider jsonSerializerSettingsProvider) {
      _jsonSerializerSettingsProvider = jsonSerializerSettingsProvider;
    }

    public void Configure(MvcNewtonsoftJsonOptions options) {
      ApplySerializerSettings(_jsonSerializerSettingsProvider.GetDefault(), options.SerializerSettings);
    }

    private static void ApplySerializerSettings(JsonSerializerSettings fromSettings, JsonSerializerSettings toSettings) {
      foreach (var converter in fromSettings.Converters) {
        toSettings.Converters.Add(converter);
      }

      toSettings.TypeNameHandling = fromSettings.TypeNameHandling;
      toSettings.MetadataPropertyHandling = fromSettings.MetadataPropertyHandling;
      toSettings.TypeNameAssemblyFormatHandling = fromSettings.TypeNameAssemblyFormatHandling;
      toSettings.PreserveReferencesHandling = fromSettings.PreserveReferencesHandling;
      toSettings.ReferenceLoopHandling = fromSettings.ReferenceLoopHandling;
      toSettings.MissingMemberHandling = fromSettings.MissingMemberHandling;
      toSettings.ObjectCreationHandling = fromSettings.ObjectCreationHandling;
      toSettings.NullValueHandling = fromSettings.NullValueHandling;
      toSettings.DefaultValueHandling = fromSettings.DefaultValueHandling;
      toSettings.ConstructorHandling = fromSettings.ConstructorHandling;
      toSettings.Context = fromSettings.Context;
      toSettings.CheckAdditionalContent = fromSettings.CheckAdditionalContent;
      toSettings.Error = fromSettings.Error;
      toSettings.ContractResolver = fromSettings.ContractResolver;
      toSettings.ReferenceResolverProvider = fromSettings.ReferenceResolverProvider;
      toSettings.TraceWriter = fromSettings.TraceWriter;
      toSettings.EqualityComparer = fromSettings.EqualityComparer;
      toSettings.SerializationBinder = fromSettings.SerializationBinder;
      toSettings.Formatting = fromSettings.Formatting;
      toSettings.DateFormatHandling = fromSettings.DateFormatHandling;
      toSettings.DateTimeZoneHandling = fromSettings.DateTimeZoneHandling;
      toSettings.DateParseHandling = fromSettings.DateParseHandling;
      toSettings.DateFormatString = fromSettings.DateFormatString;
      toSettings.FloatFormatHandling = fromSettings.FloatFormatHandling;
      toSettings.FloatParseHandling = fromSettings.FloatParseHandling;
      toSettings.StringEscapeHandling = fromSettings.StringEscapeHandling;
      toSettings.Culture = fromSettings.Culture;
      toSettings.MaxDepth = fromSettings.MaxDepth;
    }
  }
}
