using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UACF.Models;

namespace UACF.Services
{
    public static class SerializationService
    {
        public static GameObjectInfo[] SerializeHierarchy(Scene scene, int depth = -1, bool includeComponents = false)
        {
            var roots = scene.GetRootGameObjects();
            var result = new List<GameObjectInfo>();
            foreach (var root in roots)
            {
                result.Add(SerializeGameObject(root, "", depth, 0, includeComponents));
            }
            return result.ToArray();
        }

        private static string GetTagSafe(GameObject go)
        {
            try { return go.tag ?? ""; }
            catch { return ""; }
        }

        private static string GetLayerNameSafe(GameObject go)
        {
            try { return LayerMask.LayerToName(go.layer) ?? ""; }
            catch { return ""; }
        }

        private static GameObjectInfo SerializeGameObject(GameObject go, string path, int maxDepth, int currentDepth, bool includeComponents)
        {
            var info = new GameObjectInfo
            {
                InstanceId = go.GetInstanceID(),
                Name = go.name,
                Active = go.activeInHierarchy,
                ActiveSelf = go.activeSelf,
                Tag = GetTagSafe(go),
                Layer = go.layer,
                LayerName = GetLayerNameSafe(go),
                Static = go.isStatic,
                Path = string.IsNullOrEmpty(path) ? "/" + go.name : path + "/" + go.name,
                Transform = new TransformInfo
                {
                    LocalPosition = ToVector3(go.transform.localPosition),
                    LocalRotation = ToQuaternion(go.transform.localRotation),
                    LocalScale = ToVector3(go.transform.localScale)
                }
            };

            if (includeComponents)
            {
                var comps = new List<string>();
                foreach (var c in go.GetComponents<Component>())
                {
                    if (c != null)
                        comps.Add(c.GetType().Name);
                }
                info.Components = comps.ToArray();
            }

            if (maxDepth < 0 || currentDepth < maxDepth)
            {
                var children = new List<GameObjectInfo>();
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    var child = go.transform.GetChild(i).gameObject;
                    children.Add(SerializeGameObject(child, info.Path, maxDepth, currentDepth + 1, includeComponents));
                }
                info.Children = children.ToArray();
            }
            else
            {
                info.Children = new GameObjectInfo[0];
            }

            return info;
        }

        public static Vector3Json ToVector3(Vector3 v) => new Vector3Json { X = v.x, Y = v.y, Z = v.z };
        public static QuaternionJson ToQuaternion(Quaternion q) => new QuaternionJson { X = q.x, Y = q.y, Z = q.z, W = q.w };
    }
}
