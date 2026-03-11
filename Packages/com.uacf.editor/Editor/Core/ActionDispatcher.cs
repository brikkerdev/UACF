using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UACF.Models;

namespace UACF.Core
{
    public class ActionDispatcher
    {
        private readonly Dictionary<string, Func<JObject, Task<UacfResponse>>> _handlers = new Dictionary<string, Func<JObject, Task<UacfResponse>>>();

        public void Register(string action, Func<JObject, Task<UacfResponse>> handler)
        {
            _handlers[action] = handler;
        }

        public async Task<UacfResponse> DispatchAsync(string action, JObject @params)
        {
            if (string.IsNullOrEmpty(action))
                return UacfResponse.Fail("INVALID_REQUEST", "action is required", null, 0);

            if (_handlers.TryGetValue(action, out var handler))
            {
                try
                {
                    return await handler(@params ?? new JObject());
                }
                catch (Exception ex)
                {
                    return UacfResponse.Fail("INTERNAL_ERROR", ex.Message, ex.StackTrace, 0);
                }
            }

            return UacfResponse.Fail("ACTION_NOT_FOUND", $"Unknown action: {action}",
                $"Use api.list to see available actions. Similar: {FindSimilarActions(action)}", 0);
        }

        private string FindSimilarActions(string action)
        {
            var parts = action.Split('.');
            if (parts.Length == 0) return "api.list";
            var prefix = parts[0];
            var similar = new List<string>();
            foreach (var key in _handlers.Keys)
            {
                if (key.StartsWith(prefix) && similar.Count < 3)
                    similar.Add(key);
            }
            return similar.Count > 0 ? string.Join(", ", similar) : "api.list";
        }

        public IReadOnlyDictionary<string, Func<JObject, Task<UacfResponse>>> Handlers => _handlers;
    }
}
