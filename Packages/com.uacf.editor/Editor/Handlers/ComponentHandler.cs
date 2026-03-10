using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UACF.Core;
using UACF.Models;
using UACF.Services;

namespace UACF.Handlers
{
    internal class TypeNotFoundException : System.Exception
    {
        public string TypeName { get; }
        public string[] Suggestions { get; }
        public TypeNotFoundException(string typeName, string[] suggestions) : base($"Type '{typeName}' not found")
        {
            TypeName = typeName;
            Suggestions = suggestions ?? new string[0];
        }
    }

    public static class ComponentHandler
    {
        public static void Register(RequestRouter router)
        {
            router.Register("POST", "/api/component/add", HandleAdd);
            router.Register("GET", "/api/component/get", HandleGet);
            router.Register("PUT", "/api/component/set-fields", HandleSetFields);
            router.Register("DELETE", "/api/component/remove", HandleRemove);
            router.Register("GET", "/api/component/list-types", HandleListTypes);
        }

        private static async Task HandleAdd(RequestContext ctx)
        {
            if (EditorApplication.isPlaying)
            {
                ResponseHelper.Conflict(ctx, "Exit Play Mode first");
                return;
            }

            var body = await ctx.ReadBodyAsync<JObject>();
            if (body?["target"] == null || body["component_type"] == null)
            {
                ResponseHelper.InvalidRequest(ctx, "target and component_type are required");
                return;
            }

            var target = body["target"].ToObject<Dictionary<string, object>>();
            var componentType = body["component_type"]?.ToString();
            var fields = body["fields"]?.ToObject<Dictionary<string, object>>();

            try
            {
                var result = await MainThreadDispatcher.Enqueue(() =>
                {
                    var go = GameObjectService.FindByTarget(target);
                    if (go == null) return (object)null;

                    var type = TypeResolverService.Instance.Resolve(componentType);
                    if (type == null)
                    {
                        var suggestions = TypeResolverService.Instance.GetSuggestions(componentType);
                        throw new TypeNotFoundException(componentType, suggestions);
                    }

                    var comp = ComponentService.AddComponent(go, componentType, fields);
                    return comp != null ? new { component_added = componentType } : (object)null;
                });

                if (result == null)
            {
                    ResponseHelper.NotFound(ctx, "GameObject not found");
                    return;
                }
                ctx.RespondOk(result);
            }
            catch (TypeNotFoundException ex)
            {
                ResponseHelper.Unprocessable(ctx, Models.ErrorCode.TYPE_NOT_FOUND, $"Type '{ex.TypeName}' not found", new { suggestions = ex.Suggestions });
            }
        }

        private static async Task HandleGet(RequestContext ctx)
        {
            var instanceIdStr = ctx.QueryParams.TryGetValue("instance_id", out var i) ? i : null;
            var component = ctx.QueryParams.TryGetValue("component", out var c) ? c : null;
            var index = ctx.QueryParams.TryGetValue("index", out var idx) && int.TryParse(idx, out var ix) ? ix : 0;

            if (string.IsNullOrEmpty(instanceIdStr) || string.IsNullOrEmpty(component))
            {
                ResponseHelper.InvalidRequest(ctx, "instance_id and component are required");
                return;
            }

            if (!int.TryParse(instanceIdStr, out var instanceId))
            {
                ResponseHelper.InvalidRequest(ctx, "instance_id must be a number");
                return;
            }

            var data = await MainThreadDispatcher.Enqueue(() =>
            {
#pragma warning disable CS0618
                var go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
#pragma warning restore CS0618
                if (go == null) return (object)null;

                var comp = ComponentService.GetComponent(go, component, index);
                if (comp == null) return (object)null;

                var so = new SerializedObject(comp);
                var fields = new Dictionary<string, object>();
                var it = so.GetIterator();
                it.Next(true);
                while (it.Next(false))
                {
                    if (it.propertyType == SerializedPropertyType.Generic && it.depth > 2) continue;
                    var val = GetPropertyValue(it);
                    if (val != null)
                        fields[it.name] = val;
                }

                return new
                {
                    component_type = comp.GetType().Name,
                    game_object = go.name,
                    instance_id = comp.GetInstanceID(),
                    fields = fields
                };
            });

            if (data == null)
            {
                ResponseHelper.NotFound(ctx, "Component or GameObject not found");
                return;
            }
            ctx.RespondOk(data);
        }

        private static object GetPropertyValue(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer: return prop.intValue;
                case SerializedPropertyType.Float: return prop.floatValue;
                case SerializedPropertyType.Boolean: return prop.boolValue;
                case SerializedPropertyType.String: return prop.stringValue;
                case SerializedPropertyType.Vector2: return new { x = prop.vector2Value.x, y = prop.vector2Value.y };
                case SerializedPropertyType.Vector3: return new { x = prop.vector3Value.x, y = prop.vector3Value.y, z = prop.vector3Value.z };
                case SerializedPropertyType.ObjectReference:
                    var obj = prop.objectReferenceValue;
                    return obj != null ? new { value = obj.name, type = obj.GetType().Name } : null;
                case SerializedPropertyType.Enum: return prop.enumNames[prop.enumValueIndex];
                default: return null;
            }
        }

        private static async Task HandleSetFields(RequestContext ctx)
        {
            if (EditorApplication.isPlaying)
            {
                ResponseHelper.Conflict(ctx, "Exit Play Mode first");
                return;
            }

            var body = await ctx.ReadBodyAsync<JObject>();
            if (body?["target"] == null || body["component"] == null || body["fields"] == null)
            {
                ResponseHelper.InvalidRequest(ctx, "target, component, and fields are required");
                return;
            }

            var target = body["target"].ToObject<Dictionary<string, object>>();
            var component = body["component"]?.ToString();
            var index = body["index"]?.Value<int>() ?? 0;
            var fields = body["fields"]?.ToObject<Dictionary<string, object>>();

            var result = await MainThreadDispatcher.Enqueue(() =>
            {
                var go = GameObjectService.FindByTarget(target);
                if (go == null) return (object)null;

                var comp = ComponentService.GetComponent(go, component, index);
                if (comp == null) return (object)null;

                ComponentService.SetFields(comp, fields);
                return new
                {
                    fields_set = fields.Select(f => new { name = f.Key, value = f.Value, status = "ok" }).ToArray(),
                    fields_failed = new object[0],
                    total_set = fields.Count,
                    total_failed = 0
                };
            });

            if (result == null)
            {
                ResponseHelper.NotFound(ctx, "GameObject or component not found");
                return;
            }
            ctx.RespondOk(result);
        }

        private static async Task HandleRemove(RequestContext ctx)
        {
            if (EditorApplication.isPlaying)
            {
                ResponseHelper.Conflict(ctx, "Exit Play Mode first");
                return;
            }

            var body = await ctx.ReadBodyAsync<JObject>();
            if (body?["target"] == null || body["component"] == null)
            {
                ResponseHelper.InvalidRequest(ctx, "target and component are required");
                return;
            }

            var target = body["target"].ToObject<Dictionary<string, object>>();
            var component = body["component"]?.ToString();
            var index = body["index"]?.Value<int>() ?? 0;

            var result = await MainThreadDispatcher.Enqueue(() =>
            {
                var go = GameObjectService.FindByTarget(target);
                if (go == null) return false;

                var comp = ComponentService.GetComponent(go, component, index);
                if (comp == null || comp is Transform) return false;

                ComponentService.RemoveComponent(comp);
                return true;
            });

            if (!result)
            {
                ResponseHelper.NotFound(ctx, "GameObject or component not found");
                return;
            }
            ctx.RespondOk(new { removed = true });
        }

        private static async Task HandleListTypes(RequestContext ctx)
        {
            var filter = ctx.QueryParams.TryGetValue("filter", out var f) ? f : null;
            var category = ctx.QueryParams.TryGetValue("category", out var c) ? c : "all";

            var data = await MainThreadDispatcher.Enqueue(() =>
            {
                var types = new List<ComponentTypeInfo>();
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (var t in asm.GetTypes().Where(x => typeof(Component).IsAssignableFrom(x) && !x.IsAbstract))
                        {
                            if (!string.IsNullOrEmpty(filter) && !t.Name.Contains(filter)) continue;
                            var cat = t.Namespace?.StartsWith("UnityEngine") == true ? "unity" : "custom";
                            if (category != "all" && category != cat) continue;
                            types.Add(new ComponentTypeInfo
                            {
                                Name = t.Name,
                                FullName = t.FullName ?? t.Name,
                                Category = cat
                            });
                        }
                    }
                    catch { }
                }
                return new { types = types.Take(500).ToArray() };
            });
            ctx.RespondOk(data);
        }
    }
}
