using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using Unity.Plastic.Newtonsoft.Json;
using UACF.Config;
using UACF.Core;
using UACF.Models;
using UACF.Services;

namespace UACF.Handlers
{
    public static class CompileHandler
    {
        public static void Register(RequestRouter router)
        {
            router.Register("POST", "/api/compile/request", HandleRequest);
            router.Register("GET", "/api/compile/status", HandleStatus);
            router.Register("GET", "/api/compile/errors", HandleErrors);
        }

        private static async Task HandleRequest(RequestContext ctx)
        {
            if (EditorApplication.isCompiling)
            {
                ResponseHelper.ServerBusy(ctx, "Compilation already in progress", 5);
                return;
            }

            var body = await ctx.ReadBodyAsync<CompileRequestPayload>();
            var wait = body?.Wait ?? false;
            var timeout = body?.TimeoutSeconds ?? UACFSettings.instance.CompileTimeoutSeconds;

            if (!wait)
            {
                var result = await MainThreadDispatcher.Enqueue(() =>
                {
                    UnityEditor.AssetDatabase.Refresh();
                    return new { refreshed = true };
                });
                ctx.RespondOk(new { compiled = false, refreshed = true });
                return;
            }

            var compileResult = await (await MainThreadDispatcher.Enqueue(() =>
                CompilationService.Instance.RequestCompilationAsync(timeout)));

            ctx.RespondOk(new
            {
                compiled = compileResult.Compiled,
                has_errors = compileResult.HasErrors,
                error_count = compileResult.ErrorCount,
                warning_count = compileResult.WarningCount,
                errors = compileResult.Errors,
                warnings = compileResult.Warnings,
                duration_ms = compileResult.DurationMs
            });
        }

        private static async Task HandleStatus(RequestContext ctx)
        {
            var data = await MainThreadDispatcher.Enqueue(() =>
            {
                var svc = CompilationService.Instance;
                return new
                {
                    is_compiling = EditorApplication.isCompiling,
                    last_compile_success = svc.LastCompileSuccess,
                    last_compile_time = svc.LastCompileTime.ToString("o"),
                    last_error_count = svc.LastErrors.Count,
                    last_warning_count = svc.LastWarnings.Count
                };
            });
            ctx.RespondOk(data);
        }

        private static async Task HandleErrors(RequestContext ctx)
        {
            var severity = ctx.QueryParams.TryGetValue("severity", out var sev) ? sev : "all";
            var fileFilter = ctx.QueryParams.TryGetValue("file", out var f) ? f : null;

            var data = await MainThreadDispatcher.Enqueue(() =>
            {
                var svc = CompilationService.Instance;
                var errors = svc.LastErrors.AsEnumerable();
                var warnings = svc.LastWarnings.AsEnumerable();

                if (!string.IsNullOrEmpty(fileFilter))
                {
                    errors = errors.Where(e => e.File != null && e.File.Contains(fileFilter));
                    warnings = warnings.Where(w => w.File != null && w.File.Contains(fileFilter));
                }

                object[] resultList;
                if (severity == "error")
                    resultList = errors.Cast<object>().ToArray();
                else if (severity == "warning")
                    resultList = warnings.Cast<object>().ToArray();
                else
                    resultList = errors.Concat(warnings).Cast<object>().ToArray();

                return new
                {
                    errors = resultList,
                    total_errors = svc.LastErrors.Count,
                    total_warnings = svc.LastWarnings.Count
                };
            });
            ctx.RespondOk(data);
        }

        private class CompileRequestPayload
        {
            [JsonProperty("wait")] public bool Wait;
            [JsonProperty("timeout_seconds")] public int TimeoutSeconds;
        }
    }
}
