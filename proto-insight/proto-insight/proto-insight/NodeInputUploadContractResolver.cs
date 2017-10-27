using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using proto_contract;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;

namespace proto_insight
{
    public class NodeInputUploadContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
    {
        public override JsonContract ResolveContract(Type type)
        {
            var contract = base.CreateContract(type);

            if (type == typeof(NodeInput))
            {
                contract.Converter = new NodeInputJsonConverter();
            }

            return contract;
        }
    }

    public class NodeInputJsonConverter : JsonConverter
    {
        private Dictionary<string, Type> unqualifiedIndex;

        public NodeInputJsonConverter()
        {
            var convertableTypes = from nodeInputType in typeof(NodeInput).Assembly.GetExportedTypes()
                                   where typeof(NodeInput).IsAssignableFrom(nodeInputType)
                                   select nodeInputType;

            this.unqualifiedIndex = convertableTypes.ToDictionary(k => k.Name.ToLower(), v => v);
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(NodeInput).IsAssignableFrom(objectType);
        }

        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            object result = null;

            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var jObj = JObject.Load(reader);

            Type inputType = null;
            var inputTypeName = jObj["EventType"];

            if (inputTypeName != null)
            {
                unqualifiedIndex.TryGetValue(inputTypeName.Value<string>(), out inputType);
            }
            else
            {
                return null;
            }

            if (inputType != null)
            {
                result = jObj.ToObject(inputType, serializer);
            }

            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
        }
    }
}