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

            var serializedType = GetSerializedType(jObj, objectType);

            var obj = Activator.CreateInstance(serializedType);

            foreach (var jsonProp in jObj.Properties())
                TryReadClrProperty(serializedType, obj, serializer, jsonProp);

            return obj;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            WriteTypeProperty(writer, value);

            WriteClrProperties(writer, value, serializer);

            writer.WriteEndObject();
        }
    }
}