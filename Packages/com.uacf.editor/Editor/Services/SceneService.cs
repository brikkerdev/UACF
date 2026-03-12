using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UACF.Config;
using UACF.Core;
using UACF.Models;

namespace UACF.Services
{
    public class SceneService
    {
        public static SceneInfo[] GetLoadedScenes()
        {
            var list = new List<SceneInfo>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                list.Add(new SceneInfo
                {
                    Name = scene.name,
                    Path = scene.path,
                    IsLoaded = scene.isLoaded,
                    IsDirty = scene.isDirty,
                    IsActive = scene == SceneManager.GetActiveScene(),
                    RootCount = scene.rootCount,
                    BuildIndex = scene.buildIndex
                });
            }
            return list.ToArray();
        }

        public static bool OpenScene(string path, string mode = "Single")
        {
            try
            {
                var openMode = mode == "Additive" ? OpenSceneMode.Additive : OpenSceneMode.Single;
                var scene = EditorSceneManager.OpenScene(path, openMode);
                return scene.IsValid();
            }
            catch (System.Exception ex)
            {
                UACFLogger.Log($"OpenScene failed: {path} - {ex.Message}", LogLevel.Warning);
                return false;
            }
        }

        public static bool SaveScene(string path = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(path))
                {
                    var scene = SceneManager.GetSceneByPath(path);
                    if (scene.IsValid())
                        return EditorSceneManager.SaveScene(scene);
                }
                return EditorSceneManager.SaveOpenScenes();
            }
            catch (System.Exception ex)
            {
                UACFLogger.Log($"SaveScene failed: {path ?? "(all)"} - {ex.Message}", LogLevel.Warning);
                return false;
            }
        }

        public static bool NewScene(string path, string template = "default")
        {
            try
            {
                var setup = template == "empty"
                    ? NewSceneSetup.EmptyScene
                    : NewSceneSetup.DefaultGameObjects;
                var scene = EditorSceneManager.NewScene(setup, NewSceneMode.Single);
                if (!scene.IsValid()) return false;
                if (!string.IsNullOrEmpty(path))
                    return EditorSceneManager.SaveScene(scene, path);
                return true;
            }
            catch (System.Exception ex)
            {
                UACFLogger.Log($"NewScene failed: {ex.Message}", LogLevel.Warning);
                return false;
            }
        }
    }
}
