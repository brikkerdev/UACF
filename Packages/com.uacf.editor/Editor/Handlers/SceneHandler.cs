using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UACF.Core;
using UACF.Models;
using UACF.Services;

namespace UACF.Handlers
{
    public static class SceneHandler
    {
        public static void Register(ActionDispatcher dispatcher)
        {
            dispatcher.Register("scene.hierarchy.get", HandleHierarchyGet);
            dispatcher.Register("scene.open", HandleOpen);
            dispatcher.Register("scene.new", HandleNew);
            dispatcher.Register("scene.save", HandleSave);
            dispatcher.Register("scene.list", HandleList);
            dispatcher.Register("scene.buildSettings.get", HandleBuildSettingsGet);
            dispatcher.Register("scene.buildSettings.add", HandleBuildSettingsAdd);
            dispatcher.Register("scene.buildSettings.remove", HandleBuildSettingsRemove);
            dispatcher.Register("scene.validate", HandleValidate);
        }

        private static Task<UacfResponse> HandleHierarchyGet(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                if (EditorApplication.isPlaying)
                    return UacfResponse.Fail("CONFLICT", "Cannot get hierarchy in Play Mode", "Exit Play Mode first", 0);

                var scene = SceneManager.GetActiveScene();
                if (!scene.IsValid())
                    return UacfResponse.Fail("SCENE_NOT_LOADED", "No active scene", null, 0);

                var depth = p["depth"]?.Value<int>() ?? -1;
                var includeComponents = p["components"]?.Value<bool>() ?? false;
                var filter = p["filter"]?.ToString();
                var tag = p["tag"]?.ToString();
                var layer = p["layer"]?.ToString();

                var roots = SerializationService.SerializeHierarchy(scene, depth, includeComponents);
                var rootObjects = roots.Select(r => ToSpecFormat(r, filter, tag, layer)).Where(x => x != null).ToArray();

                return UacfResponse.Success(new
                {
                    sceneName = scene.name,
                    scenePath = scene.path,
                    isDirty = scene.isDirty,
                    rootObjects
                }, 0);
            });
        }

        private static object ToSpecFormat(Models.GameObjectInfo info, string filter, string tag, string layer)
        {
            if (!string.IsNullOrEmpty(filter) && info.Name?.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) < 0) return null;
            if (!string.IsNullOrEmpty(tag) && info.Tag != tag) return null;
            if (!string.IsNullOrEmpty(layer) && info.LayerName != layer) return null;

            return new
            {
                name = info.Name,
                instanceId = info.InstanceId,
                active = info.Active,
                tag = info.Tag,
                layer = info.LayerName ?? LayerMask.LayerToName(info.Layer),
                components = info.Components ?? new string[0],
                children = (info.Children ?? new Models.GameObjectInfo[0])
                    .Select(c => ToSpecFormat(c, filter, tag, layer))
                    .Where(x => x != null)
                    .ToArray()
            };
        }

        private static Task<UacfResponse> HandleOpen(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                if (EditorApplication.isPlaying)
                    return UacfResponse.Fail("CONFLICT", "Exit Play Mode first", null, 0);

                var path = p["path"]?.ToString();
                if (string.IsNullOrEmpty(path))
                    return UacfResponse.Fail("INVALID_REQUEST", "path is required", null, 0);

                var mode = p["mode"]?.ToString() ?? "single";
                var ok = SceneService.OpenScene(path, mode);
                return UacfResponse.Success(new { opened = ok, path }, 0);
            });
        }

        private static Task<UacfResponse> HandleNew(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                if (EditorApplication.isPlaying)
                    return UacfResponse.Fail("CONFLICT", "Exit Play Mode first", null, 0);

                var setup = p["setup"]?.ToString() ?? "empty";
                var template = setup == "default" ? "default" : "empty";
                var ok = SceneService.NewScene(null, template);
                return UacfResponse.Success(new { created = ok }, 0);
            });
        }

        private static Task<UacfResponse> HandleSave(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                if (EditorApplication.isPlaying)
                    return UacfResponse.Fail("CONFLICT", "Exit Play Mode first", null, 0);

                var path = p["path"]?.ToString();
                var ok = SceneService.SaveScene(path);
                return UacfResponse.Success(new { saved = ok }, 0);
            });
        }

        private static Task<UacfResponse> HandleList(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var scenes = SceneService.GetLoadedScenes();
                return UacfResponse.Success(new
                {
                    scenes = scenes.Select(s => new { name = s.Name, path = s.Path, isActive = s.IsActive, isDirty = s.IsDirty }).ToArray()
                }, 0);
            });
        }

        private static Task<UacfResponse> HandleBuildSettingsGet(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var scenes = EditorBuildSettings.scenes;
                return UacfResponse.Success(new
                {
                    scenes = scenes.Select(s => new { path = s.path, enabled = s.enabled }).ToArray()
                }, 0);
            });
        }

        private static Task<UacfResponse> HandleBuildSettingsAdd(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                if (EditorApplication.isPlaying)
                    return UacfResponse.Fail("CONFLICT", "Exit Play Mode first", null, 0);

                var path = p["path"]?.ToString();
                if (string.IsNullOrEmpty(path))
                    return UacfResponse.Fail("INVALID_REQUEST", "path is required", null, 0);

                var list = EditorBuildSettings.scenes.ToList();
                if (list.Any(s => s.path == path))
                    return UacfResponse.Success(new { added = false, message = "Already in Build Settings" }, 0);

                list.Add(new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = list.ToArray();
                return UacfResponse.Success(new { added = true }, 0);
            });
        }

        private static Task<UacfResponse> HandleBuildSettingsRemove(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var path = p["path"]?.ToString();
                if (string.IsNullOrEmpty(path))
                    return UacfResponse.Fail("INVALID_REQUEST", "path is required", null, 0);

                var list = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
                EditorBuildSettings.scenes = list.ToArray();
                return UacfResponse.Success(new { removed = true }, 0);
            });
        }

        private static Task<UacfResponse> HandleValidate(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var issues = new List<object>();
                var roots = SceneManager.GetActiveScene().GetRootGameObjects();
                foreach (var root in roots)
                    ValidateRecursive(root, issues);

                return UacfResponse.Success(new { issues }, 0);
            });
        }

        private static void ValidateRecursive(GameObject go, List<object> issues)
        {
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null)
                    issues.Add(new { severity = "error", message = $"Missing script on '{go.name}'", @object = go.name });
            }
            for (int i = 0; i < go.transform.childCount; i++)
                ValidateRecursive(go.transform.GetChild(i).gameObject, issues);
        }
    }
}
