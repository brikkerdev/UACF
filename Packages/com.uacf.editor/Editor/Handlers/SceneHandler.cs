using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine.SceneManagement;
using UACF.Core;
using UACF.Models;
using UACF.Services;

namespace UACF.Handlers
{
    public static class SceneHandler
    {
        public static void Register(RequestRouter router)
        {
            router.Register("GET", "/api/scene/list", HandleList);
            router.Register("POST", "/api/scene/open", HandleOpen);
            router.Register("POST", "/api/scene/save", HandleSave);
            router.Register("POST", "/api/scene/new", HandleNew);
            router.Register("GET", "/api/scene/hierarchy", HandleHierarchy);
        }

        private static async Task HandleList(RequestContext ctx)
        {
            var data = await MainThreadDispatcher.Enqueue(() =>
            {
                var scenes = SceneService.GetLoadedScenes();
                return new { scenes = scenes };
            });
            ctx.RespondOk(data);
        }

        private static async Task HandleOpen(RequestContext ctx)
        {
            if (EditorApplication.isPlaying)
            {
                ResponseHelper.Conflict(ctx, "Exit Play Mode first");
                return;
            }

            var body = await ctx.ReadBodyAsync<SceneOpenPayload>();
            if (body == null || string.IsNullOrEmpty(body.Path))
            {
                ResponseHelper.InvalidRequest(ctx, "path is required");
                return;
            }

            var ok = await MainThreadDispatcher.Enqueue(() => SceneService.OpenScene(body.Path, body.Mode ?? "Single"));
            ctx.RespondOk(new { opened = ok, path = body.Path });
        }

        private static async Task HandleSave(RequestContext ctx)
        {
            if (EditorApplication.isPlaying)
            {
                ResponseHelper.Conflict(ctx, "Exit Play Mode first");
                return;
            }

            var body = await ctx.ReadBodyAsync<SceneSavePayload>();
            var path = body?.Path;
            var ok = await MainThreadDispatcher.Enqueue(() => SceneService.SaveScene(path));
            ctx.RespondOk(new { saved = ok });
        }

        private static async Task HandleNew(RequestContext ctx)
        {
            if (EditorApplication.isPlaying)
            {
                ResponseHelper.Conflict(ctx, "Exit Play Mode first");
                return;
            }

            var body = await ctx.ReadBodyAsync<SceneNewPayload>();
            if (body == null || string.IsNullOrEmpty(body.Path))
            {
                ResponseHelper.InvalidRequest(ctx, "path is required");
                return;
            }

            var ok = await MainThreadDispatcher.Enqueue(() => SceneService.NewScene(body.Path, body.Template ?? "default"));
            ctx.RespondOk(new { created = ok, path = body.Path });
        }

        private static async Task HandleHierarchy(RequestContext ctx)
        {
            var depthStr = ctx.QueryParams.TryGetValue("depth", out var d) ? d : "-1";
            var includeComponents = ctx.QueryParams.TryGetValue("include_components", out var ic) && ic == "true";
            var scenePath = ctx.QueryParams.TryGetValue("scene", out var sp) ? sp : null;

            var data = await MainThreadDispatcher.Enqueue(() =>
            {
                var scene = !string.IsNullOrEmpty(scenePath)
                    ? SceneManager.GetSceneByPath(scenePath)
                    : SceneManager.GetActiveScene();
                if (!scene.IsValid())
                    return null;

                int depth = int.TryParse(depthStr, out var dep) ? dep : -1;
                var objects = SerializationService.SerializeHierarchy(scene, depth, includeComponents);
                var flat = FlattenHierarchy(objects);
                return new
                {
                    scene = scene.name,
                    objects = objects,
                    total_count = flat.Length
                };
            });

            if (data == null)
            {
                ResponseHelper.Error(ctx, 404, Models.ErrorCode.SCENE_NOT_LOADED, "Scene not found");
                return;
            }
            ctx.RespondOk(data);
        }

        private static GameObjectInfo[] FlattenHierarchy(GameObjectInfo[] arr)
        {
            var list = new System.Collections.Generic.List<GameObjectInfo>();
            void Add(GameObjectInfo[] a)
            {
                foreach (var o in a)
                {
                    list.Add(o);
                    if (o.Children != null && o.Children.Length > 0)
                        Add(o.Children);
                }
            }
            Add(arr);
            return list.ToArray();
        }

        private class SceneOpenPayload
        {
            [JsonProperty("path")] public string Path;
            [JsonProperty("mode")] public string Mode;
        }

        private class SceneSavePayload
        {
            [JsonProperty("path")] public string Path;
        }

        private class SceneNewPayload
        {
            [JsonProperty("path")] public string Path;
            [JsonProperty("template")] public string Template;
        }
    }
}
