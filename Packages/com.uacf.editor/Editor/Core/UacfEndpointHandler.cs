using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UACF.Models;

namespace UACF.Core
{
    public class UacfEndpointHandler
    {
        private readonly ActionDispatcher _dispatcher;

        public UacfEndpointHandler(ActionDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public async Task HandleAsync(RequestContext ctx)
        {
            var sw = Stopwatch.StartNew();
            UacfResponse response;

            try
            {
                var body = await ctx.ReadBodyRawAsync();
                var request = string.IsNullOrWhiteSpace(body) ? null : JsonConvert.DeserializeObject<UacfRequest>(body);
                if (request == null || string.IsNullOrEmpty(request.Action))
                {
                    response = UacfResponse.Fail("INVALID_REQUEST", "Request body must contain { \"action\": \"...\", \"params\": {...} }",
                        "Example: { \"action\": \"api.list\" }", 0);
                }
                else
                {
                    var @params = request.Params as JObject ?? (request.Params != null ? JObject.FromObject(request.Params) : new JObject());
                    response = await _dispatcher.DispatchAsync(request.Action, @params);

                    RequestLog.Add(request.Action, response.Ok, response.Duration, response.Ok ? null : response.Error?.Code);
                }
            }
            catch (Exception ex)
            {
                response = UacfResponse.Fail("INTERNAL_ERROR", ex.Message, ex.StackTrace, 0);
                RequestLog.Add("(parse error)", false, 0, "INTERNAL_ERROR");
            }

            sw.Stop();
            if (response.Duration == 0)
                response.Duration = sw.Elapsed.TotalSeconds;

            ctx.Respond(200, response);
        }
    }
}
