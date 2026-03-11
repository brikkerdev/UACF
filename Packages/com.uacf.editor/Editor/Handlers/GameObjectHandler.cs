using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UACF.Core;
using UACF.Models;
using UACF.Services;

namespace UACF.Handlers
{
    public static class GameObjectHandler
    {
        public static void Register(ActionDispatcher dispatcher)
        {
            dispatcher.Register("scene.object.create", HandleCreate);
            dispatcher.Register("scene.object.find", HandleFind);
            dispatcher.Register("scene.object.details", HandleDetails);
            dispatcher.Register("scene.object.set", HandleSet);
            dispatcher.Register("scene.object.destroy", HandleDestroy);
            dispatcher.Register("scene.object.duplicate", HandleDuplicate);
            dispatcher.Register("scene.object.createPrimitive", HandleCreatePrimitive);
        }

        private static Task<UacfResponse> HandleCreate(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                if (EditorApplication.isPlaying)
                    return UacfResponse.Fail("CONFLICT", "Exit Play Mode first", null, 0);

                var payload = ParseCreatePayload(p);
                var go = GameObjectService.Create(payload);
                if (go == null)
                    return UacfResponse.Fail("INTERNAL_ERROR", "Failed to create GameObject", null, 0);

                var componentsAdded = new List<string> { "Transform" };
                if (payload.Components != null)
                {
                    foreach (var comp in payload.Components)
                    {
                        var type = TypeResolverService.Instance.Resolve(comp.Type);
                        if (type != null)
                        {
                            var c = ComponentService.AddComponent(go, comp.Type, comp.Fields);
                            if (c != null) componentsAdded.Add(comp.Type);
                        }
                    }
                }

                return UacfResponse.Success(new
                {
                    instanceId = go.GetInstanceID(),
                    name = go.name,
                    componentsAdded = componentsAdded.ToArray()
                }, 0);
            });
        }

        private static GameObjectService.CreateGameObjectPayload ParseCreatePayload(JObject p)
        {
            var payload = new GameObjectService.CreateGameObjectPayload
            {
                Name = p["name"]?.ToString() ?? "GameObject",
                Tag = p["tag"]?.ToString(),
                LayerName = p["layer"]?.ToString(),
                Static = p["static"]?.Value<bool>() ?? false,
                Active = p["active"]?.Value<bool>() ?? true
            };

            var parent = p["parent"]?.ToString();
            if (!string.IsNullOrEmpty(parent))
                payload.Parent = new Dictionary<string, object> { ["name"] = parent };

            var pos = p["position"] as JArray;
            var rot = p["rotation"] as JArray;
            var scale = p["scale"] as JArray;
            if (pos != null || rot != null || scale != null)
            {
                payload.Transform = new GameObjectService.TransformPayload
                {
                    Position = pos != null ? ToVector3Json(pos) : null,
                    Rotation = rot != null ? ToVector3Json(rot) : null,
                    Scale = scale != null ? ToVector3Json(scale) : null
                };
            }

            var comps = p["components"] as JArray;
            if (comps != null)
            {
                payload.Components = comps.Select(c =>
                {
                    var jo = c as JObject;
                    var type = jo?["type"]?.ToString();
                    if (string.IsNullOrEmpty(type)) return null;
                    var props = jo["properties"]?.ToObject<Dictionary<string, object>>();
                    return new GameObjectService.ComponentPayload { Type = type, Fields = props };
                }).Where(x => x != null).ToList();
            }

            return payload;
        }

        private static Vector3Json ToVector3Json(JArray arr)
        {
            if (arr == null || arr.Count < 3) return null;
            return new Vector3Json
            {
                X = arr[0].Value<float>(),
                Y = arr[1].Value<float>(),
                Z = arr[2].Value<float>()
            };
        }

        private static Task<UacfResponse> HandleFind(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var payload = new GameObjectService.FindGameObjectPayload
                {
                    InstanceId = p["instanceId"]?.Value<int?>(),
                    Name = p["name"]?.ToString(),
                    Path = p["path"]?.ToString(),
                    Tag = p["tag"]?.ToString(),
                    Component = p["component"]?.ToString()
                };

                var objects = GameObjectService.Find(payload);
                return UacfResponse.Success(new
                {
                    objects = objects.Select(go => new
                    {
                        instanceId = go.GetInstanceID(),
                        name = go.name,
                        path = GetPath(go),
                        active = go.activeInHierarchy,
                        tag = go.tag,
                        layer = LayerMask.LayerToName(go.layer),
                        components = go.GetComponents<Component>().Where(x => x != null).Select(x => x.GetType().Name).ToArray()
                    }).ToArray(),
                    count = objects.Length
                }, 0);
            });
        }

        private static Task<UacfResponse> HandleDetails(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var target = ResolveTarget(p);
                if (target == null)
                    return UacfResponse.Fail("OBJECT_NOT_FOUND", "Object not found", "Use scene.object.find to list objects", 0);

                var comps = target.GetComponents<Component>();
                var components = comps.Where(c => c != null).Select(c =>
                {
                    var so = new SerializedObject(c);
                    var props = new Dictionary<string, object>();
                    var it = so.GetIterator();
                    it.Next(true);
                    while (it.Next(false))
                    {
                        if (it.propertyType == SerializedPropertyType.Generic && it.depth > 2) continue;
                        var val = GetPropertyValue(it);
                        if (val != null) props[it.name] = val;
                    }
                    return new { type = c.GetType().Name, enabled = (c as Behaviour)?.enabled ?? true, properties = props };
                }).ToArray();

                return UacfResponse.Success(new
                {
                    instanceId = target.GetInstanceID(),
                    name = target.name,
                    path = GetPath(target),
                    active = target.activeSelf,
                    tag = target.tag,
                    layer = LayerMask.LayerToName(target.layer),
                    components
                }, 0);
            });
        }

        private static object GetPropertyValue(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer: return prop.intValue;
                case SerializedPropertyType.Float: return prop.floatValue;
                case SerializedPropertyType.Boolean: return prop.boolValue;
                case SerializedPropertyType.String: return prop.stringValue;
                case SerializedPropertyType.Vector2: return new[] { prop.vector2Value.x, prop.vector2Value.y };
                case SerializedPropertyType.Vector3: return new[] { prop.vector3Value.x, prop.vector3Value.y, prop.vector3Value.z };
                case SerializedPropertyType.ObjectReference:
                    var obj = prop.objectReferenceValue;
                    return obj != null ? new { name = obj.name, instanceId = obj.GetInstanceID() } : null;
                case SerializedPropertyType.Enum: return prop.enumNames[prop.enumValueIndex];
                default: return null;
            }
        }

        private static Task<UacfResponse> HandleSet(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                if (EditorApplication.isPlaying)
                    return UacfResponse.Fail("CONFLICT", "Exit Play Mode first", null, 0);

                var target = ResolveTarget(p);
                if (target == null)
                    return UacfResponse.Fail("OBJECT_NOT_FOUND", "Object not found", null, 0);

                var payload = new GameObjectService.ModifyGameObjectPayload
                {
                    Name = p["name"]?.ToString(),
                    Active = p["active"]?.Value<bool?>(),
                    Tag = p["tag"]?.ToString(),
                    LayerName = p["layer"]?.ToString()
                };

                var pos = p["position"] as JArray;
                var rot = p["rotation"] as JArray;
                var scale = p["scale"] as JArray;
                if (pos != null || rot != null || scale != null)
                {
                    payload.Transform = new GameObjectService.TransformPayload
                    {
                        Position = pos != null ? ToVector3Json(pos) : null,
                        Rotation = rot != null ? ToVector3Json(rot) : null,
                        Scale = scale != null ? ToVector3Json(scale) : null
                    };
                }

                GameObjectService.Modify(target, payload);
                return UacfResponse.Success(new { modified = true }, 0);
            });
        }

        private static Task<UacfResponse> HandleDestroy(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                if (EditorApplication.isPlaying)
                    return UacfResponse.Fail("CONFLICT", "Exit Play Mode first", null, 0);

                var name = p["name"]?.ToString();
                var tag = p["tag"]?.ToString();

                if (!string.IsNullOrEmpty(tag))
                {
                    var arr = GameObject.FindGameObjectsWithTag(tag);
                    foreach (var go in arr)
                        GameObjectService.Destroy(go);
                    return UacfResponse.Success(new { destroyed = arr.Length }, 0);
                }

                var target = ResolveTarget(p);
                if (target == null)
                    return UacfResponse.Fail("OBJECT_NOT_FOUND", "Object not found", null, 0);

                GameObjectService.Destroy(target);
                return UacfResponse.Success(new { destroyed = true }, 0);
            });
        }

        private static Task<UacfResponse> HandleDuplicate(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                if (EditorApplication.isPlaying)
                    return UacfResponse.Fail("CONFLICT", "Exit Play Mode first", null, 0);

                var target = ResolveTarget(p);
                if (target == null)
                    return UacfResponse.Fail("OBJECT_NOT_FOUND", "Object not found", null, 0);

                var newName = p["newName"]?.ToString();
                var count = p["count"]?.Value<int>() ?? 1;

                var results = new List<object>();
                for (int i = 0; i < count; i++)
                {
                    var dup = GameObjectService.Duplicate(target, count > 1 ? $"{newName ?? target.name}_{i + 1}" : newName);
                    if (dup != null)
                        results.Add(new { instanceId = dup.GetInstanceID(), name = dup.name });
                }
                return UacfResponse.Success(new { duplicated = results.ToArray() }, 0);
            });
        }

        private static Task<UacfResponse> HandleCreatePrimitive(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                if (EditorApplication.isPlaying)
                    return UacfResponse.Fail("CONFLICT", "Exit Play Mode first", null, 0);

                var typeStr = p["type"]?.ToString() ?? "Cube";
                PrimitiveType type;
                switch (typeStr.ToLowerInvariant())
                {
                    case "sphere": type = PrimitiveType.Sphere; break;
                    case "capsule": type = PrimitiveType.Capsule; break;
                    case "cylinder": type = PrimitiveType.Cylinder; break;
                    case "plane": type = PrimitiveType.Plane; break;
                    case "quad": type = PrimitiveType.Quad; break;
                    default: type = PrimitiveType.Cube; break;
                }

                var go = GameObject.CreatePrimitive(type);
                go.name = p["name"]?.ToString() ?? typeStr;

                var pos = p["position"] as JArray;
                if (pos != null && pos.Count >= 3)
                    go.transform.position = new Vector3(pos[0].Value<float>(), pos[1].Value<float>(), pos[2].Value<float>());

                var scale = p["scale"] as JArray;
                if (scale != null && scale.Count >= 3)
                    go.transform.localScale = new Vector3(scale[0].Value<float>(), scale[1].Value<float>(), scale[2].Value<float>());

                Undo.RegisterCreatedObjectUndo(go, "UACF Create Primitive");
                return UacfResponse.Success(new { instanceId = go.GetInstanceID(), name = go.name }, 0);
            });
        }

        private static GameObject ResolveTarget(JObject p)
        {
            var target = p["target"];
            if (target == null)
            {
                var name = p["name"]?.ToString();
                var instanceId = p["instanceId"]?.Value<int?>();
                if (instanceId.HasValue)
                    return EditorUtility.InstanceIDToObject(instanceId.Value) as GameObject;
                if (!string.IsNullOrEmpty(name))
                    return GameObjectService.FindByName(name);
                return null;
            }

            if (target is JValue jv)
            {
                if (jv.Type == JTokenType.Integer)
                    return EditorUtility.InstanceIDToObject(jv.Value<int>()) as GameObject;
                var s = jv.ToString();
                if (int.TryParse(s, out var id))
                    return EditorUtility.InstanceIDToObject(id) as GameObject;
                return GameObjectService.FindByName(s);
            }

            var dict = new Dictionary<string, object>();
            if (target is JObject jo)
            {
                if (jo["instanceId"] != null) dict["instance_id"] = jo["instanceId"].Value<int>();
                else if (jo["instance_id"] != null) dict["instance_id"] = jo["instance_id"].Value<int>();
                if (jo["name"] != null) dict["name"] = jo["name"].ToString();
                if (jo["path"] != null) dict["path"] = jo["path"].ToString();
            }

            return dict.Count > 0 ? GameObjectService.FindByTarget(dict) : null;
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
    }
}
