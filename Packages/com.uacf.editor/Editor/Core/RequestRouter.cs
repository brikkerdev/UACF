using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UACF.Core
{
    public class RequestRouter
    {
        private readonly List<(string Method, string Pattern, Func<RequestContext, Task> Handler)> _routes = new List<(string, string, Func<RequestContext, Task>)>();
        private static readonly Regex ParamRegex = new Regex(@"\{(\w+)\}");

        public void Register(string method, string pattern, Func<RequestContext, Task> handler)
        {
            _routes.Add((method.ToUpperInvariant(), NormalizePattern(pattern), handler));
        }

        private static string NormalizePattern(string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return "/";
            pattern = pattern.TrimEnd('/');
            if (string.IsNullOrEmpty(pattern)) return "/";
            return pattern.StartsWith("/") ? pattern : "/" + pattern;
        }

        public async Task Route(HttpListenerContext context)
        {
            await Route(new RequestContext(context));
        }

        public async Task Route(RequestContext ctx)
        {
            var method = ctx.Method;
            var path = ctx.Path;

            foreach (var (routeMethod, routePattern, handler) in _routes)
            {
                if (routeMethod != method) continue;

                var pathParams = MatchPattern(routePattern, path);
                if (pathParams != null)
                {
                    ctx.SetPathParams(pathParams);
                    try
                    {
                        await handler(ctx);
                    }
                    catch (Exception ex)
                    {
                        ctx.RespondError(500, Models.ErrorCode.INTERNAL_ERROR, ex.Message, new { stack = ex.StackTrace }, 0);
                    }
                    return;
                }
            }

            ctx.RespondError(404, Models.ErrorCode.NOT_FOUND, "Endpoint not found", new { available_endpoints = GetAvailableEndpoints() }, 0);
        }

        private Dictionary<string, string> MatchPattern(string pattern, string path)
        {
            var paramNames = ParamRegex.Matches(pattern).Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            var escaped = Regex.Escape(pattern);
            var regexPattern = "^" + Regex.Replace(escaped, @"\\\{([^}]+)\\\}", "([^/]+)") + "$";
            var match = Regex.Match(path, regexPattern);
            if (!match.Success) return null;

            var result = new Dictionary<string, string>();
            for (int i = 0; i < paramNames.Count && i + 1 < match.Groups.Count; i++)
                result[paramNames[i]] = match.Groups[i + 1].Value;
            return result;
        }

        private object GetAvailableEndpoints()
        {
            return _routes.Select(r => $"{r.Method} {r.Pattern}").Distinct().OrderBy(x => x).ToList();
        }
    }
}
