using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UACF.Core;
using UACF.Models;
using UACF.Services;

namespace UACF.Handlers
{
    public static class RuntimeHandler
    {
        public static void Register(ActionDispatcher dispatcher)
        {
            dispatcher.Register("runtime.inspect", HandleInspect);
            dispatcher.Register("runtime.invoke", HandleInvoke);
        }

        private static Task<UacfResponse> HandleInspect(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                if (!Application.isPlaying)
                    return UacfResponse.Fail("CONFLICT", "Not in Play Mode", "Use editor.play first", 0);

                var objName = p["object"]?.ToString();
                var component = p["component"]?.ToString();
                if (string.IsNullOrEmpty(objName) || string.IsNullOrEmpty(component))
                    return UacfResponse.Fail("INVALID_REQUEST", "object and component are required", null, 0);

                var go = GameObject.Find(objName);
                if (go == null)
                    return UacfResponse.Fail("OBJECT_NOT_FOUND", "Object not found", null, 0);

                var comp = go.GetComponent(component);
                if (comp == null)
                    return UacfResponse.Fail("COMPONENT_NOT_FOUND", "Component not found", null, 0);

                var so = new UnityEditor.SerializedObject(comp);
                var props = new System.Collections.Generic.Dictionary<string, object>();
                var it = so.GetIterator();
                it.Next(true);
                while (it.Next(false))
                {
                    if (it.depth > 1) continue;
                    var val = GetRuntimeValue(it, comp);
                    if (val != null) props[it.name] = val;
                }

                return UacfResponse.Success(props, 0);
            });
        }

        private static object GetRuntimeValue(UnityEditor.SerializedProperty prop, Component comp)
        {
            switch (prop.propertyType)
            {
                case UnityEditor.SerializedPropertyType.Integer: return prop.intValue;
                case UnityEditor.SerializedPropertyType.Float: return prop.floatValue;
                case UnityEditor.SerializedPropertyType.Boolean: return prop.boolValue;
                case UnityEditor.SerializedPropertyType.String: return prop.stringValue;
                case UnityEditor.SerializedPropertyType.Vector2: return new[] { prop.vector2Value.x, prop.vector2Value.y };
                case UnityEditor.SerializedPropertyType.Vector3: return new[] { prop.vector3Value.x, prop.vector3Value.y, prop.vector3Value.z };
                case UnityEditor.SerializedPropertyType.ObjectReference:
                    var obj = prop.objectReferenceValue;
                    return obj != null ? new { name = obj.name, instanceId = obj.GetInstanceID() } : null;
                default: return null;
            }
        }

        private static Task<UacfResponse> HandleInvoke(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                if (!Application.isPlaying)
                    return UacfResponse.Fail("CONFLICT", "Not in Play Mode", "Use editor.play first", 0);

                var objName = p["object"]?.ToString();
                var component = p["component"]?.ToString();
                var methodName = p["method"]?.ToString();
                if (string.IsNullOrEmpty(objName) || string.IsNullOrEmpty(component) || string.IsNullOrEmpty(methodName))
                    return UacfResponse.Fail("INVALID_REQUEST", "object, component, and method are required", null, 0);

                var go = GameObject.Find(objName);
                if (go == null)
                    return UacfResponse.Fail("OBJECT_NOT_FOUND", "Object not found", null, 0);

                var comp = go.GetComponent(component);
                if (comp == null)
                    return UacfResponse.Fail("COMPONENT_NOT_FOUND", "Component not found", null, 0);

                var method = comp.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod);
                if (method == null)
                    return UacfResponse.Fail("METHOD_NOT_FOUND", $"Method '{methodName}' not found", null, 0);

                var argsToken = p["args"] as JArray;
                object[] args = null;
                if (argsToken != null)
                {
                    var paramTypes = method.GetParameters();
                    args = new object[paramTypes.Length];
                    for (int i = 0; i < Math.Min(argsToken.Count, paramTypes.Length); i++)
                    {
                        var paramType = paramTypes[i].ParameterType;
                        var val = argsToken[i];
                        if (paramType == typeof(string)) args[i] = val?.ToString();
                        else if (paramType == typeof(int)) args[i] = val?.Value<int>() ?? 0;
                        else if (paramType == typeof(float)) args[i] = val?.Value<float>() ?? 0f;
                        else if (paramType == typeof(bool)) args[i] = val?.Value<bool>() ?? false;
                        else args[i] = val?.ToObject(paramType);
                    }
                }

                try
                {
                    var result = method.Invoke(comp, args);
                    return UacfResponse.Success(new { result }, 0);
                }
                catch (Exception ex)
                {
                    var inner = ex.InnerException ?? ex;
                    return UacfResponse.Fail("INVOCATION_ERROR", inner.Message, inner.StackTrace, 0);
                }
            });
        }
    }
}
