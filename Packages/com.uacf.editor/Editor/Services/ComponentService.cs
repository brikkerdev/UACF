using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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
            var dict = value as Dictionary<string, object>;
            if (dict == null) return null;
            var x = dict.TryGetValue("x", out var vx) ? Convert.ToSingle(vx) : 0f;
            var y = dict.TryGetValue("y", out var vy) ? Convert.ToSingle(vy) : 0f;
            return new Vector2(x, y);
        }

        private static Vector3? ParseVector3(object value)
        {
            var dict = value as Dictionary<string, object>;
            if (dict == null) return null;
            var x = dict.TryGetValue("x", out var vx) ? Convert.ToSingle(vx) : 0f;
            var y = dict.TryGetValue("y", out var vy) ? Convert.ToSingle(vy) : 0f;
            var z = dict.TryGetValue("z", out var vz) ? Convert.ToSingle(vz) : 0f;
            return new Vector3(x, y, z);
        }

        private static Vector4? ParseVector4(object value)
        {
            var dict = value as Dictionary<string, object>;
            if (dict == null) return null;
            var x = dict.TryGetValue("x", out var vx) ? Convert.ToSingle(vx) : 0f;
            var y = dict.TryGetValue("y", out var vy) ? Convert.ToSingle(vy) : 0f;
            var z = dict.TryGetValue("z", out var vz) ? Convert.ToSingle(vz) : 0f;
            var w = dict.TryGetValue("w", out var vw) ? Convert.ToSingle(vw) : 0f;
            return new Vector4(x, y, z, w);
        }

        private static Color? ParseColor(object value)
        {
            var dict = value as Dictionary<string, object>;
            if (dict == null) return null;
            var r = dict.TryGetValue("r", out var vr) ? Convert.ToSingle(vr) : 1f;
            var g = dict.TryGetValue("g", out var vg) ? Convert.ToSingle(vg) : 0f;
            var b = dict.TryGetValue("b", out var vb) ? Convert.ToSingle(vb) : 0f;
            var a = dict.TryGetValue("a", out var va) ? Convert.ToSingle(va) : 1f;
            return new Color(r, g, b, a);
        }

        private static UnityEngine.Object ResolveReference(object value, SerializedProperty prop)
        {
            var dict = value as Dictionary<string, object>;
            if (dict == null) return null;

            if (dict.TryGetValue("reference", out var refObj))
            {
                var refDict = refObj as Dictionary<string, object>;
                if (refDict == null) return null;

                if (refDict.TryGetValue("instance_id", out var idObj))
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
                {
                    var refType = TypeResolverService.Instance.Resolve(prop.type);
                    return AssetDatabase.LoadAssetAtPath(path, refType ?? typeof(UnityEngine.Object));
                }
            }

            return null;
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
