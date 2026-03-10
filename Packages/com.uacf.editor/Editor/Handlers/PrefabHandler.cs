using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UACF.Core;
using UACF.Models;
using UACF.Services;

namespace UACF.Handlers
{
    public static class PrefabHandler
    {
        public static void Register(RequestRouter router)
        {
            router.Register("POST", "/api/prefab/create", HandleCreate);
            router.Register("POST", "/api/prefab/instantiate", HandleInstantiate);
            router.Register("PUT", "/api/prefab/modify", HandleModify);
            router.Register("POST", "/api/prefab/apply-overrides", HandleApplyOverrides);
        }

        private static async Task HandleCreate(RequestContext ctx)
        {
            if (EditorApplication.isPlaying)
            {
                ResponseHelper.Conflict(ctx, "Exit Play Mode first");
                return;
            }

            var body = await ctx.ReadBodyAsync<JObject>();
            if (body?["source"] == null || body["path"] == null)
            {
                ResponseHelper.InvalidRequest(ctx, "source and path are required");
                return;
            }

            var target = body["source"].ToObject<Dictionary<string, object>>();
            var path = body["path"]?.ToString();
            var keepConnection = body["keep_connection"]?.Value<bool>() ?? true;

            var result = await MainThreadDispatcher.Enqueue(() =>
            {
                var go = GameObjectService.FindByTarget(target);
                if (go == null) return (object)null;
                var created = PrefabService.CreatePrefab(go, path, keepConnection);
                return created ? new { path = path, success = true } : (object)null;
            });

            if (result == null)
            {
                ResponseHelper.NotFound(ctx, "Source GameObject not found");
                return;
            }
            ctx.RespondOk(result);
        }

        private static async Task HandleInstantiate(RequestContext ctx)
        {
            if (EditorApplication.isPlaying)
            {
                ResponseHelper.Conflict(ctx, "Exit Play Mode first");
                return;
            }

            var body = await ctx.ReadBodyAsync<JObject>();
            if (body?["prefab_path"] == null)
            {
                ResponseHelper.InvalidRequest(ctx, "prefab_path is required");
                return;
            }

            var prefabPath = body["prefab_path"]?.ToString();
            var name = body["name"]?.ToString();
            object parentObj = body["parent"]?.Type == JTokenType.Null ? null : body["parent"]?.ToObject<Dictionary<string, object>>();
            Vector3? position = body["position"] != null ? ParseVector3(body["position"] as JObject) : null;
            Quaternion? rotation = body["rotation"] != null ? ParseQuaternion(body["rotation"] as JObject) : null;
            var overrides = body["component_overrides"]?.ToObject<Dictionary<string, Dictionary<string, object>>>();

            var result = await MainThreadDispatcher.Enqueue(() =>
            {
                Transform parent = null;
                if (parentObj != null)
                {
                    var pGo = GameObjectService.FindByTarget(parentObj as Dictionary<string, object>);
                    parent = pGo?.transform;
                }
                var instance = PrefabService.InstantiatePrefab(prefabPath, name, parent, position, rotation, overrides);
                return instance != null ? new { instance_id = instance.GetInstanceID(), name = instance.name } : (object)null;
            });

            if (result == null)
            {
                ResponseHelper.NotFound(ctx, "Prefab not found or failed to instantiate");
                return;
            }
            ctx.RespondOk(result);
        }

        private static async Task HandleModify(RequestContext ctx)
        {
            if (EditorApplication.isPlaying)
            {
                ResponseHelper.Conflict(ctx, "Exit Play Mode first");
                return;
            }

            var body = await ctx.ReadBodyAsync<JObject>();
            if (body?["prefab_path"] == null || body["operations"] == null)
            {
                ResponseHelper.InvalidRequest(ctx, "prefab_path and operations are required");
                return;
            }

            var prefabPath = body["prefab_path"]?.ToString();
            var ops = new List<PrefabService.PrefabOperation>();
            foreach (var op in body["operations"] as JArray ?? new JArray())
            {
                var jo = op as JObject;
                if (jo == null) continue;
                ops.Add(new PrefabService.PrefabOperation
                {
                    Action = jo["action"]?.ToString(),
                    TargetPath = jo["target_path"]?.ToString(),
                    Name = jo["name"]?.ToString(),
                    Component = jo["component"]?.ToString(),
                    Fields = jo["fields"]?.ToObject<Dictionary<string, object>>(),
                    Transform = jo["transform"] != null ? new TransformInfo
                    {
                        Position = jo["transform"]?["position"]?.ToObject<Vector3Json>()
                    } : null
                });
            }

            var result = await MainThreadDispatcher.Enqueue(() => PrefabService.ModifyPrefab(prefabPath, ops));
            ctx.RespondOk(new { success = result });
        }

        private static async Task HandleApplyOverrides(RequestContext ctx)
        {
            if (EditorApplication.isPlaying)
            {
                ResponseHelper.Conflict(ctx, "Exit Play Mode first");
                return;
            }

            var body = await ctx.ReadBodyAsync<JObject>();
            if (body?["instance"] == null)
            {
                ResponseHelper.InvalidRequest(ctx, "instance is required");
                return;
            }

            var target = body["instance"].ToObject<Dictionary<string, object>>();
            var applyAll = body["apply_all"]?.Value<bool>() ?? true;

            var result = await MainThreadDispatcher.Enqueue(() =>
            {
                var go = GameObjectService.FindByTarget(target);
                if (go == null) return false;
                return PrefabService.ApplyOverrides(go, applyAll);
            });

            if (!result)
            {
                ResponseHelper.NotFound(ctx, "Instance not found or not a prefab instance");
                return;
            }
            ctx.RespondOk(new { applied = true });
        }

        private static Vector3? ParseVector3(JObject o)
        {
            if (o == null) return null;
            return new Vector3(o["x"]?.Value<float>() ?? 0, o["y"]?.Value<float>() ?? 0, o["z"]?.Value<float>() ?? 0);
        }

        private static Quaternion? ParseQuaternion(JObject o)
        {
            if (o == null) return null;
            if (o["w"] != null)
                return new Quaternion(o["x"]?.Value<float>() ?? 0, o["y"]?.Value<float>() ?? 0, o["z"]?.Value<float>() ?? 0, o["w"]?.Value<float>() ?? 1);
            return Quaternion.Euler(o["x"]?.Value<float>() ?? 0, o["y"]?.Value<float>() ?? 0, o["z"]?.Value<float>() ?? 0);
        }
    }
}
