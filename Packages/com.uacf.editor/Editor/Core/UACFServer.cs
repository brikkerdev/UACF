using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UACF.Config;
using UACF.Models;
using Unity.Plastic.Newtonsoft.Json;

namespace UACF.Core
{
    public class UACFServer
    {
        private TcpHttpServer _tcpServer;
        private CancellationTokenSource _cts;
        private volatile bool _isRunning;
        private int _port;
        private readonly RequestRouter _router;
        private static readonly Stopwatch _uptimeWatch = new Stopwatch();

        public bool IsRunning => _isRunning;
        public int Port => _port;
        public static long UptimeSeconds => _uptimeWatch.ElapsedMilliseconds / 1000;

        public UACFServer(RequestRouter router)
        {
            _router = router;
        }

        public bool Start(int port)
        {
            if (_isRunning) return true;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                var tryPort = port + attempt;
                if (TryStartListener(tryPort))
                {
                    _port = tryPort;
                    _uptimeWatch.Restart();
                    return true;
                }
            }
            return false;
        }

        private bool TryStartListener(int port)
        {
            try
            {
                _tcpServer = new TcpHttpServer(HandleRequestAsync);
                if (!_tcpServer.Start(port))
                    return false;

                _port = _tcpServer.Port;
                _cts = new CancellationTokenSource();
                _isRunning = true;

                UACFLogger.Log($"UACF server started on http://127.0.0.1:{_port}/");
                return true;
            }
            catch (Exception ex)
            {
                UACFLogger.Log($"Failed to start on port {port}: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _cts?.Cancel();
            _tcpServer?.Stop();
            _tcpServer = null;
            UACFLogger.Log("UACF server stopped");
        }

        private async Task HandleRequestAsync(string method, string path, string query, string body, Stream responseStream)
        {
            var sw = Stopwatch.StartNew();
            var statusCode = 500;
            var timeoutMs = UACFSettings.instance.RequestTimeoutSeconds * 1000;

            try
            {
                var ctx = new RequestContext(method, path, query, body, responseStream);
                var routeTask = _router.Route(ctx);
                var timeoutTask = Task.Delay(timeoutMs);
                var completed = await Task.WhenAny(routeTask, timeoutTask);

                if (completed == timeoutTask)
                {
                    UACFLogger.Log($"Request timeout ({timeoutMs}ms) - editor main thread may be blocked", LogLevel.Warning);
                    statusCode = 503;
                    var errBody = JsonConvert.SerializeObject(ApiResponse.Fail(ErrorCode.SERVER_BUSY, "Request timed out - editor main thread may be blocked", null, 0));
                    await TcpHttpServer.WriteResponseAsync(responseStream, 503, errBody);
                    return;
                }

                await routeTask;
                statusCode = ctx.StatusCode;
            }
            catch (Exception ex)
            {
                UACFLogger.LogError(method, path ?? "/", ex.Message, sw.ElapsedMilliseconds);
                try
                {
                    var errBody = JsonConvert.SerializeObject(ApiResponse.Fail(ErrorCode.INTERNAL_ERROR, ex.Message, null, 0));
                    await TcpHttpServer.WriteResponseAsync(responseStream, 500, errBody);
                }
                catch { }
            }
            finally
            {
                sw.Stop();
                if (statusCode >= 500)
                    UACFLogger.LogError(method, path ?? "/", $"Status {statusCode}", sw.ElapsedMilliseconds);
                else
                    UACFLogger.LogRequest(method, path ?? "/", statusCode, sw.ElapsedMilliseconds);
            }
        }
    }
}
