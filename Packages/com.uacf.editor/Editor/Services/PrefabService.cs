using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UACF.Models;

namespace UACF.Services
{
    public class PrefabService
    {
        public static bool CreatePrefab(GameObject source, string path, bool keepConnection = true)
        {
            if (source == null || string.IsNullOrEmpty(path)) return false;

            if (keepConnection)
                return PrefabUtility.SaveAsPrefabAssetAndConnect(source, path, InteractionMode.AutomatedAction);
            return PrefabUtility.SaveAsPrefabAsset(source, path);
        }

        public static GameObject InstantiatePrefab(string prefabPath, string name = null, Transform parent = null,
            Vector3? position = null, Quaternion? rotation = null, Dictionary<string, Dictionary<string, object>> componentOverrides = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return null;

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) return null;

            Undo.RegisterCreatedObjectUndo(instance, "UACF Instantiate Prefab");

            if (!string.IsNullOrEmpty(name))
                instance.name = name;
            if (parent != null)
                instance.transform.SetParent(parent);
            if (position.HasValue)
                instance.transform.position = position.Value;
            if (rotation.HasValue)
                instance.transform.rotation = rotation.Value;

            if (componentOverrides != null)
            {
                foreach (var kv in componentOverrides)
                {
                    var comp = instance.GetComponent(kv.Key);
                    if (comp != null && kv.Value != null)
                        ComponentService.SetFields(comp, kv.Value);
                }
            }

            return instance;
        }

        public static bool ModifyPrefab(string prefabPath, List<PrefabOperation> operations)
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null) return false;

            try
            {
                foreach (var op in operations)
                {
                    if (op.Action == "add_component")
                    {
                        var target = string.IsNullOrEmpty(op.TargetPath) ? prefabRoot : FindInPrefab(prefabRoot, op.TargetPath) ?? prefabRoot;
                        if (target != null)
                            ComponentService.AddComponent(target, op.Component, op.Fields);
                    }
                    else if (op.Action == "add_child")
                    {
                        var parent = string.IsNullOrEmpty(op.TargetPath) ? prefabRoot : FindInPrefab(prefabRoot, op.TargetPath) ?? prefabRoot;
                        if (parent != null)
                        {
                            var child = ObjectFactory.CreateGameObject(op.Name ?? "NewObject");
                            child.transform.SetParent(parent.transform);
                            if (op.Transform?.Position != null)
                                child.transform.localPosition = new Vector3(op.Transform.Position.X, op.Transform.Position.Y, op.Transform.Position.Z);
                        }
                    }
                    else if (op.Action == "set_fields")
                    {
                        var target = string.IsNullOrEmpty(op.TargetPath) ? prefabRoot : FindInPrefab(prefabRoot, op.TargetPath) ?? prefabRoot;
                        if (target != null)
                        {
                            var comp = ComponentService.GetComponent(target, op.Component, 0);
                            if (comp != null && op.Fields != null)
                                ComponentService.SetFields(comp, op.Fields);
                        }
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        public static bool ApplyOverrides(GameObject instance, bool applyAll = true)
        {
            if (instance == null) return false;
            if (!PrefabUtility.IsPartOfAnyPrefab(instance)) return false;

            if (applyAll)
            {
                PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
            }
            else
            {
                PrefabUtility.RevertPrefabInstance(instance, InteractionMode.AutomatedAction);
            }
            return true;
        }

        private static GameObject FindInPrefab(GameObject root, string path)
        {
            var parts = path.TrimStart('/').Split('/');
            if (parts.Length == 0) return root;
            var current = root.transform;
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                var child = current.Find(part);
                if (child == null) return null;
                current = child;
            }
            return current.gameObject;
        }

        public class PrefabOperation
        {
            public string Action;
            public string TargetPath;
            public string Name;
            public string Component;
            public Dictionary<string, object> Fields;
            public TransformInfo Transform;
        }
    }
}
