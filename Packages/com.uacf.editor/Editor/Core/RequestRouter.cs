using System;
using System.Threading.Tasks;
using UACF.Models;

namespace UACF.Core
{
    public class RequestRouter
    {
        private readonly UacfEndpointHandler _uacfHandler;

        public RequestRouter(UacfEndpointHandler uacfHandler)
        {
            _uacfHandler = uacfHandler;
        }

        public async Task Route(RequestContext ctx)
        {
            if (ctx.Method != "POST" || !IsUacfPath(ctx.Path))
            {
                ctx.Respond(404, new
                {
                    ok = false,
                    error = new
                    {
                        code = "NOT_FOUND",
                        message = "Endpoint not found. Use POST /uacf with JSON body: { \"action\": \"api.list\", \"params\": {} }",
                        suggestion = "All UACF requests go to POST /uacf"
                    },
                    duration = 0.0
                });
                return;
            }

            await _uacfHandler.HandleAsync(ctx);
        }

        private static bool IsUacfPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            var p = path.TrimEnd('/');
            return p == "/uacf" || p == "uacf";
        }
    }
}
