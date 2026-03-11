using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UACF.Core;
using UACF.Models;

namespace UACF.Handlers
{
    public static class ConsoleHandler
    {
        public static void Register(ActionDispatcher dispatcher)
        {
            dispatcher.Register("console.get", HandleGet);
            dispatcher.Register("console.clear", HandleClear);
        }

        private static Task<UacfResponse> HandleGet(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var typeFilter = p["type"]?.ToString();
                var last = p["last"]?.Value<int>() ?? 50;
                var contains = p["contains"]?.ToString();

                var entries = GetLogEntries(typeFilter, contains, last);
                return UacfResponse.Success(new { entries }, 0);
            });
        }

        private static object[] GetLogEntries(string typeFilter, string contains, int maxCount)
        {
            try
            {
                var logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor");
                if (logEntriesType == null) return new object[0];

                var getCounts = logEntriesType.GetMethod("GetCountsByType", BindingFlags.Static | BindingFlags.Public);
                if (getCounts == null) return new object[0];

                var counts = getCounts.Invoke(null, null) as int[];
                if (counts == null || counts.Length < 3) return new object[0];

                var entries = new List<object>();
                var startRow = Math.Max(0, counts[0] + counts[1] + counts[2] - maxCount);
                var rowCount = Math.Min(maxCount, counts[0] + counts[1] + counts[2]);

                var getEntryMethod = logEntriesType.GetMethod("GetEntry", BindingFlags.Static | BindingFlags.NonPublic);
                if (getEntryMethod == null) return new object[0];

                var entryType = logEntriesType.Assembly.GetType("UnityEditor.LogEntry");
                if (entryType == null) return new object[0];

                var messageField = entryType.GetField("message");
                var stackField = entryType.GetField("stackTrace");
                var modeField = entryType.GetField("mode");
                var instanceIdField = entryType.GetField("instanceID");
                var countField = entryType.GetField("count");

                var args = new object[] { 0, null };
                for (int i = startRow; i < startRow + rowCount && i < counts[0] + counts[1] + counts[2]; i++)
                {
                    args[0] = i;
                    args[1] = Activator.CreateInstance(entryType);
                    getEntryMethod.Invoke(null, args);

                    var entry = args[1];
                    var msg = messageField?.GetValue(entry)?.ToString() ?? "";
                    var stack = stackField?.GetValue(entry)?.ToString() ?? "";
                    var mode = (int)(modeField?.GetValue(entry) ?? 0);
                    var count = (int)(countField?.GetValue(entry) ?? 1);

                    var typeStr = mode == 1 ? "error" : mode == 2 ? "warning" : "log";
                    if (!string.IsNullOrEmpty(typeFilter) && typeFilter != typeStr) continue;
                    if (!string.IsNullOrEmpty(contains) && msg?.IndexOf(contains, StringComparison.OrdinalIgnoreCase) < 0) continue;

                    entries.Add(new { type = typeStr, message = msg, stackTrace = stack, count });
                }

                return entries.ToArray();
            }
            catch
            {
                return new object[] { new { type = "info", message = "Console API not available in this Unity version" } };
            }
        }

        private static Task<UacfResponse> HandleClear(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                try
                {
                    var logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor");
                    var clearMethod = logEntriesType?.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
                    clearMethod?.Invoke(null, null);
                    return UacfResponse.Success(new { cleared = true }, 0);
                }
                catch
                {
                    return UacfResponse.Success(new { cleared = false, message = "Clear not available" }, 0);
                }
            });
        }
    }
}
