using System;
using System.Collections.Generic;
using Mcma.Core.Utility;
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
            var resource = dict as IMcmaResource;

            foreach (var jsonProp in jObj.Properties())
            {
                // check if this is the ID property of a dynamic resource, which needs to be treated as a special case
                if (jsonProp.Name.Equals(nameof(IMcmaResource.Id), StringComparison.OrdinalIgnoreCase) && resource != null)
                {
                    resource.Id = jsonProp.Value.Value<string>();
                    continue;
                }

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

            if (value is IMcmaResource resource)
            {
                writer.WritePropertyName("id");
                writer.WriteValue(resource.Id);
            }

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