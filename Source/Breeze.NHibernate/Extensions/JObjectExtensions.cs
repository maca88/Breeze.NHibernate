using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Breeze.NHibernate.Extensions
{
    internal static class JObjectExtensions
    {
        public static Dictionary<string, object> ToDictionary(this JObject json)
        {
            return json?.Cast<JProperty>().ToDictionary(o => o.Name, prop =>
            {
                switch (prop.Value)
                {
                    case JValue val:
                        return val.Value;
                    case JArray array:
                        return array;
                    default:
                        return prop.Value as JObject;
                }
            });
        }
    }
}
