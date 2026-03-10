using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UACF.Models;

namespace UACF.Services
{
    public class GameObjectService
    {
        public static GameObject FindByTarget(object target)
        {
            if (target == null) return null;
            var dict = target as Dictionary<string, object>;
            if (dict == null) return null;

            if (dict.TryGetValue("instance_id", out var idObj) && idObj != null)
            {
                try
                {
                    var id = System.Convert.ToInt32(idObj);
#pragma warning disable CS0618
                    var obj = EditorUtility.InstanceIDToObject(id) as GameObject;
#pragma warning restore CS0618
                    if (obj != null) return obj;
                }
                catch { }
            }

            if (dict.TryGetValue("name", out var nameObj))
            {
                var name = nameObj?.ToString();
                if (!string.IsNullOrEmpty(name))
                    return FindByName(name);
            }

            if (dict.TryGetValue("path", out var pathObj))
            {
                var path = pathObj?.ToString();
                if (!string.IsNullOrEmpty(path))
                    return FindByPath(path);
            }

            if (dict.TryGetValue("tag", out var tagObj))
            {
                var tag = tagObj?.ToString();
                if (!string.IsNullOrEmpty(tag))
                {
                    var arr = GameObject.FindGameObjectsWithTag(tag);
                    return arr.Length > 0 ? arr[0] : null;
                }
            }

            return null;
        }

        public static GameObject FindByName(string name)
        {
            var all = FindAllByName(name);
            return all.Length > 0 ? all[0] : null;
        }

        public static GameObject[] FindAllByName(string name)
        {
            var list = new List<GameObject>();
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
                FindInHierarchyCollect(root.transform, name, list);
            return list.ToArray();
        }

        private static void FindInHierarchyCollect(Transform parent, string name, List<GameObject> list)
        {
            if (parent.name == name) list.Add(parent.gameObject);
            for (int i = 0; i < parent.childCount; i++)
                FindInHierarchyCollect(parent.GetChild(i), name, list);
        }

        private static GameObject FindInHierarchy(Transform parent, string name)
        {
            if (parent.name == name) return parent.gameObject;
            for (int i = 0; i < parent.childCount; i++)
            {
                var found = FindInHierarchy(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        public static GameObject FindByPath(string path)
        {
            var parts = path.TrimStart('/').Split('/');
            if (parts.Length == 0) return null;

            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            GameObject current = null;
            foreach (var root in roots)
            {
                if (root.name == parts[0])
                {
                    current = root;
                    break;
                }
            }
            if (current == null) return null;

            for (int i = 1; i < parts.Length; i++)
            {
                var child = current.transform.Find(parts[i]);
                if (child == null) return null;
                current = child.gameObject;
            }
            return current;
        }

        public static GameObject Create(CreateGameObjectPayload payload)
        {
            if (UnityEngine.Application.isPlaying)
                return null;

            var go = ObjectFactory.CreateGameObject(payload.Name ?? "GameObject");
            Undo.RegisterCreatedObjectUndo(go, "UACF Create GameObject");

            if (payload.Parent != null)
            {
                var parent = ResolveParent(payload.Parent);
                if (parent != null)
                    go.transform.SetParent(parent.transform, payload.WorldPositionStays);
            }

            if (!string.IsNullOrEmpty(payload.Tag) && UnityEditorInternal.InternalEditorUtility.tags.Contains(payload.Tag))
                go.tag = payload.Tag;
            if (payload.Layer >= 0)
                go.layer = payload.Layer;
            if (!string.IsNullOrEmpty(payload.LayerName))
            {
                var layer = LayerMask.NameToLayer(payload.LayerName);
                if (layer >= 0) go.layer = layer;
            }
            go.isStatic = payload.Static;
            go.SetActive(payload.Active);

            if (payload.Transform != null)
            {
                if (payload.Transform.Position != null)
                    go.transform.position = ToVector3(payload.Transform.Position);
                if (payload.Transform.Rotation != null)
                    go.transform.rotation = ToQuaternionOrEuler(payload.Transform.Rotation);
                if (payload.Transform.Scale != null)
                    go.transform.localScale = ToVector3(payload.Transform.Scale);
            }

            return go;
        }

        private static Transform ResolveParent(object parent)
        {
            if (parent == null) return null;
            var dict = parent as Dictionary<string, object>;
            if (dict == null) return null;

            if (dict.TryGetValue("instance_id", out var idObj))
            {
                var id = System.Convert.ToInt32(idObj);
#pragma warning disable CS0618
                var obj = EditorUtility.InstanceIDToObject(id) as GameObject;
#pragma warning restore CS0618
                return obj?.transform;
            }
            if (dict.TryGetValue("name", out var nameObj))
            {
                var go = FindByName(nameObj?.ToString());
                return go?.transform;
            }
            if (dict.TryGetValue("path", out var pathObj))
            {
                var go = FindByPath(pathObj?.ToString());
                return go?.transform;
            }
            return null;
        }

        public static GameObject[] Find(FindGameObjectPayload payload)
        {
            var list = new List<GameObject>();

            if (payload.InstanceId.HasValue)
            {
#pragma warning disable CS0618
                var obj = EditorUtility.InstanceIDToObject(payload.InstanceId.Value) as GameObject;
#pragma warning restore CS0618
                if (obj != null) list.Add(obj);
                return list.ToArray();
            }

            if (!string.IsNullOrEmpty(payload.Name))
            {
                return FindAllByName(payload.Name);
            }

            if (!string.IsNullOrEmpty(payload.Path))
            {
                var go = FindByPath(payload.Path);
                if (go != null) list.Add(go);
                return list.ToArray();
            }

            if (!string.IsNullOrEmpty(payload.Tag))
            {
                var arr = GameObject.FindGameObjectsWithTag(payload.Tag);
                return arr;
            }

            if (!string.IsNullOrEmpty(payload.Component))
            {
                var type = TypeResolverService.Instance.Resolve(payload.Component);
                if (type != null)
                {
                    var arr = Object.FindObjectsByType(type, FindObjectsSortMode.None)
                        .Select(o => (o as Component)?.gameObject).Where(g => g != null && g.scene.isLoaded).Distinct().ToArray();
                    return arr;
                }
            }

            return list.ToArray();
        }

        public static void Modify(GameObject go, ModifyGameObjectPayload payload)
        {
            if (go == null) return;
            Undo.RecordObject(go, "UACF Modify");

            if (!string.IsNullOrEmpty(payload.Name))
                go.name = payload.Name;
            if (payload.Active.HasValue)
                go.SetActive(payload.Active.Value);
            if (!string.IsNullOrEmpty(payload.Tag))
                go.tag = payload.Tag;
            if (!string.IsNullOrEmpty(payload.LayerName))
            {
                var layer = LayerMask.NameToLayer(payload.LayerName);
                if (layer >= 0) go.layer = layer;
            }
            if (payload.Static.HasValue)
                go.isStatic = payload.Static.Value;
            if (payload.Transform != null)
            {
                if (payload.Transform.Position != null)
                    go.transform.position = ToVector3(payload.Transform.Position);
                if (payload.Transform.Rotation != null)
                    go.transform.rotation = ToQuaternionOrEuler(payload.Transform.Rotation);
                if (payload.Transform.Scale != null)
                    go.transform.localScale = ToVector3(payload.Transform.Scale);
            }
        }

        public static void Destroy(GameObject go, bool destroyChildren = true)
        {
            if (go == null) return;
            Undo.DestroyObjectImmediate(go);
        }

        public static void SetParent(GameObject go, object parent, bool worldPositionStays = true)
        {
            if (go == null) return;
            var parentTransform = ResolveParent(parent);
            Undo.RecordObject(go.transform, "UACF Set Parent");
            go.transform.SetParent(parentTransform, worldPositionStays);
        }

        public static GameObject Duplicate(GameObject go, string newName = null, Vector3? offset = null)
        {
            if (go == null) return null;
            var dup = Object.Instantiate(go);
            dup.name = newName ?? go.name + " (2)";
            if (offset.HasValue)
                dup.transform.position += offset.Value;
            Undo.RegisterCreatedObjectUndo(dup, "UACF Duplicate");
            return dup;
        }

        private static Vector3 ToVector3(Vector3Json v) => v == null ? Vector3.zero : new Vector3(v.X, v.Y, v.Z);
        private static Quaternion ToQuaternion(QuaternionJson q) => q == null ? Quaternion.identity : new Quaternion(q.X, q.Y, q.Z, q.W);
        private static Quaternion ToQuaternionOrEuler(Vector3Json v) => v == null ? Quaternion.identity : Quaternion.Euler(v.X, v.Y, v.Z);

        public class CreateGameObjectPayload
        {
            public string Name;
            public object Parent;
            public bool WorldPositionStays = true;
            public string Tag;
            public string LayerName;
            public int Layer = -1;
            public bool Static;
            public bool Active = true;
            public TransformPayload Transform;
            public List<ComponentPayload> Components;
        }

        public class TransformPayload
        {
            public Vector3Json Position;
            public Vector3Json Rotation; // Euler angles in degrees
            public Vector3Json Scale;
        }

        public class ComponentPayload
        {
            public string Type;
            public Dictionary<string, object> Fields;
        }

        public class FindGameObjectPayload
        {
            public int? InstanceId;
            public string Name;
            public string Path;
            public string Tag;
            public string Component;
        }

        public class ModifyGameObjectPayload
        {
            public string Name;
            public bool? Active;
            public string Tag;
            public string LayerName;
            public bool? Static;
            public TransformPayload Transform;
        }
    }
}
