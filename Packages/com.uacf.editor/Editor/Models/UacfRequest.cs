using Unity.Plastic.Newtonsoft.Json;

namespace UACF.Models
{
    public class UacfRequest
    {
        [JsonProperty("action")]
        public string Action;

        [JsonProperty("params")]
        public object Params;
    }
}
