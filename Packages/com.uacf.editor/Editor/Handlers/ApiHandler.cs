using System.Linq;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UACF.Core;
using UACF.Models;

namespace UACF.Handlers
{
    public static class ApiHandler
    {
        public static void Register(ActionDispatcher dispatcher)
        {
            dispatcher.Register("api.list", HandleList);
            dispatcher.Register("api.help", HandleHelp);
            dispatcher.Register("api.prompt", HandlePrompt);
            dispatcher.Register("api.logs", HandleLogs);
        }

        private static Task<UacfResponse> HandleList(JObject p)
        {
            var actions = ActionsRegistry.All.Select(a => new
            {
                action = a.Action,
                description = a.Description,
                @params = a.Params.Select(pd => new
                {
                    name = pd.Name,
                    type = pd.Type,
                    required = pd.Required,
                    description = pd.Description
                }).ToArray(),
                example = a.Example
            }).ToArray();

            return Task.FromResult(UacfResponse.Success(new
            {
                version = ActionsRegistry.Version,
                actions
            }, 0));
        }

        private static Task<UacfResponse> HandleHelp(JObject p)
        {
            var action = p["action"]?.ToString();
            if (string.IsNullOrEmpty(action))
                return Task.FromResult(UacfResponse.Fail("INVALID_REQUEST", "params.action is required", null, 0));

            var def = ActionsRegistry.Find(action);
            if (def == null)
                return Task.FromResult(UacfResponse.Fail("NOT_FOUND", $"Action '{action}' not found",
                    $"Use api.list to see available actions", 0));

            return Task.FromResult(UacfResponse.Success(new
            {
                action = def.Action,
                description = def.Description,
                @params = def.Params.Select(pd => new
                {
                    name = pd.Name,
                    type = pd.Type,
                    required = pd.Required,
                    description = pd.Description
                }).ToArray(),
                example = def.Example
            }, 0));
        }

        private static Task<UacfResponse> HandlePrompt(JObject p)
        {
            var format = p["format"]?.ToString() ?? "compact";
            var prompt = format == "full"
                ? GetFullPrompt()
                : GetCompactPrompt();

            return Task.FromResult(UacfResponse.Success(new { prompt }, 0));
        }

        private static string GetCompactPrompt()
        {
            return @"You have access to Unity Editor through UACF (Unity Autonomous Control Framework).

All requests: POST http://localhost:7890/uacf with JSON body:
{ ""action"": ""action.name"", ""params"": { ... } }

Response format: { ""ok"": true|false, ""data"": ..., ""error"": {...}, ""duration"": seconds }

Use api.list to get all available actions. Use api.help with params.action to get help for specific action.";
        }

        private static string GetFullPrompt()
        {
            var compact = GetCompactPrompt();
            var actions = string.Join("\n", ActionsRegistry.All.Take(20).Select(a => $"  - {a.Action}: {a.Description}"));
            return compact + "\n\nKey actions:\n" + actions + "\n\n... (use api.list for full list)";
        }

        private static Task<UacfResponse> HandleLogs(JObject p)
        {
            var last = p["last"]?.Value<int>() ?? 20;
            var entries = RequestLog.GetLast(last);
            return Task.FromResult(UacfResponse.Success(new
            {
                entries = entries.Select(e => new
                {
                    timestamp = e.Timestamp,
                    action = e.Action,
                    ok = e.Ok,
                    duration = e.Duration,
                    error = e.ErrorCode
                }).ToArray()
            }, 0));
        }
    }
}
