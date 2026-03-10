using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using Unity.Plastic.Newtonsoft.Json;
using UACF.Core;
using UACF.Services;

namespace UACF.Handlers
{
    public static class AssetsHandler
    {
        public static void Register(RequestRouter router)
        {
            router.Register("POST", "/api/assets/refresh", HandleRefresh);
            router.Register("GET", "/api/assets/find", HandleFind);
            router.Register("POST", "/api/assets/create-folder", HandleCreateFolder);
            router.Register("DELETE", "/api/assets/delete", HandleDelete);
        }

        private static async Task HandleRefresh(RequestContext ctx)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await MainThreadDispatcher.Enqueue(() =>
            {
                AssetDatabase.Refresh();
            });
            sw.Stop();
            ctx.RespondOk(new { refreshed = true, duration_ms = sw.ElapsedMilliseconds });
        }

        private static async Task HandleFind(RequestContext ctx)
        {
            var filter = ctx.QueryParams.TryGetValue("filter", out var f) ? f : "t:Object";
            var path = ctx.QueryParams.TryGetValue("path", out var p) ? p : null;

            var data = await MainThreadDispatcher.Enqueue(() =>
            {
                var assets = AssetDatabaseService.FindAssetsWithDetails(filter, path);
                return new
                {
                    assets = assets.Select(a => new { guid = a.guid, path = a.path, type = a.type }).ToArray(),
                    count = assets.Length
                };
            });
            ctx.RespondOk(data);
        }

        private static async Task HandleCreateFolder(RequestContext ctx)
        {
            var body = await ctx.ReadBodyAsync<CreateFolderPayload>();
            if (body == null || string.IsNullOrEmpty(body.Path))
            {
                ResponseHelper.InvalidRequest(ctx, "path is required");
                return;
            }

            var path = body.Path.StartsWith("Assets/") ? body.Path : "Assets/" + body.Path;
            var created = await MainThreadDispatcher.Enqueue(() => AssetDatabaseService.CreateFolder(path));
            ctx.RespondOk(new { created = created, path = path });
        }

        private static async Task HandleDelete(RequestContext ctx)
        {
            var body = await ctx.ReadBodyAsync<DeleteAssetPayload>();
            if (body == null || string.IsNullOrEmpty(body.Path))
            {
                ResponseHelper.InvalidRequest(ctx, "path is required");
                return;
            }

            var path = body.Path.StartsWith("Assets/") ? body.Path : "Assets/" + body.Path;
            var deleted = await MainThreadDispatcher.Enqueue(() => AssetDatabaseService.DeleteAsset(path));
            ctx.RespondOk(new { deleted = deleted, path = path });
        }

        private class CreateFolderPayload
        {
            [JsonProperty("path")] public string Path;
        }

        private class DeleteAssetPayload
        {
            [JsonProperty("path")] public string Path;
        }
    }
}
