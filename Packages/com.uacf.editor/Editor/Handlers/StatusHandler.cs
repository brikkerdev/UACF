using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UACF.Core;
using UACF.Models;

namespace UACF.Handlers
{
    public static class StatusHandler
    {
        public static void Register(RequestRouter router)
        {
            router.Register("GET", "/api/status", HandleStatus);
        }

        private static async System.Threading.Tasks.Task HandleStatus(RequestContext ctx)
        {
            var data = await MainThreadDispatcher.Enqueue(() =>
            {
                var scenes = new string[SceneManager.sceneCount];
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    scenes[i] = scene.path;
                }

                var activeScene = SceneManager.GetActiveScene();

                return new
                {
                    server_version = "1.0.0",
                    unity_version = Application.unityVersion,
                    project_name = System.IO.Path.GetFileNameWithoutExtension(Application.dataPath),
                    project_path = System.IO.Path.GetDirectoryName(Application.dataPath),
                    is_compiling = EditorApplication.isCompiling,
                    is_playing = EditorApplication.isPlaying,
                    active_scene = activeScene.path,
                    loaded_scenes = scenes,
                    uptime_seconds = UACFServer.UptimeSeconds
                };
            });

            ctx.RespondOk(data);
        }
    }
}
