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
                _listener.Prefixes.Add($"http://localhost:{port}/");
                _listener.Start();

                _cts = new CancellationTokenSource();
                _isRunning = true;
                _listenerThread = new Thread(ListenLoop) { IsBackground = true };
                _listenerThread.Start();

                UACFLogger.Log($"UACF server started on http://localhost:{port}/");
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

            try
            {
                await _router.Route(context);
                statusCode = context.Response.StatusCode;
            }
            catch (Exception ex)
            {
                UACFLogger.LogError(context.Request.HttpMethod, context.Request.Url?.AbsolutePath ?? "/", ex.Message, sw.ElapsedMilliseconds);
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
                    UACFLogger.LogError(context.Request.HttpMethod, context.Request.Url?.AbsolutePath ?? "/", $"Status {statusCode}", sw.ElapsedMilliseconds);
                else
                    UACFLogger.LogRequest(context.Request.HttpMethod, context.Request.Url?.AbsolutePath ?? "/", statusCode, sw.ElapsedMilliseconds);
            }
        }
    }
}
