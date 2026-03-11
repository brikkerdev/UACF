using System;
using Unity.Plastic.Newtonsoft.Json;

namespace UACF.Models
{
    public class UacfResponse
    {
        [JsonProperty("ok")]
        public bool Ok;

        [JsonProperty("data")]
        public object Data;

        [JsonProperty("warnings")]
        public string[] Warnings;

        [JsonProperty("error")]
        public UacfError Error;

        [JsonProperty("duration")]
        public double Duration;

        public static UacfResponse Success(object data, double durationSeconds, string[] warnings = null)
        {
            return new UacfResponse
            {
                Ok = true,
                Data = data,
                Duration = durationSeconds,
                Warnings = warnings?.Length > 0 ? warnings : null
            };
        }

        public static UacfResponse Fail(string code, string message, string suggestion = null, double durationSeconds = 0)
        {
            return new UacfResponse
            {
                Ok = false,
                Error = new UacfError
                {
                    Code = code,
                    Message = message,
                    Suggestion = suggestion
                },
                Duration = durationSeconds
            };
        }
    }

    public class UacfError
    {
        [JsonProperty("code")]
        public string Code;

        [JsonProperty("message")]
        public string Message;

        [JsonProperty("suggestion")]
        public string Suggestion;
    }
}
