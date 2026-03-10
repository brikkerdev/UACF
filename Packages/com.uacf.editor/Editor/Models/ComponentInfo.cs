using System;
using Unity.Plastic.Newtonsoft.Json;

namespace UACF.Models
{
    [Serializable]
    public class ComponentInfo
    {
        [JsonProperty("component_type")]
        public string ComponentType;

        [JsonProperty("game_object")]
        public string GameObject;

        [JsonProperty("instance_id")]
        public int InstanceId;

        [JsonProperty("fields")]
        public System.Collections.Generic.Dictionary<string, FieldValueInfo> Fields;
    }

    [Serializable]
    public class FieldValueInfo
    {
        [JsonProperty("value")]
        public object Value;

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("serialized")]
        public bool Serialized;

        [JsonProperty("is_object_reference")]
        public bool IsObjectReference;
    }

    [Serializable]
    public class ComponentTypeInfo
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("full_name")]
        public string FullName;

        [JsonProperty("category")]
        public string Category;
    }
}
