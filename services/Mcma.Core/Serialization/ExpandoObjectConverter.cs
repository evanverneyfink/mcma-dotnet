using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Mcma.Core.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mcma.Core.Serialization
{
    public class ExpandoObjectConverter : McmaJsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(ExpandoObject);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObj = JObject.Load(reader);

            IDictionary<string, object> expando = new ExpandoObject();

            foreach (var jsonProp in jObj.Properties().Where(p => !p.Name.Equals(TypeJsonPropertyName, StringComparison.OrdinalIgnoreCase)))
                expando[jsonProp.Name.CamelCaseToPascalCase()] = ConvertJsonToClr(jsonProp.Value, serializer);

            return expando;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            foreach (var keyValuePair in (IDictionary<string, object>)value)
            {
                if (keyValuePair.Value == null && serializer.NullValueHandling == NullValueHandling.Ignore)
                    continue;

                writer.WritePropertyName(keyValuePair.Key.PascalCaseToCamelCase());
                serializer.Serialize(writer, keyValuePair.Value);
            }

            writer.WriteEndObject();
        }
    }
}