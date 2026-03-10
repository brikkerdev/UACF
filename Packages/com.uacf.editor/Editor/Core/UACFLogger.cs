using System;
using UnityEngine;
using UACF.Config;

namespace UACF.Core
{
    public static class UACFLogger
    {
        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
            if (level > UACFSettings.instance.LogLevel) return;

            var prefix = "[UACF] ";
            switch (level)
            {
                case LogLevel.Error:
                    Debug.LogError(prefix + message);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(prefix + message);
                    break;
                case LogLevel.Info:
                case LogLevel.Debug:
                default:
                    Debug.Log(prefix + message);
                    break;
            }
        }

        public static void LogRequest(string method, string path, int statusCode, long durationMs)
        {
            if (!UACFSettings.instance.LogRequests) return;
            var level = durationMs > 5000 ? LogLevel.Warning : LogLevel.Info;
            Log($"{method} {path} -> {statusCode} ({durationMs}ms)", level);
        }

        public static void LogError(string method, string path, string error, long durationMs)
        {
            Log($"{method} {path} -> 500 {error} ({durationMs}ms)", LogLevel.Error);
        }

        public static void DebugLog(string message)
        {
            Log(message, LogLevel.Debug);
        }
    }
}
