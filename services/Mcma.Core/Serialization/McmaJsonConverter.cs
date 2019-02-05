using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mcma.Core.Serialization
{
    public abstract class McmaJsonConverter : JsonConverter
    {
        protected Type GetSerializedType(JObject jObj, Type objectType)
        {
            var typeProperty = jObj.Property("@type");
            if (typeProperty != null)
            {
                var typeString = typeProperty.Value.Value<string>();

                objectType = McmaTypes.FindType(typeString);
                if (objectType != null)
                {
                    jObj.Remove("@type");
                    
                    return objectType;
                }
            }

            return objectType;
        }
    }
}