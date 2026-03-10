using System.Threading.Tasks;
using UnityEditor;
using UACF.Core;

namespace UACF.Handlers
{
    public static class EditorHandler
    {
        public static void Register(RequestRouter router)
        {
            router.Register("POST", "/api/editor/play", HandlePlay);
            router.Register("POST", "/api/editor/stop", HandleStop);
            router.Register("POST", "/api/editor/pause", HandlePause);
        }

        private static async Task HandlePlay(RequestContext ctx)
        {
            await MainThreadDispatcher.Enqueue(() => EditorApplication.isPlaying = true);
            ctx.RespondOk(new { is_playing = true });
        }

        private static async Task HandleStop(RequestContext ctx)
        {
            await MainThreadDispatcher.Enqueue(() => EditorApplication.isPlaying = false);
            ctx.RespondOk(new { is_playing = false });
        }

        private static async Task HandlePause(RequestContext ctx)
        {
            await MainThreadDispatcher.Enqueue(() => EditorApplication.isPaused = !EditorApplication.isPaused);
            ctx.RespondOk(new { is_paused = EditorApplication.isPaused });
        }
    }
}
