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
    public static class ComponentHandler
    {
        public static void Register(ActionDispatcher dispatcher)
        {
            dispatcher.Register("component.list", HandleList);
            dispatcher.Register("component.get", HandleGet);
            dispatcher.Register("component.add", HandleAdd);
            dispatcher.Register("component.set", HandleSet);
            dispatcher.Register("component.remove", HandleRemove);
            dispatcher.Register("component.setEnabled", HandleSetEnabled);
            dispatcher.Register("component.serialized.get", HandleSerializedGet);
            dispatcher.Register("component.serialized.set", HandleSerializedSet);
        }

        private static GameObject ResolveObject(JObject p)
        {
            var obj = p["object"];
            if (obj == null) return null;

            if (obj is JValue jv)
            {
                var s = jv.ToString();
                if (int.TryParse(s, out var id))
                    return EditorUtility.InstanceIDToObject(id) as GameObject;
                return GameObjectService.FindByName(s);
            }
            return GameObjectService.FindByName(obj.ToString());
        }

        private static Task<UacfResponse> HandleList(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var go = ResolveObject(p);
                if (go == null)
                    return UacfResponse.Fail("OBJECT_NOT_FOUND", "Object not found", null, 0);

                var comps = go.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name).ToArray();
                return UacfResponse.Success(new { components = comps }, 0);
            });
        }

        private static Task<UacfResponse> HandleGet(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var go = ResolveObject(p);
                if (go == null)
                    return UacfResponse.Fail("OBJECT_NOT_FOUND", "Object not found", null, 0);

                var component = p["component"]?.ToString();
                if (string.IsNullOrEmpty(component))
                    return UacfResponse.Fail("INVALID_REQUEST", "component is required", null, 0);

                var comp = ComponentService.GetComponent(go, component, 0);
                if (comp == null)
                    return UacfResponse.Fail("COMPONENT_NOT_FOUND", $"Component '{component}' not found", null, 0);

                var so = new SerializedObject(comp);
                var props = new Dictionary<string, object>();
                var it = so.GetIterator();
                it.Next(true);
                while (it.Next(false))
                {
                    if (it.propertyType == SerializedPropertyType.Generic && it.depth > 2) continue;
                    var val = GetPropertyValue(it);
                    if (val != null) props[it.name] = val;
                }

                return UacfResponse.Success(new
                {
                    type = comp.GetType().Name,
                    enabled = (comp as Behaviour)?.enabled ?? true,
                    properties = props
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

        private static Task<UacfResponse> HandleAdd(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                if (EditorApplication.isPlaying)
                    return UacfResponse.Fail("CONFLICT", "Exit Play Mode first", null, 0);

                var go = ResolveObject(p);
                if (go == null)
                    return UacfResponse.Fail("OBJECT_NOT_FOUND", "Object not found", null, 0);

                var type = p["type"]?.ToString();
                if (string.IsNullOrEmpty(type))
                    return UacfResponse.Fail("INVALID_REQUEST", "type is required", null, 0);

                var resolved = TypeResolverService.Instance.Resolve(type);
                if (resolved == null)
                {
                    var suggestions = TypeResolverService.Instance.GetSuggestions(type);
                    return UacfResponse.Fail("TYPE_NOT_FOUND", $"Type '{type}' not found", string.Join(", ", suggestions ?? new string[0]), 0);
                }

                var props = p["properties"]?.ToObject<Dictionary<string, object>>();
                var comp = ComponentService.AddComponent(go, type, props);
                if (comp == null)
                    return UacfResponse.Fail("INTERNAL_ERROR", "Failed to add component", null, 0);

                return UacfResponse.Success(new { componentAdded = type }, 0);
            });
        }

        private static Task<UacfResponse> HandleSet(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                if (EditorApplication.isPlaying)
                    return UacfResponse.Fail("CONFLICT", "Exit Play Mode first", null, 0);

                var go = ResolveObject(p);
                if (go == null)
                    return UacfResponse.Fail("OBJECT_NOT_FOUND", "Object not found", null, 0);

                var component = p["component"]?.ToString();
                if (string.IsNullOrEmpty(component))
                    return UacfResponse.Fail("INVALID_REQUEST", "component is required", null, 0);

                var props = p["properties"]?.ToObject<Dictionary<string, object>>();
                if (props == null || props.Count == 0)
                    return UacfResponse.Fail("INVALID_REQUEST", "properties is required", null, 0);

                var comp = ComponentService.GetComponent(go, component, 0);
                if (comp == null)
                    return UacfResponse.Fail("COMPONENT_NOT_FOUND", $"Component '{component}' not found", null, 0);

                ComponentService.SetFields(comp, props);
                return UacfResponse.Success(new { updated = true }, 0);
            });
        }

        private static Task<UacfResponse> HandleRemove(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                if (EditorApplication.isPlaying)
                    return UacfResponse.Fail("CONFLICT", "Exit Play Mode first", null, 0);

                var go = ResolveObject(p);
                if (go == null)
                    return UacfResponse.Fail("OBJECT_NOT_FOUND", "Object not found", null, 0);

                var component = p["component"]?.ToString();
                if (string.IsNullOrEmpty(component))
                    return UacfResponse.Fail("INVALID_REQUEST", "component is required", null, 0);

                var comp = ComponentService.GetComponent(go, component, 0);
                if (comp == null || comp is Transform)
                    return UacfResponse.Fail("COMPONENT_NOT_FOUND", "Cannot remove Transform", null, 0);

                ComponentService.RemoveComponent(comp);
                return UacfResponse.Success(new { removed = true }, 0);
            });
        }

        private static Task<UacfResponse> HandleSetEnabled(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                if (EditorApplication.isPlaying)
                    return UacfResponse.Fail("CONFLICT", "Exit Play Mode first", null, 0);

                var go = ResolveObject(p);
                if (go == null)
                    return UacfResponse.Fail("OBJECT_NOT_FOUND", "Object not found", null, 0);

                var component = p["component"]?.ToString();
                if (string.IsNullOrEmpty(component))
                    return UacfResponse.Fail("INVALID_REQUEST", "component is required", null, 0);

                var enabled = p["enabled"]?.Value<bool>() ?? false;

                var comp = ComponentService.GetComponent(go, component, 0) as Behaviour;
                if (comp == null)
                    return UacfResponse.Fail("COMPONENT_NOT_FOUND", "Component not found or not a Behaviour", null, 0);

                Undo.RecordObject(comp, "UACF Set Enabled");
                comp.enabled = enabled;
                return UacfResponse.Success(new { enabled = comp.enabled }, 0);
            });
        }

        private static Task<UacfResponse> HandleSerializedGet(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var go = ResolveObject(p);
                if (go == null)
                    return UacfResponse.Fail("OBJECT_NOT_FOUND", "Object not found", null, 0);

                var component = p["component"]?.ToString();
                if (string.IsNullOrEmpty(component))
                    return UacfResponse.Fail("INVALID_REQUEST", "component is required", null, 0);

                var comp = ComponentService.GetComponent(go, component, 0);
                if (comp == null)
                    return UacfResponse.Fail("COMPONENT_NOT_FOUND", "Component not found", null, 0);

                var so = new SerializedObject(comp);
                var props = new List<object>();
                var it = so.GetIterator();
                it.Next(true);
                while (it.Next(false))
                {
                    if (it.depth > 1) continue;
                    props.Add(new { name = it.name, type = it.propertyType.ToString(), value = GetPropertyValue(it) });
                }

                return UacfResponse.Success(new { properties = props }, 0);
            });
        }

        private static Task<UacfResponse> HandleSerializedSet(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                if (EditorApplication.isPlaying)
                    return UacfResponse.Fail("CONFLICT", "Exit Play Mode first", null, 0);

                var go = ResolveObject(p);
                if (go == null)
                    return UacfResponse.Fail("OBJECT_NOT_FOUND", "Object not found", null, 0);

                var component = p["component"]?.ToString();
                if (string.IsNullOrEmpty(component))
                    return UacfResponse.Fail("INVALID_REQUEST", "component is required", null, 0);

                var props = p["properties"] as JObject;
                if (props == null)
                    return UacfResponse.Fail("INVALID_REQUEST", "properties is required", null, 0);

                var comp = ComponentService.GetComponent(go, component, 0);
                if (comp == null)
                    return UacfResponse.Fail("COMPONENT_NOT_FOUND", "Component not found", null, 0);

                ComponentService.SetSerializedFields(comp, props);
                return UacfResponse.Success(new { updated = true }, 0);
            });
        }
    }
}
