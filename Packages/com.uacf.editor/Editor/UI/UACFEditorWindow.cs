using UnityEditor;
using UnityEngine;
using UACF.Core;
using UACF.Handlers;

namespace UACF.UI
{
    public class UACFEditorWindow : EditorWindow
    {
        [MenuItem("Window/UACF/Status")]
        public static void ShowWindow()
        {
            GetWindow<UACFEditorWindow>("UACF Status");
        }

        private void OnGUI()
        {
            var server = UACFBootstrap.GetServer();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("UACF Server Status", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (server == null)
            {
                EditorGUILayout.HelpBox("Server not initialized.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Status:", server.IsRunning ? "Running" : "Stopped");
            EditorGUILayout.LabelField("Port:", server.Port.ToString());
            EditorGUILayout.LabelField("URL:", $"http://127.0.0.1:{server.Port}/");
            EditorGUILayout.LabelField("Uptime:", $"{UACFServer.UptimeSeconds} seconds");

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Open in Browser"))
            {
                Application.OpenURL($"http://localhost:{server.Port}/api/status");
            }
        }
    }
}
