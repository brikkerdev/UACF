using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UACF.Config;

namespace UACF.Core
{
    /// <summary>Minimal HTTP server over TcpListener. Accepts connections concurrently without HttpListener's response-blocking limitation.</summary>
    internal sealed class TcpHttpServer
    {
        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private volatile bool _isRunning;
        private int _port;
        private readonly Func<string, string, string, string, Stream, Task> _handleRequest;

        public int Port => _port;

        public TcpHttpServer(Func<string, string, string, string, Stream, Task> handleRequest)
        {
            _handleRequest = handleRequest;
        }

        public bool Start(int port)
        {
            if (_isRunning) return false;
            try
            {
                _listener = new TcpListener(IPAddress.Loopback, port);
                _listener.Start();
                _port = ((IPEndPoint)_listener.LocalEndpoint).Port;
                _cts = new CancellationTokenSource();
                _isRunning = true;
                _ = AcceptLoopAsync();
                return true;
            }
            catch (Exception ex)
            {
                UACFLogger.Log($"TcpHttpServer failed to start: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;
            _cts?.Cancel();
            try { _listener?.Stop(); } catch { }
            _listener = null;
        }

        private async Task AcceptLoopAsync()
        {
            while (_isRunning && _listener != null)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client);
                }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    if (_isRunning) UACFLogger.Log($"Accept error: {ex.Message}", LogLevel.Warning);
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                try
                {
                    var (method, path, query, body) = await ParseRequestAsync(stream);
                    if (method == null) return;

                    await _handleRequest(method, path, query, body, stream);
                }
                catch (Exception ex)
                {
                    if (_isRunning) UACFLogger.Log($"Request error: {ex.Message}", LogLevel.Warning);
                    try { await WriteErrorResponse(stream, 500, ex.Message); } catch { }
                }
            }
        }

        private static async Task<(string method, string path, string query, string body)> ParseRequestAsync(NetworkStream stream)
        {
            var buffer = new byte[8192];
            var total = 0;
            while (total < buffer.Length)
            {
                var n = await stream.ReadAsync(buffer, total, buffer.Length - total);
                if (n == 0) return (null, null, null, null);
                total += n;
                var text = Encoding.UTF8.GetString(buffer, 0, total);
                var headerEnd = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                if (headerEnd < 0) continue;

                var headers = text.Substring(0, headerEnd);
                var bodyStart = headerEnd + 4;
                var bodyLength = total - bodyStart;

                var firstLine = headers.Split(new[] { "\r\n" }, StringSplitOptions.None)[0];
                var parts = firstLine.Split(new[] { ' ' }, 3);
                if (parts.Length < 2) return (null, null, null, null);

                var method = parts[0];
                var pathAndQuery = parts[1];
                var qIdx = pathAndQuery.IndexOf('?');
                var path = qIdx >= 0 ? pathAndQuery.Substring(0, qIdx) : pathAndQuery;
                var query = qIdx >= 0 ? pathAndQuery.Substring(qIdx + 1) : "";

                var contentLength = 0;
                foreach (var line in headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(line.Substring(15).Trim(), out contentLength);
                        break;
                    }
                }

                var body = "";
                if (contentLength > 0 && contentLength <= 1024 * 1024)
                {
                    var bodyBytes = new byte[contentLength];
                    var bodyOffset = Math.Min(bodyLength, contentLength);
                    if (bodyOffset > 0)
                        Buffer.BlockCopy(buffer, bodyStart, bodyBytes, 0, bodyOffset);
                    while (bodyOffset < contentLength)
                    {
                        var n1 = await stream.ReadAsync(bodyBytes, bodyOffset, contentLength - bodyOffset);
                        if (n1 == 0) break;
                        bodyOffset += n1;
                    }
                    body = Encoding.UTF8.GetString(bodyBytes, 0, bodyOffset);
                }

                return (method, path, query, body);
            }
            return (null, null, null, null);
        }

        private static async Task WriteErrorResponse(NetworkStream stream, int status, string message)
        {
            var body = $"{{\"success\":false,\"error\":{{\"message\":\"{EscapeJson(message)}\"}}}}";
            await WriteResponseAsync(stream, status, body);
        }

        private static string EscapeJson(string s)
        {
            return s?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") ?? "";
        }

        public static async Task WriteResponseAsync(Stream stream, int statusCode, string jsonBody)
        {
            var body = Encoding.UTF8.GetBytes(jsonBody);
            var statusText = statusCode == 200 ? "OK" : statusCode == 400 ? "Bad Request" : statusCode == 404 ? "Not Found" : statusCode == 409 ? "Conflict" : statusCode == 422 ? "Unprocessable Entity" : statusCode == 500 ? "Internal Server Error" : statusCode == 503 ? "Service Unavailable" : statusCode.ToString();
            var headers = $"HTTP/1.1 {statusCode} {statusText}\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n";
            var headerBytes = Encoding.UTF8.GetBytes(headers);
            await stream.WriteAsync(headerBytes, 0, headerBytes.Length);
            await stream.WriteAsync(body, 0, body.Length);
            stream.Close();
        }
    }
}
