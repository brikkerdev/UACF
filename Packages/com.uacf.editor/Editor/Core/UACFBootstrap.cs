using System;
using UnityEditor;
using UnityEditor.Compilation;
using UACF.Config;
using UACF.Handlers;

namespace UACF.Core
{
    [InitializeOnLoad]
    public static class UACFBootstrap
    {
        private static UACFServer _server;
        private static RequestRouter _router;
        private const string PortKey = "UACF_Port";

        static UACFBootstrap()
        {
            EditorApplication.delayCall += Initialize;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            EditorApplication.quitting += OnQuitting;
        }

        private static void Initialize()
        {
            if (_server != null) return;

            _router = new RequestRouter();
            RegisterAllRoutes(_router);

            _server = new UACFServer(_router);

            if (UACFSettings.instance.AutoStart)
            {
                var port = SessionState.GetInt(PortKey, UACFSettings.instance.Port);
                if (port == 0) port = UACFSettings.instance.Port;
                if (_server.Start(port))
                    SessionState.SetInt(PortKey, _server.Port);
            }
        }

        private static void RegisterAllRoutes(RequestRouter router)
        {
            StatusHandler.Register(router);
            AssetsHandler.Register(router);
            CompileHandler.Register(router);
            FileHandler.Register(router);
            SceneHandler.Register(router);
            GameObjectHandler.Register(router);
            ComponentHandler.Register(router);
            PrefabHandler.Register(router);
            ProjectHandler.Register(router);
            EditorHandler.Register(router);
            BatchHandler.Register(router);
        }

        private static void OnBeforeAssemblyReload()
        {
            _server?.Stop();
        }

        private static void OnAfterAssemblyReload()
        {
            _server = null;
            _router = null;
            EditorApplication.delayCall += Initialize;
        }

        private static void OnQuitting()
        {
            _server?.Stop();
        }

        public static UACFServer GetServer() => _server;
    }
}
