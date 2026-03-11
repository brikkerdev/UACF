using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json;
using UACF.Models;

namespace UACF.Core
{
    public class RequestContext
    {
        private readonly HttpListenerContext _context;
        private readonly string _bodyFromTcp;
        private readonly Stream _responseStream;
        private string _bodyCache;

        public string Method { get; }
        public string Path { get; }
        public string RawPath { get; }
        public Dictionary<string, string> PathParams { get; } = new Dictionary<string, string>();
        public Dictionary<string, string> QueryParams { get; } = new Dictionary<string, string>();
        public int StatusCode { get; private set; } = 200;

        public RequestContext(HttpListenerContext context)
        {
            _context = context;
            _bodyFromTcp = null;
            _responseStream = null;
            AuthHeader = context.Request?.Headers["Authorization"] ?? "";
            var request = context.Request;
            Method = request.HttpMethod;
            RawPath = request.Url?.AbsolutePath ?? "/";
            Path = NormalizePath(RawPath);

            ParseQueryString(request.Url?.Query);
        }

        public string AuthHeader { get; }

        public RequestContext(string method, string rawPath, string queryString, string body, string authHeader, Stream responseStream)
        {
            _context = null;
            _bodyFromTcp = body ?? "";
            _responseStream = responseStream;
            AuthHeader = authHeader ?? "";
            Method = method ?? "GET";
            try { RawPath = string.IsNullOrEmpty(rawPath) ? "/" : Uri.UnescapeDataString(rawPath); } catch { RawPath = rawPath ?? "/"; }
            Path = NormalizePath(RawPath);
            ParseQueryString(queryString);
        }


        public void SetPathParams(Dictionary<string, string> pathParams)
        {
            PathParams.Clear();
            foreach (var kv in pathParams)
                PathParams[kv.Key] = kv.Value;
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "/";
            path = path.TrimEnd('/');
            if (string.IsNullOrEmpty(path)) return "/";
            return path.StartsWith("/") ? path : "/" + path;
        }

        private void ParseQueryString(string query)
        {
            if (string.IsNullOrEmpty(query)) return;
            if (query.StartsWith("?")) query = query.Substring(1);
            foreach (var pair in query.Split('&'))
            {
                var idx = pair.IndexOf('=');
                if (idx >= 0)
                {
                    var key = Uri.UnescapeDataString(pair.Substring(0, idx));
                    var value = Uri.UnescapeDataString(pair.Substring(idx + 1));
                    QueryParams[key] = value;
                }
            }
        }

        public async Task<string> ReadBodyRawAsync()
        {
            if (_bodyCache != null) return _bodyCache;
            if (_bodyFromTcp != null) return _bodyCache = _bodyFromTcp;
            if (_context?.Request?.InputStream == null) return _bodyCache = "";
            using (var reader = new StreamReader(_context.Request.InputStream, Encoding.UTF8))
            {
                _bodyCache = await reader.ReadToEndAsync();
            }
            return _bodyCache;
        }

        public async Task<T> ReadBodyAsync<T>()
        {
            var json = await ReadBodyRawAsync();
            if (string.IsNullOrWhiteSpace(json)) return default;
            return JsonConvert.DeserializeObject<T>(json);
        }

        public void SetResponseHeader(string name, string value)
        {
            if (_context?.Response?.Headers != null)
                _context.Response.Headers[name] = value;
        }

        public void Respond(int statusCode, object body)
        {
            StatusCode = statusCode;
            var json = body is string s ? s : JsonConvert.SerializeObject(body);
            var bytes = Encoding.UTF8.GetBytes(json);

            if (_responseStream != null)
            {
                var statusText = statusCode == 200 ? "OK" : statusCode == 400 ? "Bad Request" : statusCode == 404 ? "Not Found" : statusCode == 409 ? "Conflict" : statusCode == 422 ? "Unprocessable Entity" : statusCode == 500 ? "Internal Server Error" : statusCode == 503 ? "Service Unavailable" : statusCode.ToString();
                var headers = $"HTTP/1.1 {statusCode} {statusText}\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n";
                var headerBytes = Encoding.UTF8.GetBytes(headers);
                _responseStream.Write(headerBytes, 0, headerBytes.Length);
                _responseStream.Write(bytes, 0, bytes.Length);
                _responseStream.Close();
            }
            else
            {
                var response = _context.Response;
                response.StatusCode = statusCode;
                response.ContentType = "application/json; charset=utf-8";
                response.ContentEncoding = Encoding.UTF8;
                response.ContentLength64 = bytes.Length;
                response.OutputStream.Write(bytes, 0, bytes.Length);
                response.OutputStream.Close();
            }
        }

        public void RespondOk(object data, long durationMs = 0)
        {
            var response = ApiResponse.Ok(data, durationMs);
            Respond(200, response);
        }

        public void RespondError(int statusCode, ErrorCode code, string message, object details = null, long durationMs = 0)
        {
            var response = ApiResponse.Fail(code, message, details, durationMs);
            Respond(statusCode, response);
        }
    }
}
