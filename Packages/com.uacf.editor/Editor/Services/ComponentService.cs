using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.Plastic.Newtonsoft.Json.Linq;

namespace UACF.Services
{
    public class ComponentService
    {
        public static Component AddComponent(GameObject go, string componentType, Dictionary<string, object> fields = null)
        {
            var type = TypeResolverService.Instance.Resolve(componentType);
            if (type == null) return null;

            var comp = go.AddComponent(type);
            if (comp != null && fields != null && fields.Count > 0)
                SetFields(comp, fields);
            return comp;
        }

        public static void SetFields(Component component, Dictionary<string, object> fields)
        {
            if (component == null || fields == null) return;
            var so = new SerializedObject(component);
            foreach (var kv in fields)
            {
                var prop = so.FindProperty(kv.Key);
                if (prop != null)
                    SetPropertyValue(prop, kv.Value);
            }
            so.ApplyModifiedProperties();
        }

        public static void SetSerializedFields(Component component, JObject fields)
        {
            if (component == null || fields == null) return;

            var so = new SerializedObject(component);
            foreach (var kv in fields)
            {
                var prop = so.FindProperty(kv.Key);
                if (prop == null) continue;
                SetPropertyValue(prop, ConvertTokenToValue(kv.Value));
            }
            so.ApplyModifiedProperties();
        }

        private static void SetPropertyValue(SerializedProperty prop, object value)
        {
            if (value == null)
            {
                prop.objectReferenceValue = null;
                return;
            }

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    prop.intValue = Convert.ToInt32(value);
                    break;
                case SerializedPropertyType.Float:
                    prop.floatValue = Convert.ToSingle(value);
                    break;
                case SerializedPropertyType.Boolean:
                    prop.boolValue = Convert.ToBoolean(value);
                    break;
                case SerializedPropertyType.String:
                    prop.stringValue = value?.ToString() ?? "";
                    break;
                case SerializedPropertyType.Vector2:
                    var v2 = ParseVector2(value);
                    if (v2.HasValue) prop.vector2Value = v2.Value;
                    break;
                case SerializedPropertyType.Vector3:
                    var v3 = ParseVector3(value);
                    if (v3.HasValue) prop.vector3Value = v3.Value;
                    break;
                case SerializedPropertyType.Vector4:
                    var v4 = ParseVector4(value);
                    if (v4.HasValue) prop.vector4Value = v4.Value;
                    break;
                case SerializedPropertyType.ObjectReference:
                    var refObj = ResolveReference(value, prop);
                    if (refObj != null || value == null)
                        prop.objectReferenceValue = refObj;
                    break;
                case SerializedPropertyType.Enum:
                    if (value is string s)
                    {
                        var idx = System.Array.IndexOf(prop.enumNames, s);
                        if (idx >= 0) prop.enumValueIndex = idx;
                    }
                    else
                        prop.enumValueIndex = Convert.ToInt32(value);
                    break;
                case SerializedPropertyType.Color:
                    var c = ParseColor(value);
                    if (c.HasValue) prop.colorValue = c.Value;
                    break;
            }
        }

        private static Vector2? ParseVector2(object value)
        {
            if (value is JObject jObj)
            {
                var jx = jObj["x"]?.Value<float>() ?? 0f;
                var jy = jObj["y"]?.Value<float>() ?? 0f;
                return new Vector2(jx, jy);
            }

            var dict = value as Dictionary<string, object>;
            if (dict == null) return null;
            var dx = dict.TryGetValue("x", out var vx) ? Convert.ToSingle(vx) : 0f;
            var dy = dict.TryGetValue("y", out var vy) ? Convert.ToSingle(vy) : 0f;
            return new Vector2(dx, dy);
        }

        private static Vector3? ParseVector3(object value)
        {
            if (value is JObject jObj)
            {
                var jx = jObj["x"]?.Value<float>() ?? 0f;
                var jy = jObj["y"]?.Value<float>() ?? 0f;
                var jz = jObj["z"]?.Value<float>() ?? 0f;
                return new Vector3(jx, jy, jz);
            }

            var dict = value as Dictionary<string, object>;
            if (dict == null) return null;
            var dx = dict.TryGetValue("x", out var vx) ? Convert.ToSingle(vx) : 0f;
            var dy = dict.TryGetValue("y", out var vy) ? Convert.ToSingle(vy) : 0f;
            var dz = dict.TryGetValue("z", out var vz) ? Convert.ToSingle(vz) : 0f;
            return new Vector3(dx, dy, dz);
        }

        private static Vector4? ParseVector4(object value)
        {
            if (value is JObject jObj)
            {
                var jx = jObj["x"]?.Value<float>() ?? 0f;
                var jy = jObj["y"]?.Value<float>() ?? 0f;
                var jz = jObj["z"]?.Value<float>() ?? 0f;
                var jw = jObj["w"]?.Value<float>() ?? 0f;
                return new Vector4(jx, jy, jz, jw);
            }

            var dict = value as Dictionary<string, object>;
            if (dict == null) return null;
            var dx = dict.TryGetValue("x", out var vx) ? Convert.ToSingle(vx) : 0f;
            var dy = dict.TryGetValue("y", out var vy) ? Convert.ToSingle(vy) : 0f;
            var dz = dict.TryGetValue("z", out var vz) ? Convert.ToSingle(vz) : 0f;
            var dw = dict.TryGetValue("w", out var vw) ? Convert.ToSingle(vw) : 0f;
            return new Vector4(dx, dy, dz, dw);
        }

        private static Color? ParseColor(object value)
        {
            if (value is JObject jObj)
            {
                var jr = jObj["r"]?.Value<float>() ?? 1f;
                var jg = jObj["g"]?.Value<float>() ?? 0f;
                var jb = jObj["b"]?.Value<float>() ?? 0f;
                var ja = jObj["a"]?.Value<float>() ?? 1f;
                return new Color(jr, jg, jb, ja);
            }

            var dict = value as Dictionary<string, object>;
            if (dict == null) return null;
            var dr = dict.TryGetValue("r", out var vr) ? Convert.ToSingle(vr) : 1f;
            var dg = dict.TryGetValue("g", out var vg) ? Convert.ToSingle(vg) : 0f;
            var db = dict.TryGetValue("b", out var vb) ? Convert.ToSingle(vb) : 0f;
            var da = dict.TryGetValue("a", out var va) ? Convert.ToSingle(va) : 1f;
            return new Color(dr, dg, db, da);
        }

        private static UnityEngine.Object ResolveReference(object value, SerializedProperty prop)
        {
            if (value is UnityEngine.Object unityObj)
                return unityObj;

            var pathStr = value as string;
            if (!string.IsNullOrEmpty(pathStr) && pathStr.StartsWith("Assets/"))
            {
                return LoadAssetForProperty(pathStr, prop);
            }

            var jobj = value as JObject;
            if (jobj != null)
            {
                var instanceIdToken = jobj["instanceId"] ?? jobj["instance_id"];
                if (instanceIdToken != null)
                {
#pragma warning disable CS0618
                    return EditorUtility.InstanceIDToObject(instanceIdToken.Value<int>());
#pragma warning restore CS0618
                }

                var nestedRefToken = jobj["reference"] as JObject;
                if (nestedRefToken != null)
                {
                    var nestedIdToken = nestedRefToken["instanceId"] ?? nestedRefToken["instance_id"];
                    if (nestedIdToken != null)
                    {
#pragma warning disable CS0618
                        return EditorUtility.InstanceIDToObject(nestedIdToken.Value<int>());
#pragma warning restore CS0618
                    }

                    var nestedNameToken = nestedRefToken["name"];
                    if (nestedNameToken != null)
                    {
                        var go = GameObjectService.FindByName(nestedNameToken.ToString());
                        if (go != null)
                        {
                            var refType = ResolveSerializedReferenceType(prop.type);
                            if (refType != null && typeof(Component).IsAssignableFrom(refType))
                                return go.GetComponent(refType);
                            if (refType != null && typeof(Transform).IsAssignableFrom(refType))
                                return go.transform;
                        }
                    }
                }

                var goNameToken = jobj["name"];
                if (goNameToken != null && jobj["asset"] == null)
                {
                    var go = GameObjectService.FindByName(goNameToken.ToString());
                    if (go != null)
                    {
                        var refType = ResolveSerializedReferenceType(prop.type);
                        if (refType != null && typeof(Component).IsAssignableFrom(refType))
                            return go.GetComponent(refType);
                        if (refType != null && typeof(Transform).IsAssignableFrom(refType))
                            return go.transform;
                    }
                }

                var pathToken = jobj["asset"];
                if (pathToken != null)
                {
                    var path = pathToken.ToString();
                    if (!string.IsNullOrEmpty(path))
                        return LoadAssetForProperty(path, prop);
                }
                return null;
            }

            var dict = value as Dictionary<string, object>;
            if (dict == null) return null;

            if (dict.TryGetValue("reference", out var refObj))
            {
                var refDict = refObj as Dictionary<string, object>;
                if (refDict == null) return null;

                if (refDict.TryGetValue("instance_id", out var idObj) || refDict.TryGetValue("instanceId", out idObj))
                {
                    var id = Convert.ToInt32(idObj);
#pragma warning disable CS0618
                    return EditorUtility.InstanceIDToObject(id);
#pragma warning restore CS0618
                }
                if (refDict.TryGetValue("name", out var nameObj))
                {
                    var go = GameObjectService.FindByName(nameObj?.ToString());
                    if (go == null) return null;
                    var refType = TypeResolverService.Instance.Resolve(prop.type);
                    if (refType == null) return null;
                    if (typeof(Transform).IsAssignableFrom(refType)) return go.transform;
                    return go.GetComponent(refType);
                }
            }

            if (dict.TryGetValue("asset", out var pathObj))
            {
                var path = pathObj?.ToString();
                if (!string.IsNullOrEmpty(path))
                    return LoadAssetForProperty(path, prop);
            }

            return null;
        }

        private static object ConvertTokenToValue(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return null;
            if (token is JValue jv) return jv.Value;
            if (token is JObject jo) return jo;
            if (token is JArray ja) return ja.ToObject<List<object>>();
            return token.ToString();
        }

        private static UnityEngine.Object LoadAssetForProperty(string path, SerializedProperty prop)
        {
            if (string.IsNullOrEmpty(path)) return null;

            var mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
            if (mainAsset != null) return mainAsset;

            var refType = ResolveSerializedReferenceType(prop.type);
            if (refType != null)
            {
                var typed = AssetDatabase.LoadAssetAtPath(path, refType);
                if (typed != null) return typed;
            }

            return AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
        }

        private static Type ResolveSerializedReferenceType(string serializedTypeName)
        {
            if (string.IsNullOrEmpty(serializedTypeName)) return null;

            var direct = TypeResolverService.Instance.Resolve(serializedTypeName);
            if (direct != null) return direct;

            var normalized = serializedTypeName;
            if (normalized.StartsWith("PPtr<") && normalized.EndsWith(">"))
                normalized = normalized.Substring(5, normalized.Length - 6).TrimStart('$');

            var resolved = TypeResolverService.Instance.Resolve(normalized);
            if (resolved != null) return resolved;

            return TypeResolverService.Instance.Resolve("UnityEngine.UIElements." + normalized);
        }

        public static void RemoveComponent(Component comp)
        {
            if (comp != null)
                Undo.DestroyObjectImmediate(comp);
        }

        public static Component GetComponent(GameObject go, string componentType, int index = 0)
        {
            var type = TypeResolverService.Instance.Resolve(componentType);
            if (type == null) return null;
            var comps = go.GetComponents(type);
            return index < comps.Length ? comps[index] : null;
        }
    }
}
