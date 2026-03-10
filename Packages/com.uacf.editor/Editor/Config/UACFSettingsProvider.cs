using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UACF.Config
{
    public class UACFSettingsProvider : SettingsProvider
    {
        private SerializedObject _serialized;

        public UACFSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _serialized = new SerializedObject(UACFSettings.instance);
        }

        public override void OnGUI(string searchContext)
        {
            if (_serialized == null) return;

            _serialized.Update();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("UACF - Unity Autonomous Control Framework", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(_serialized.FindProperty("_port"), new GUIContent("Port"));
            EditorGUILayout.PropertyField(_serialized.FindProperty("_autoStart"), new GUIContent("Auto Start"));
            EditorGUILayout.PropertyField(_serialized.FindProperty("_logRequests"), new GUIContent("Log Requests"));
            EditorGUILayout.PropertyField(_serialized.FindProperty("_requestTimeoutSeconds"), new GUIContent("Request Timeout (s)"));
            EditorGUILayout.PropertyField(_serialized.FindProperty("_compileTimeoutSeconds"), new GUIContent("Compile Timeout (s)"));
            EditorGUILayout.PropertyField(_serialized.FindProperty("_enableBatchEndpoint"), new GUIContent("Enable Batch Endpoint"));

            _serialized.ApplyModifiedProperties();
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new UACFSettingsProvider("Project/UACF", SettingsScope.Project)
            {
                keywords = new[] { "UACF", "API", "Editor", "HTTP" }
            };
        }
    }
}
