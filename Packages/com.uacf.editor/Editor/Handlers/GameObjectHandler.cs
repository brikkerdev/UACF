using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UACF.Core;
using UACF.Models;
using UACF.Services;

namespace UACF.Handlers
{
    public static class GameObjectHandler
    {
        public static void Register(RequestRouter router)
        {
            router.Register("POST", "/api/gameobject/create", HandleCreate);
            router.Register("GET", "/api/gameobject/find", HandleFind);
            router.Register("PUT", "/api/gameobject/modify", HandleModify);
            router.Register("DELETE", "/api/gameobject/destroy", HandleDestroy);
            router.Register("POST", "/api/gameobject/set-parent", HandleSetParent);
            router.Register("POST", "/api/gameobject/duplicate", HandleDuplicate);
        }

        private static async Task HandleCreate(RequestContext ctx)
        {
            if (EditorApplication.isPlaying)
            {
                ResponseHelper.Conflict(ctx, "Exit Play Mode first");
                return;
            }

            var body = await ctx.ReadBodyAsync<JObject>();
            if (body == null)
            {
                ResponseHelper.InvalidRequest(ctx, "Request body required");
                return;
            }

            var payload = ParseCreatePayload(body);
            var result = await MainThreadDispatcher.Enqueue(() =>
            {
                var go = GameObjectService.Create(payload);
                if (go == null)
                    return (object)null;

                var componentsAdded = new List<string> { "Transform" };
                var fieldsFailed = new List<string>();

                if (payload.Components != null)
                {
                    foreach (var compPayload in payload.Components)
                    {
                        var type = TypeResolverService.Instance.Resolve(compPayload.Type);
                        if (type == null)
                        {
                            fieldsFailed.Add(compPayload.Type + " (type not found)");
                            continue;
                        }
                        var comp = ComponentService.AddComponent(go, compPayload.Type, compPayload.Fields);
                        if (comp != null)
                            componentsAdded.Add(compPayload.Type);
                    }
                }

                return new
                {
                    instance_id = go.GetInstanceID(),
                    name = go.name,
                    components_added = componentsAdded.ToArray(),
                    fields_set = componentsAdded.Count - 1,
                    fields_failed = fieldsFailed.ToArray()
                };
            });

            if (result == null)
            {
                ResponseHelper.InternalError(ctx, "Failed to create GameObject");
                return;
            }
            ctx.RespondOk(result);
        }

        private static GameObjectService.CreateGameObjectPayload ParseCreatePayload(JObject body)
        {
            var payload = new GameObjectService.CreateGameObjectPayload
            {
                Name = body["name"]?.ToString() ?? "GameObject",
                Tag = body["tag"]?.ToString(),
                LayerName = body["layer"]?.ToString(),
                Static = body["static"]?.Value<bool>() ?? false,
                Active = body["active"]?.Value<bool>() ?? true,
                WorldPositionStays = body["world_position_stays"]?.Value<bool>() ?? true
            };

            if (body["parent"] != null && body["parent"].Type != JTokenType.Null)
                payload.Parent = body["parent"].ToObject<Dictionary<string, object>>();

            if (body["transform"] != null)
            {
                var t = body["transform"] as JObject;
                payload.Transform = new GameObjectService.TransformPayload
                {
                    Position = t["position"]?.ToObject<Vector3Json>(),
                    Rotation = t["rotation"]?.ToObject<Vector3Json>(),
                    Scale = t["scale"]?.ToObject<Vector3Json>()
                };
            }

            if (body["components"] != null && body["components"] is JArray arr)
            {
                payload.Components = arr.Select(c =>
                {
                    var jo = c as JObject;
                    return new GameObjectService.ComponentPayload
                    {
                        Type = jo["type"]?.ToString(),
                        Fields = jo["fields"]?.ToObject<Dictionary<string, object>>()
                    };
                }).Where(x => !string.IsNullOrEmpty(x.Type)).ToList();
            }

            return payload;
        }

        private static async Task HandleFind(RequestContext ctx)
        {
            var payload = new GameObjectService.FindGameObjectPayload
            {
                InstanceId = ctx.QueryParams.TryGetValue("instance_id", out var id) && int.TryParse(id, out var i) ? i : (int?)null,
                Name = ctx.QueryParams.TryGetValue("name", out var n) ? n : null,
                Path = ctx.QueryParams.TryGetValue("path", out var p) ? p : null,
                Tag = ctx.QueryParams.TryGetValue("tag", out var t) ? t : null,
                Component = ctx.QueryParams.TryGetValue("component", out var c) ? c : null
            };

            var data = await MainThreadDispatcher.Enqueue(() =>
            {
                var objects = GameObjectService.Find(payload);
                return new
                {
                    objects = objects.Select(go => new
                    {
                        instance_id = go.GetInstanceID(),
                        name = go.name,
                        path = GetPath(go),
                        active_self = go.activeSelf,
                        active_hierarchy = go.activeInHierarchy,
                        tag = go.tag,
                        layer_name = UnityEngine.LayerMask.LayerToName(go.layer),
                        components = go.GetComponents<Component>().Where(x => x != null).Select(x => x.GetType().Name).ToArray()
                    }).ToArray(),
                    count = objects.Length
                };
            });
            ctx.RespondOk(data);
        }

        private static string GetPath(GameObject go)
        {
            var path = go.name;
            var p = go.transform.parent;
            while (p != null)
            {
                path = p.name + "/" + path;
                p = p.parent;
            }
            return "/" + path;
        }

        private static async Task HandleModify(RequestContext ctx)
        {
            if (EditorApplication.isPlaying)
            {
                ResponseHelper.Conflict(ctx, "Exit Play Mode first");
                return;
            }

            var body = await ctx.ReadBodyAsync<JObject>();
            if (body?["target"] == null || body["set"] == null)
            {
                ResponseHelper.InvalidRequest(ctx, "target and set are required");
                return;
            }

            var target = body["target"].ToObject<Dictionary<string, object>>();
            var set = body["set"] as JObject;

            var result = await MainThreadDispatcher.Enqueue(() =>
            {
                var go = GameObjectService.FindByTarget(target);
                if (go == null) return (object)null;

                var payload = new GameObjectService.ModifyGameObjectPayload
                {
                    Name = set["name"]?.ToString(),
                    Active = set["active"]?.Value<bool?>(),
                    Tag = set["tag"]?.ToString(),
                    LayerName = set["layer"]?.ToString(),
                    Static = set["static"]?.Value<bool?>()
                };
                if (set["transform"] != null)
                {
                    var t = set["transform"] as JObject;
                    payload.Transform = new GameObjectService.TransformPayload
                    {
                        Position = t["position"]?.ToObject<Vector3Json>(),
                        Rotation = t["rotation"]?.ToObject<Vector3Json>(),
                        Scale = t["scale"]?.ToObject<Vector3Json>()
                    };
                }
                GameObjectService.Modify(go, payload);
                return new { modified = true };
            });

            if (result == null)
            {
                ResponseHelper.NotFound(ctx, "GameObject not found");
                return;
            }
            ctx.RespondOk(result);
        }

        private static async Task HandleDestroy(RequestContext ctx)
        {
            if (EditorApplication.isPlaying)
            {
                ResponseHelper.Conflict(ctx, "Exit Play Mode first");
                return;
            }

            var body = await ctx.ReadBodyAsync<JObject>();
            if (body?["target"] == null)
            {
                ResponseHelper.InvalidRequest(ctx, "target is required");
                return;
            }

            var target = body["target"].ToObject<Dictionary<string, object>>();
            var destroyChildren = body["destroy_children"]?.Value<bool>() ?? true;

            var result = await MainThreadDispatcher.Enqueue(() =>
            {
                var go = GameObjectService.FindByTarget(target);
                if (go == null) return false;
                GameObjectService.Destroy(go, destroyChildren);
                return true;
            });

            if (!result)
            {
                ResponseHelper.NotFound(ctx, "GameObject not found");
                return;
            }
            ctx.RespondOk(new { destroyed = true });
        }

        private static async Task HandleSetParent(RequestContext ctx)
        {
            if (EditorApplication.isPlaying)
            {
                ResponseHelper.Conflict(ctx, "Exit Play Mode first");
                return;
            }

            var body = await ctx.ReadBodyAsync<JObject>();
            if (body?["target"] == null)
            {
                ResponseHelper.InvalidRequest(ctx, "target is required");
                return;
            }

            var target = body["target"].ToObject<Dictionary<string, object>>();
            object parent = body["parent"]?.Type == JTokenType.Null ? null : body["parent"]?.ToObject<Dictionary<string, object>>();
            var worldPositionStays = body["world_position_stays"]?.Value<bool>() ?? true;

            var result = await MainThreadDispatcher.Enqueue(() =>
            {
                var go = GameObjectService.FindByTarget(target);
                if (go == null) return false;
                GameObjectService.SetParent(go, parent, worldPositionStays);
                return true;
            });

            if (!result)
            {
                ResponseHelper.NotFound(ctx, "GameObject not found");
                return;
            }
            ctx.RespondOk(new { success = true });
        }

        private static async Task HandleDuplicate(RequestContext ctx)
        {
            if (EditorApplication.isPlaying)
            {
                ResponseHelper.Conflict(ctx, "Exit Play Mode first");
                return;
            }

            var body = await ctx.ReadBodyAsync<JObject>();
            if (body?["target"] == null)
            {
                ResponseHelper.InvalidRequest(ctx, "target is required");
                return;
            }

            var target = body["target"].ToObject<Dictionary<string, object>>();
            var newName = body["new_name"]?.ToString();
            Vector3? offset = null;
            if (body["offset"] != null)
            {
                var o = body["offset"] as JObject;
                if (o != null)
                    offset = new Vector3(o["x"]?.Value<float>() ?? 0, o["y"]?.Value<float>() ?? 0, o["z"]?.Value<float>() ?? 0);
            }

            var result = await MainThreadDispatcher.Enqueue(() =>
            {
                var go = GameObjectService.FindByTarget(target);
                if (go == null) return (object)null;
                var dup = GameObjectService.Duplicate(go, newName, offset);
                return dup != null ? new { instance_id = dup.GetInstanceID(), name = dup.name } : null;
            });

            if (result == null)
            {
                ResponseHelper.NotFound(ctx, "GameObject not found");
                return;
            }
            ctx.RespondOk(result);
        }
    }
}
