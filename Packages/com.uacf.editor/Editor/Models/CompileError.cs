using System;
using Unity.Plastic.Newtonsoft.Json;

namespace UACF.Models
{
    [Serializable]
    public class CompileError
    {
        [JsonProperty("message")]
        public string Message;

        [JsonProperty("file")]
        public string File;

        [JsonProperty("line")]
        public int Line;

        [JsonProperty("column")]
        public int Column;

        [JsonProperty("severity")]
        public string Severity;

        [JsonProperty("id")]
        public string Id;
    }
}
