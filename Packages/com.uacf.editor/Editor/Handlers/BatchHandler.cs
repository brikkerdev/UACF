using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UACF.Config;
using UACF.Core;
using UACF.Handlers;

namespace UACF.Handlers
{
    public static class BatchHandler
    {
        public static void Register(RequestRouter router)
        {
            router.Register("POST", "/api/batch/execute", HandleExecute);
        }

        private static async Task HandleExecute(RequestContext ctx)
        {
            if (!UACFSettings.instance.EnableBatchEndpoint)
            {
                ResponseHelper.InvalidRequest(ctx, "Batch endpoint is disabled");
                return;
            }

            var body = await ctx.ReadBodyAsync<JObject>();
            if (body?["operations"] == null)
            {
                ResponseHelper.InvalidRequest(ctx, "operations array is required");
                return;
            }

            var ops = body["operations"] as JArray;
            var stopOnError = body["stop_on_error"]?.Value<bool>() ?? true;

            var server = UACFBootstrap.GetServer();
            if (server == null || !server.IsRunning)
            {
                ResponseHelper.InternalError(ctx, "Server not running");
                return;
            }

            var port = server.Port;
            var baseUrl = $"http://127.0.0.1:{port}";
            var results = new List<object>();
            var httpClient = new HttpClient { Timeout = System.TimeSpan.FromSeconds(UACFSettings.instance.RequestTimeoutSeconds) };

            foreach (var op in ops)
            {
                var jo = op as JObject;
                if (jo == null) continue;

                var id = jo["id"]?.ToString() ?? "op";
                var endpoint = jo["endpoint"]?.ToString();
                var opBody = jo["body"];

                if (string.IsNullOrEmpty(endpoint))
                {
                    results.Add(new { id, success = false, error = "endpoint required" });
                    if (stopOnError) break;
                    continue;
                }

                var parts = endpoint.Trim().Split(new[] { ' ' }, 2);
                var method = parts.Length > 0 ? parts[0].ToUpperInvariant() : "POST";
                var path = parts.Length > 1 ? parts[1].Trim() : "";

                if (!path.StartsWith("/")) path = "/" + path;

                try
                {
                    var request = new HttpRequestMessage(new HttpMethod(method), baseUrl + path);
                    if (opBody != null && (method == "POST" || method == "PUT"))
                    {
                        request.Content = new StringContent(opBody.ToString(), Encoding.UTF8, "application/json");
                    }

                    var response = await httpClient.SendAsync(request);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    object data = null;
                    try
                    {
                        var json = JObject.Parse(responseBody);
                        data = json["data"];
                    }
                    catch { }

                    results.Add(new
                    {
                        id,
                        success = response.IsSuccessStatusCode,
                        status = (int)response.StatusCode,
                        data = data
                    });

                    if (stopOnError && !response.IsSuccessStatusCode)
                        break;
                }
                catch (System.Exception ex)
                {
                    results.Add(new { id, success = false, error = ex.Message });
                    if (stopOnError) break;
                }
            }

            var succeeded = 0;
            foreach (var r in results)
            {
                var dict = r as JObject;
                if (dict?["success"]?.Value<bool>() == true) succeeded++;
            }

            ctx.RespondOk(new
            {
                results = results,
                total = results.Count,
                succeeded = succeeded,
                failed = results.Count - succeeded
            });
        }
    }
}
