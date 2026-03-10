using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using Unity.Plastic.Newtonsoft.Json;
using UACF.Config;
using UACF.Core;
using UACF.Models;
using UACF.Services;

namespace UACF.Handlers
{
    public static class FileHandler
    {
        public static void Register(RequestRouter router)
        {
            router.Register("POST", "/api/file/write", HandleWrite);
            router.Register("GET", "/api/file/read", HandleRead);
        }

        private static async Task HandleWrite(RequestContext ctx)
        {
            var body = await ctx.ReadBodyAsync<FileWritePayload>();
            if (body == null || string.IsNullOrEmpty(body.Path))
            {
                ResponseHelper.InvalidRequest(ctx, "path is required");
                return;
            }

            var path = body.Path;
            if (!path.StartsWith("Assets/") && !path.StartsWith("Packages/"))
            {
                ResponseHelper.InvalidRequest(ctx, "path must be under Assets/ or Packages/");
                return;
            }

            var result = await MainThreadDispatcher.Enqueue(() =>
            {
                var projectRoot = Path.GetDirectoryName(UnityEngine.Application.dataPath);
                var fullPath = Path.Combine(projectRoot, path);
                var dir = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(fullPath, body.Content ?? "");

                if (body.AutoRefresh)
                    AssetDatabase.Refresh();

                CompileResult compileResult = null;
                if (body.WaitCompile && body.AutoRefresh)
                {
                    var svc = CompilationService.Instance;
                    var task = svc.RequestCompilationAsync(UACFSettings.instance.CompileTimeoutSeconds);
                    task.Wait();
                    compileResult = task.Result;
                }

                return new
                {
                    file_written = true,
                    path = path,
                    compiled = compileResult != null,
                    has_errors = compileResult?.HasErrors ?? false,
                    errors = compileResult?.Errors ?? new CompileError[0],
                    warnings = compileResult?.Warnings ?? new CompileError[0]
                };
            });

            ctx.RespondOk(result);
        }

        private static async Task HandleRead(RequestContext ctx)
        {
            var path = ctx.QueryParams.TryGetValue("path", out var p) ? p : null;
            if (string.IsNullOrEmpty(path))
            {
                ResponseHelper.InvalidRequest(ctx, "path query parameter is required");
                return;
            }

            var pathNorm = path.StartsWith("Assets/") || path.StartsWith("Packages/") ? path : "Assets/" + path;
            var projectRoot = Path.GetDirectoryName(UnityEngine.Application.dataPath);
            var fullPath = Path.Combine(projectRoot, pathNorm);

            var result = await MainThreadDispatcher.Enqueue(() =>
            {
                var exists = File.Exists(fullPath);
                var content = exists ? File.ReadAllText(fullPath) : "";
                var size = exists ? new FileInfo(fullPath).Length : 0;
                return new
                {
                    path = pathNorm,
                    content = content,
                    exists = exists,
                    size_bytes = size
                };
            });

            ctx.RespondOk(result);
        }

        private class FileWritePayload
        {
            [JsonProperty("path")] public string Path;
            [JsonProperty("content")] public string Content;
            [JsonProperty("auto_refresh")] public bool AutoRefresh = true;
            [JsonProperty("wait_compile")] public bool WaitCompile = false;
        }
    }
}
