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
    public static class EditorHandler
    {
        public static void Register(ActionDispatcher dispatcher)
        {
            dispatcher.Register("editor.compilationStatus", HandleCompilationStatus);
            dispatcher.Register("editor.screenshot", HandleScreenshot);
            dispatcher.Register("editor.play", HandlePlay);
            dispatcher.Register("editor.stop", HandleStop);
            dispatcher.Register("editor.pause", HandlePause);
            dispatcher.Register("editor.step", HandleStep);
            dispatcher.Register("editor.playState", HandlePlayState);
            dispatcher.Register("editor.undo", HandleUndo);
            dispatcher.Register("editor.redo", HandleRedo);
            dispatcher.Register("editor.undoHistory", HandleUndoHistory);
            dispatcher.Register("editor.select", HandleSelect);
            dispatcher.Register("editor.selection", HandleSelection);
            dispatcher.Register("editor.focus", HandleFocus);
        }

        private static Task<UacfResponse> HandleCompilationStatus(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var svc = CompilationService.Instance;
                return UacfResponse.Success(new
                {
                    isCompiling = EditorApplication.isCompiling,
                    hasErrors = svc.LastErrors.Count > 0,
                    errors = svc.LastErrors.Select(e => new { file = e.File, line = e.Line, column = e.Column, message = e.Message, severity = "error" }).ToArray(),
                    warnings = svc.LastWarnings.Select(w => new { file = w.File, line = w.Line, message = w.Message, severity = "warning" }).ToArray()
                }, 0);
            });
        }

        private static Task<UacfResponse> HandleScreenshot(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var view = p["view"]?.ToString() ?? "scene";
                var width = p["width"]?.Value<int>() ?? 1920;
                var height = p["height"]?.Value<int>() ?? 1080;

                var cam = Camera.main;
                if (p["camera"] != null)
                {
                    var camName = p["camera"]?.ToString();
                    var camObj = GameObject.Find(camName);
                    if (camObj != null) cam = camObj.GetComponent<Camera>();
                }

                RenderTexture rt = null;
                Camera targetCam = null;

                if (view == "game" && cam != null)
                {
                    rt = new RenderTexture(width, height, 24);
                    cam.targetTexture = rt;
                    cam.Render();
                    cam.targetTexture = null;
                    targetCam = cam;
                }
                else
                {
                    var sceneView = SceneView.lastActiveSceneView;
                    if (sceneView != null && sceneView.camera != null)
                    {
                        rt = new RenderTexture(width, height, 24);
                        sceneView.camera.targetTexture = rt;
                        sceneView.camera.Render();
                        sceneView.camera.targetTexture = null;
                        targetCam = sceneView.camera;
                    }
                }

                if (rt == null)
                    return UacfResponse.Fail("SCREENSHOT_FAILED", "No camera available", null, 0);

                var tex = new Texture2D(width, height);
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                rt.Release();

                var bytes = tex.EncodeToPNG();
                Object.DestroyImmediate(tex);
                var base64 = System.Convert.ToBase64String(bytes);

                return UacfResponse.Success(new { base64, format = "png", width, height }, 0);
            });
        }

        private static Task<UacfResponse> HandlePlay(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                EditorApplication.isPlaying = true;
                return UacfResponse.Success(null, 0);
            });
        }

        private static Task<UacfResponse> HandleStop(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                EditorApplication.isPlaying = false;
                return UacfResponse.Success(null, 0);
            });
        }

        private static Task<UacfResponse> HandlePause(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                EditorApplication.isPaused = !EditorApplication.isPaused;
                return UacfResponse.Success(new { paused = EditorApplication.isPaused }, 0);
            });
        }

        private static Task<UacfResponse> HandleStep(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                EditorApplication.Step();
                return UacfResponse.Success(null, 0);
            });
        }

        private static Task<UacfResponse> HandlePlayState(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var state = EditorApplication.isPlaying ? (EditorApplication.isPaused ? "paused" : "playing") : "stopped";
                return UacfResponse.Success(new
                {
                    state,
                    time = EditorApplication.timeSinceStartup,
                    frameCount = Time.frameCount
                }, 0);
            });
        }

        private static Task<UacfResponse> HandleUndo(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                Undo.PerformUndo();
                return UacfResponse.Success(null, 0);
            });
        }

        private static Task<UacfResponse> HandleRedo(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                Undo.PerformRedo();
                return UacfResponse.Success(null, 0);
            });
        }

        private static Task<UacfResponse> HandleUndoHistory(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                return UacfResponse.Success(new { message = "Undo history not exposed by Unity API" }, 0);
            });
        }

        private static Task<UacfResponse> HandleSelect(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var obj = p["object"]?.ToString();
                var objects = p["objects"] as JArray;

                if (objects != null)
                {
                    var gos = objects.Select(t => GameObject.Find(t.ToString())).Where(g => g != null).ToArray();
                    Selection.objects = gos;
                }
                else if (!string.IsNullOrEmpty(obj))
                {
                    var go = GameObject.Find(obj);
                    if (go != null)
                        Selection.activeGameObject = go;
                }

                return UacfResponse.Success(null, 0);
            });
        }

        private static Task<UacfResponse> HandleSelection(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var objects = Selection.gameObjects.Select(g => new { name = g.name, instanceId = g.GetInstanceID() }).ToArray();
                return UacfResponse.Success(new { selection = objects }, 0);
            });
        }

        private static Task<UacfResponse> HandleFocus(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var obj = p["object"]?.ToString();
                if (string.IsNullOrEmpty(obj))
                    return UacfResponse.Fail("INVALID_REQUEST", "object is required", null, 0);

                var go = GameObject.Find(obj);
                if (go == null)
                    return UacfResponse.Fail("OBJECT_NOT_FOUND", "Object not found", null, 0);

                Selection.activeGameObject = go;
                SceneView.lastActiveSceneView?.FrameSelected();
                return UacfResponse.Success(null, 0);
            });
        }
    }
}
