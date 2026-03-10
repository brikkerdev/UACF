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
        private string _bodyCache;

        public string Method { get; }
        public string Path { get; }
        public string RawPath { get; }
        public Dictionary<string, string> PathParams { get; } = new Dictionary<string, string>();
        public Dictionary<string, string> QueryParams { get; } = new Dictionary<string, string>();

        public RequestContext(HttpListenerContext context)
        {
            _context = context;
            var request = context.Request;
            Method = request.HttpMethod;
            RawPath = request.Url?.AbsolutePath ?? "/";
            Path = NormalizePath(RawPath);

            ParseQueryString(request.Url?.Query);
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
            if (_context.Request.InputStream == null) return "";
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
            _context.Response.Headers[name] = value;
        }

        public void Respond(int statusCode, object body)
        {
            var response = _context.Response;
            response.StatusCode = statusCode;
            response.ContentType = "application/json; charset=utf-8";
            response.ContentEncoding = Encoding.UTF8;

            var json = body is string s ? s : JsonConvert.SerializeObject(body);
            var bytes = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.OutputStream.Close();
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
