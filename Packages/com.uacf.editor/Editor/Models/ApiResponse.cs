using System;
using Unity.Plastic.Newtonsoft.Json;

namespace UACF.Models
{
    public enum ErrorCode
    {
        INVALID_REQUEST,
        NOT_FOUND,
        COMPILE_ERROR,
        TYPE_NOT_FOUND,
        FIELD_NOT_FOUND,
        SCENE_NOT_LOADED,
        INTERNAL_ERROR,
        SERVER_BUSY,
        CONFLICT
    }

    [Serializable]
    public class ApiResponse
    {
        [JsonProperty("success")]
        public bool Success;

        [JsonProperty("data")]
        public object Data;

        [JsonProperty("error")]
        public ErrorInfo Error;

        [JsonProperty("timestamp")]
        public string Timestamp;

        [JsonProperty("duration_ms")]
        public long DurationMs;

        public static ApiResponse Ok(object data, long durationMs = 0)
        {
            return new ApiResponse
            {
                Success = true,
                Data = data,
                Timestamp = DateTime.UtcNow.ToString("o"),
                DurationMs = durationMs
            };
        }

        public static ApiResponse Fail(ErrorCode code, string message, object details = null, long durationMs = 0)
        {
            return new ApiResponse
            {
                Success = false,
                Error = new ErrorInfo
                {
                    Code = code.ToString(),
                    Message = message,
                    Details = details
                },
                Timestamp = DateTime.UtcNow.ToString("o"),
                DurationMs = durationMs
            };
        }
    }

    [Serializable]
    public class ErrorInfo
    {
        [JsonProperty("code")]
        public string Code;

        [JsonProperty("message")]
        public string Message;

        [JsonProperty("details")]
        public object Details;
    }
}
