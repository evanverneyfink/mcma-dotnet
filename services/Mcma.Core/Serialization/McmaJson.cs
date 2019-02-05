using System.Collections.Generic;
using Mcma.Core.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Mcma.Core.Serialization
{
    public static class McmaJson
    {
        public static readonly JsonSerializerSettings DefaultSettings = new JsonSerializerSettings
        {
            ContractResolver = new McmaCamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            Converters =
            {
                new McmaObjectConverter(),
                new McmaDynamicObjectConverter()
            }
        };

        public static JsonSerializer Serializer { get; private set; } = JsonSerializer.CreateDefault(DefaultSettings);

        public static void SetJsonSerializerSettings(JsonSerializerSettings settings)
            => Serializer = JsonSerializer.CreateDefault(settings);

        public static T ToMcmaObject<T>(this JToken json) => json.ToObject<T>(Serializer);

        public static JToken ToMcmaJson<T>(this T obj) => JToken.FromObject(obj, Serializer);
    }
}