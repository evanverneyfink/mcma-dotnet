using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mcma.Core.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mcma.Core.Serialization
{
    public class McmaObjectConverter : McmaJsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(McmaObject).IsAssignableFrom(objectType);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObj = JObject.Load(reader);

            var obj = Activator.CreateInstance(GetSerializedType(jObj, objectType));

            foreach (var jsonProp in jObj.Properties())
            {
                var clrProp = objectType.GetProperties().FirstOrDefault(p => p.CanWrite && p.Name.Equals(jsonProp.Name, StringComparison.OrdinalIgnoreCase));
                if (clrProp != null)
                {
                    try
                    {
                        clrProp.SetValue(obj, jsonProp.Value.Type != JTokenType.Null ? jsonProp.Value.ToObject(clrProp.PropertyType, serializer) : null);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to write property {clrProp.Name} on type {objectType.Name} with value {jsonProp.Value.ToString()}: {ex}");
                    }
                }
            }

            return obj;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("@type");
            writer.WriteValue(((IMcmaObject)value).Type);

            foreach (var property in value.GetType().GetProperties().Where(p => p.Name != nameof(IMcmaObject.Type) && p.CanRead))
            {
                var propValue = property.GetValue(value);
                if (propValue == null && serializer.NullValueHandling == NullValueHandling.Ignore)
                    continue;
                
                writer.WritePropertyName(char.ToLower(property.Name[0]) + property.Name.Substring(1));
                serializer.Serialize(writer, propValue);
            }

            writer.WriteEndObject();
        }
    }
}