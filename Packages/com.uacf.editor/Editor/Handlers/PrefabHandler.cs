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
    public static class PrefabHandler
    {
        public static void Register(ActionDispatcher dispatcher)
        {
            dispatcher.Register("prefab.create", HandleCreate);
            dispatcher.Register("prefab.instantiate", HandleInstantiate);
            dispatcher.Register("prefab.contents", HandleContents);
            dispatcher.Register("prefab.edit", HandleEdit);
            dispatcher.Register("prefab.apply", HandleApply);
            dispatcher.Register("prefab.revert", HandleRevert);
            dispatcher.Register("prefab.createVariant", HandleCreateVariant);
        }

        private static GameObject ResolveObject(JObject p)
        {
            var name = p["object"]?.ToString();
            if (string.IsNullOrEmpty(name)) return null;
            return GameObjectService.FindByName(name);
        }

        private static Task<UacfResponse> HandleCreate(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                if (EditorApplication.isPlaying)
                    return UacfResponse.Fail("CONFLICT", "Exit Play Mode first", null, 0);

                var source = p["sourceObject"]?.ToString();
                var path = p["path"]?.ToString();
                if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(path))
                    return UacfResponse.Fail("INVALID_REQUEST", "sourceObject and path are required", null, 0);

                var go = GameObjectService.FindByName(source);
                if (go == null)
                    return UacfResponse.Fail("OBJECT_NOT_FOUND", $"Object '{source}' not found", null, 0);

                var ok = PrefabService.CreatePrefab(go, path);
                return UacfResponse.Success(new { created = ok, path }, 0);
            });
        }

        private static Task<UacfResponse> HandleInstantiate(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                if (EditorApplication.isPlaying)
                    return UacfResponse.Fail("CONFLICT", "Exit Play Mode first", null, 0);

                var path = p["path"]?.ToString();
                if (string.IsNullOrEmpty(path))
                    return UacfResponse.Fail("INVALID_REQUEST", "path is required", null, 0);

                Vector3? pos = null;
                Quaternion? rot = null;
                var posArr = p["position"] as JArray;
                if (posArr != null && posArr.Count >= 3)
                    pos = new Vector3(posArr[0].Value<float>(), posArr[1].Value<float>(), posArr[2].Value<float>());
                var rotArr = p["rotation"] as JArray;
                if (rotArr != null && rotArr.Count >= 3)
                    rot = Quaternion.Euler(rotArr[0].Value<float>(), rotArr[1].Value<float>(), rotArr[2].Value<float>());

                Transform parent = null;
                var parentName = p["parent"]?.ToString();
                if (!string.IsNullOrEmpty(parentName))
                    parent = GameObjectService.FindByName(parentName)?.transform;

                var instance = PrefabService.InstantiatePrefab(path, p["name"]?.ToString(), parent, pos, rot);
                if (instance == null)
                    return UacfResponse.Fail("PREFAB_NOT_FOUND", "Prefab not found", null, 0);

                return UacfResponse.Success(new { instanceId = instance.GetInstanceID(), name = instance.name }, 0);
            });
        }

        private static Task<UacfResponse> HandleContents(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var path = p["path"]?.ToString();
                if (string.IsNullOrEmpty(path))
                    return UacfResponse.Fail("INVALID_REQUEST", "path is required", null, 0);

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    return UacfResponse.Fail("NOT_FOUND", "Prefab not found", null, 0);

                var root = prefab.transform;
                var hierarchy = SerializePrefabHierarchy(root);
                return UacfResponse.Success(new { hierarchy }, 0);
            });
        }

        private static object SerializePrefabHierarchy(Transform t)
        {
            var go = t.gameObject;
            var children = new List<object>();
            for (int i = 0; i < t.childCount; i++)
                children.Add(SerializePrefabHierarchy(t.GetChild(i)));

            return new
            {
                name = go.name,
                components = go.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name).ToArray(),
                children
            };
        }

        private static Task<UacfResponse> HandleEdit(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                if (EditorApplication.isPlaying)
                    return UacfResponse.Fail("CONFLICT", "Exit Play Mode first", null, 0);

                var path = p["path"]?.ToString();
                var ops = p["operations"] as JArray;
                if (string.IsNullOrEmpty(path) || ops == null)
                    return UacfResponse.Fail("INVALID_REQUEST", "path and operations are required", null, 0);

                var operations = new List<PrefabService.PrefabOperation>();
                foreach (var op in ops)
                {
                    var jo = op as JObject;
                    if (jo == null) continue;

                    var opType = jo["op"]?.ToString() ?? jo["action"]?.ToString();
                    var target = jo["target"]?.ToString() ?? ".";
                    var operation = new PrefabService.PrefabOperation { TargetPath = target == "." ? "" : target };

                    if (opType == "addComponent" || opType == "add_component")
                    {
                        operation.Action = "add_component";
                        operation.Component = jo["type"]?.ToString();
                        operation.Fields = jo["properties"]?.ToObject<Dictionary<string, object>>();
                    }
                    else if (opType == "setProperty" || opType == "set_property")
                    {
                        operation.Action = "set_fields";
                        operation.Component = jo["component"]?.ToString();
                        operation.Fields = new Dictionary<string, object> { [jo["property"]?.ToString() ?? ""] = jo["value"] };
                    }
                    else if (opType == "addChild" || opType == "add_child")
                    {
                        operation.Action = "add_child";
                        operation.Name = jo["name"]?.ToString();
                    }
                    else if (opType == "removeComponent" || opType == "remove_component")
                    {
                        operation.Action = "remove_component";
                        operation.Component = jo["type"]?.ToString();
                    }

                    if (!string.IsNullOrEmpty(operation.Action))
                        operations.Add(operation);
                }

                var ok = PrefabService.ModifyPrefab(path, operations);
                return UacfResponse.Success(new { modified = ok }, 0);
            });
        }

        private static Task<UacfResponse> HandleApply(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                if (EditorApplication.isPlaying)
                    return UacfResponse.Fail("CONFLICT", "Exit Play Mode first", null, 0);

                var go = ResolveObject(p);
                if (go == null)
                    return UacfResponse.Fail("OBJECT_NOT_FOUND", "Object not found", null, 0);

                if (!PrefabUtility.IsPartOfAnyPrefab(go))
                    return UacfResponse.Fail("INVALID_REQUEST", "Object is not a prefab instance", null, 0);

                PrefabUtility.ApplyPrefabInstance(go, InteractionMode.AutomatedAction);
                return UacfResponse.Success(new { applied = true }, 0);
            });
        }

        private static Task<UacfResponse> HandleRevert(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                if (EditorApplication.isPlaying)
                    return UacfResponse.Fail("CONFLICT", "Exit Play Mode first", null, 0);

                var go = ResolveObject(p);
                if (go == null)
                    return UacfResponse.Fail("OBJECT_NOT_FOUND", "Object not found", null, 0);

                if (!PrefabUtility.IsPartOfAnyPrefab(go))
                    return UacfResponse.Fail("INVALID_REQUEST", "Object is not a prefab instance", null, 0);

                PrefabUtility.RevertPrefabInstance(go, InteractionMode.AutomatedAction);
                return UacfResponse.Success(new { reverted = true }, 0);
            });
        }

        private static Task<UacfResponse> HandleCreateVariant(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                if (EditorApplication.isPlaying)
                    return UacfResponse.Fail("CONFLICT", "Exit Play Mode first", null, 0);

                var basePath = p["basePrefab"]?.ToString();
                var path = p["path"]?.ToString();
                if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(path))
                    return UacfResponse.Fail("INVALID_REQUEST", "basePrefab and path are required", null, 0);

                var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath);
                if (basePrefab == null)
                    return UacfResponse.Fail("NOT_FOUND", "Base prefab not found", null, 0);

                var instance = PrefabUtility.InstantiatePrefab(basePrefab) as GameObject;
                if (instance == null)
                    return UacfResponse.Fail("INTERNAL_ERROR", "Failed to instantiate", null, 0);

                PrefabUtility.SaveAsPrefabAsset(instance, path);
                Object.DestroyImmediate(instance);
                return UacfResponse.Success(new { created = true, path }, 0);
            });
        }
    }
}
