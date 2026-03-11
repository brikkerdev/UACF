using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UACF.Config;
using UACF.Core;
using UACF.Models;

namespace UACF.Handlers
{
    public static class BatchHandler
    {
        public static void Register(ActionDispatcher dispatcher)
        {
            dispatcher.Register("batch", HandleBatch);
        }

        private static async Task<UacfResponse> HandleBatch(JObject p)
        {
                if (!UACFSettings.instance.EnableBatchEndpoint)
                    return UacfResponse.Fail("FORBIDDEN", "Batch endpoint is disabled", null, 0);

                var operations = p["operations"] as JArray;
                if (operations == null)
                    return UacfResponse.Fail("INVALID_REQUEST", "operations array is required", null, 0);

                var undoGroup = p["undoGroup"]?.ToString();
                var stopOnError = p["stopOnError"]?.Value<bool>() ?? true;

                var dispatcher = GetDispatcher();
                if (dispatcher == null)
                    return UacfResponse.Fail("INTERNAL_ERROR", "ActionDispatcher not available", null, 0);

                int undoGroupId = 0;
                if (!string.IsNullOrEmpty(undoGroup))
                {
                    Undo.SetCurrentGroupName(undoGroup);
                    undoGroupId = Undo.GetCurrentGroup();
                }

                var results = new List<object>();
                var allOk = true;

                foreach (var op in operations)
                {
                    var jo = op as JObject;
                    if (jo == null) continue;

                    var action = jo["action"]?.ToString();
                    var @params = jo["params"] as JObject ?? new JObject();

                    if (string.IsNullOrEmpty(action))
                    {
                        results.Add(new { ok = false, error = "action required" });
                        if (stopOnError) { allOk = false; break; }
                        continue;
                    }

                    var response = await dispatcher.DispatchAsync(action, @params);
                    results.Add(new
                    {
                        ok = response.Ok,
                        data = response.Data,
                        error = response.Error != null ? new { response.Error.Code, response.Error.Message } : null
                    });

                    if (!response.Ok && stopOnError)
                    {
                        allOk = false;
                        if (undoGroupId != 0)
                            Undo.RevertAllDownToGroup(undoGroupId);
                        break;
                    }
                }

                return UacfResponse.Success(new
                {
                    results,
                    undoGroup = undoGroup,
                    allSucceeded = allOk
                }, 0);
        }

        private static ActionDispatcher GetDispatcher() => UACFBootstrap.GetDispatcher();
    }
}
