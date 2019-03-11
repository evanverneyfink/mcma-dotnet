using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
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
            try
            {
                var jObj = JObject.Load(reader);

                var serializedType = GetSerializedType(jObj, objectType);
                var dynamicObj = (IDictionary<string, object>)Activator.CreateInstance(serializedType);

                foreach (var jsonProp in jObj.Properties().Where(p => !p.Name.Equals(TypeJsonPropertyName, StringComparison.OrdinalIgnoreCase)))
                    if (!TryReadClrProperty(objectType, dynamicObj, serializer, jsonProp))
                        dynamicObj[jsonProp.Name.CamelCaseToPascalCase()] = ConvertJsonToClr(jsonProp.Value, serializer);

                return dynamicObj;
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred reading JSON for an object of type {objectType.Name}. See inner exception for details.", ex);
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            WriteTypeProperty(writer, value);

            WriteClrProperties(writer, value, serializer);

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