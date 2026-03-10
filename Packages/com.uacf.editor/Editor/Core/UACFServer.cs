using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UACF.Config;

namespace UACF.Core
{
    public class UACFServer
    {
        private HttpListener _listener;
        private Thread _listenerThread;
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
                _listener = new HttpListener();
                // Use 127.0.0.1 explicitly: on Linux, "localhost" may bind to IPv6 only,
                // causing curl/agents to fail (IPv4 timeout) or Bad Request (IPv6 URL parsing).
                _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                _listener.Start();

                _cts = new CancellationTokenSource();
                _isRunning = true;
                _listenerThread = new Thread(ListenLoop) { IsBackground = true };
                _listenerThread.Start();

                UACFLogger.Log($"UACF server started on http://127.0.0.1:{port}/");
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

            try
            {
                _listener?.Stop();
                _listener?.Close();
            }
            catch (Exception ex)
            {
                UACFLogger.Log($"Error stopping server: {ex.Message}", LogLevel.Warning);
            }

            _listener = null;
            UACFLogger.Log("UACF server stopped");
        }

        private void ListenLoop()
        {
            var timeout = UACFSettings.instance.RequestTimeoutSeconds * 1000;

            while (_isRunning && _listener != null && _listener.IsListening)
            {
                try
                {
                    var contextTask = _listener.GetContextAsync();
                    if (contextTask.Wait(timeout, _cts.Token))
                    {
                        var context = contextTask.Result;
                        ThreadPool.QueueUserWorkItem(_ =>
                        {
                            HandleRequest(context);
                        });
                    }
                }
                catch (HttpListenerException)
                {
                    if (!_isRunning) break;
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                        UACFLogger.Log($"Listener error: {ex.Message}", LogLevel.Error);
                }
            }
        }

        private async void HandleRequest(HttpListenerContext context)
        {
            var sw = Stopwatch.StartNew();
            var statusCode = 500;
            var path = context.Request.Url?.AbsolutePath ?? "/";
            var timeoutMs = UACFSettings.instance.RequestTimeoutSeconds * 1000;

            try
            {
                var routeTask = _router.Route(context);
                var timeoutTask = Task.Delay(timeoutMs);
                var completed = await Task.WhenAny(routeTask, timeoutTask);

                if (completed == timeoutTask)
                {
                    UACFLogger.Log($"Request timeout ({timeoutMs}ms) - editor main thread may be blocked", LogLevel.Warning);
                    statusCode = 503;
                    try
                    {
                        context.Response.StatusCode = 503;
                        context.Response.ContentType = "application/json; charset=utf-8";
                        var body = System.Text.Encoding.UTF8.GetBytes("{\"success\":false,\"error\":{\"code\":\"EDITOR_BUSY\",\"message\":\"Request timed out - editor main thread may be blocked (compilation, modal dialog, etc.)\"}}");
                        context.Response.ContentLength64 = body.Length;
                        context.Response.OutputStream.Write(body, 0, body.Length);
                        context.Response.Close();
                    }
                    catch (Exception ex)
                    {
                        UACFLogger.Log($"Failed to send timeout response: {ex.Message}", LogLevel.Warning);
                    }
                    return;
                }

                await routeTask;
                statusCode = context.Response.StatusCode;
            }
            catch (Exception ex)
            {
                UACFLogger.LogError(context.Request.HttpMethod, path, ex.Message, sw.ElapsedMilliseconds);
                try
                {
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                }
                catch { }
            }
            finally
            {
                sw.Stop();
                if (statusCode >= 500)
                    UACFLogger.LogError(context.Request.HttpMethod, path, $"Status {statusCode}", sw.ElapsedMilliseconds);
                else
                    UACFLogger.LogRequest(context.Request.HttpMethod, path, statusCode, sw.ElapsedMilliseconds);
            }
        }
    }
}
