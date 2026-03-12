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
        private static ActionDispatcher _dispatcher;
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

            _dispatcher = new ActionDispatcher();
            var dispatcher = _dispatcher;
            ApiHandler.Register(dispatcher);
            SceneHandler.Register(dispatcher);
            GameObjectHandler.Register(dispatcher);
            ComponentHandler.Register(dispatcher);
            ExecuteHandler.Register(dispatcher);
            AssetHandler.Register(dispatcher);
            PrefabHandler.Register(dispatcher);
            ConsoleHandler.Register(dispatcher);
            EditorHandler.Register(dispatcher);
            ProjectHandler.Register(dispatcher);
            RuntimeHandler.Register(dispatcher);
            TestHandler.Register(dispatcher);
            BatchHandler.Register(dispatcher);

            var uacfHandler = new UacfEndpointHandler(dispatcher);
            _router = new RequestRouter(uacfHandler);

            _server = new UACFServer(_router);

            if (UACFSettings.instance.AutoStart)
            {
                var config = UACFConfig.Instance;
                var port = SessionState.GetInt(PortKey, config.Port);
                if (port == 0) port = config.Port;
                if (_server.Start(port))
                    SessionState.SetInt(PortKey, _server.Port);
            }
        }

        private static void OnBeforeAssemblyReload()
        {
            _server?.Stop();
        }

        private static void OnAfterAssemblyReload()
        {
            _server = null;
            _router = null;
            _dispatcher = null;
            EditorApplication.delayCall += Initialize;
        }

        private static void OnQuitting()
        {
            _server?.Stop();
        }

        public static UACFServer GetServer() => _server;
        public static ActionDispatcher GetDispatcher() => _dispatcher;
    }
}
