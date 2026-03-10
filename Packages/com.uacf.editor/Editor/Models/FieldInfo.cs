using System;
using Unity.Plastic.Newtonsoft.Json;

namespace UACF.Models
{
    [Serializable]
    public class FieldInfo
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("value")]
        public object Value;

        [JsonProperty("serialized")]
        public bool Serialized;
    }

    [Serializable]
    public class FieldSetResult
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("value")]
        public object Value;

        [JsonProperty("status")]
        public string Status;
    }
}
