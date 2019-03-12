using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Mcma.Core.Logging;
using Mcma.Core.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mcma.Core.Serialization
{
    public abstract class McmaJsonConverter : JsonConverter
    {
        protected const string TypeJsonPropertyName = "@type";

        protected Type GetSerializedType(JObject jObj, Type objectType)
        {
            var typeProperty = jObj.Property(TypeJsonPropertyName);
            if (typeProperty != null)
            {
                var typeString = typeProperty.Value.Value<string>();

                objectType = McmaTypes.FindType(typeString);
                if (objectType != null)
                {
                    jObj.Remove(TypeJsonPropertyName);
                    
                    return objectType;
                }
            }

            return objectType;
        }

        protected bool TryReadClrProperty(Type objectType, object obj, JsonSerializer serializer, JProperty jsonProp)
        {
            var clrProp = objectType.GetProperties().FirstOrDefault(p => p.CanWrite && p.Name.Equals(jsonProp.Name, StringComparison.OrdinalIgnoreCase));
            if (clrProp != null)
            {
                try
                {
                    clrProp.SetValue(obj, jsonProp.Value.Type != JTokenType.Null ? jsonProp.Value.ToObject(clrProp.PropertyType, serializer) : null);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to set property {clrProp.Name} on type {objectType.Name} with JSON value {jsonProp.Value.ToString()}: {ex}");
                }
            }

            return false;
        }

        protected bool IsMcmaObject(JObject jObj)
            => jObj.Properties().Any(p => p.Name.Equals(TypeJsonPropertyName, StringComparison.OrdinalIgnoreCase));

        protected object CreateMcmaObject(JObject jObj, JsonSerializer serializer)
        {
            var objType = GetSerializedType(jObj, null);
            if (objType == null)
                throw new Exception($"Unrecognized @type specified in JSON: {jObj[TypeJsonPropertyName]}");
            
            return jObj.ToObject(objType, serializer);
        }

        protected void WriteTypeProperty(JsonWriter writer, object value)
        {
            writer.WritePropertyName(TypeJsonPropertyName);
            writer.WriteValue(((IMcmaObject)value).Type);
        }

        protected void WriteClrProperties(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var properties =
                value.GetType().GetProperties()
                    .Where(p => p.Name != nameof(IMcmaObject.Type) && p.CanRead && p.GetIndexParameters().Length == 0)
                    .ToList();
                    
            foreach (var property in properties)
            {
                var propValue = property.GetValue(value);
                if (propValue == null && serializer.NullValueHandling == NullValueHandling.Ignore)
                    continue;
                
                writer.WritePropertyName(char.ToLower(property.Name[0]) + property.Name.Substring(1));
                serializer.Serialize(writer, propValue);
            }
        }

        protected object ConvertJsonToClr(JToken token, JsonSerializer serializer)
        {
            switch (token.Type)
            {
                case JTokenType.Boolean:
                    return token.Value<bool>();
                case JTokenType.Bytes:
                    return token.Value<byte[]>();
                case JTokenType.Date:
                    return token.Value<DateTime>();
                case JTokenType.Float:
                    return token.Value<decimal>();
                case JTokenType.Guid:
                    return token.Value<Guid>();
                case JTokenType.Integer:
                    return token.Value<long>();
                case JTokenType.String:
                case JTokenType.Uri:
                    return token.Value<string>();
                case JTokenType.TimeSpan:
                    return token.Value<TimeSpan>();
                case JTokenType.Null:
                case JTokenType.Undefined:
                    return null;
                case JTokenType.Array:
                    return token.Select(x => ConvertJsonToClr(x, serializer)).ToArray();
                case JTokenType.Object:
                    var jObj = (JObject)token;
                    return IsMcmaObject(jObj) ? CreateMcmaObject(jObj, serializer) : CreateExpando(jObj, serializer);
                default:
                    return token;
            }
        }

        protected object CreateExpando(JObject jObj, JsonSerializer serializer)
        {
            IDictionary<string, object> expando = new ExpandoObject();

            foreach (var property in jObj.Properties())
                expando[property.Name.CamelCaseToPascalCase()] = ConvertJsonToClr(property.Value, serializer);

            return expando;
        }
    }
}