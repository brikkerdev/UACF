using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
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
            var openMode = mode == "Additive" ? OpenSceneMode.Additive : OpenSceneMode.Single;
            var scene = EditorSceneManager.OpenScene(path, openMode);
            return scene.IsValid();
        }

        public static bool SaveScene(string path = null)
        {
            if (!string.IsNullOrEmpty(path))
            {
                var scene = SceneManager.GetSceneByPath(path);
                if (scene.IsValid())
                    return EditorSceneManager.SaveScene(scene);
            }
            return EditorSceneManager.SaveOpenScenes();
        }

        public static bool NewScene(string path, string template = "default")
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
    }
}
