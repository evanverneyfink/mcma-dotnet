using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mcma.Core.Serialization
{
    public class McmaDynamicObjectConverter : McmaJsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(McmaDynamicObject).IsAssignableFrom(objectType);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObj = JObject.Load(reader);

            var dict = (IDictionary<string, object>)Activator.CreateInstance(GetSerializedType(jObj, objectType));

            foreach (var jsonProp in jObj.Properties())
            {
                if (jsonProp.Value is JObject childJObj && childJObj["@type"] != null)
                {
                    var childObjType = GetSerializedType(childJObj, null);
                    if (childObjType != null)
                    {
                        dict[jsonProp.Name] = jsonProp.Value.ToObject(childObjType, serializer);
                        continue;
                    }
                }
                
                dict[jsonProp.Name] = jsonProp.Value;
            }

            return dict;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("@type");
            writer.WriteValue(((IMcmaObject)value).Type);

            foreach (var keyValuePair in (IDictionary<string, object>)value)
            {
                writer.WritePropertyName(keyValuePair.Key);

                serializer.Serialize(writer, keyValuePair.Value);
            }

            writer.WriteEndObject();
        }
    }
}