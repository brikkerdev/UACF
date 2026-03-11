using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;

namespace UACF.Config
{
    public class UACFConfig
    {
        public int Port { get; set; } = 6400;
        public string Host { get; set; } = "127.0.0.1";
        public string Token { get; set; }
        public bool AllowExecute { get; set; } = true;
        public bool LogRequests { get; set; } = true;
        public string LogFile { get; set; } = "Logs/UACF/session.log";

        private static UACFConfig _instance;
        private static string _configPath;

        public static UACFConfig Instance
        {
            get
            {
                if (_instance == null)
                    Load();
                return _instance;
            }
        }

        private static string GetConfigPath()
        {
            if (_configPath != null) return _configPath;
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            _configPath = Path.Combine(projectRoot, "ProjectSettings", "UACF", "config.json");
            return _configPath;
        }

        public static void Load()
        {
            var path = GetConfigPath();
            _instance = new UACFConfig();

            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var jo = JObject.Parse(json);
                    if (jo["port"] != null) _instance.Port = jo["port"].Value<int>();
                    if (jo["host"] != null) _instance.Host = jo["host"].Value<string>();
                    if (jo["token"] != null) _instance.Token = jo["token"].Value<string>();
                    if (jo["allowExecute"] != null) _instance.AllowExecute = jo["allowExecute"].Value<bool>();
                    if (jo["logRequests"] != null) _instance.LogRequests = jo["logRequests"].Value<bool>();
                    if (jo["logFile"] != null) _instance.LogFile = jo["logFile"].Value<string>();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[UACF] Failed to load config: {ex.Message}");
                }
            }

            if (string.IsNullOrEmpty(_instance.Token))
            {
                _instance.Token = GenerateToken();
                Save(_instance);
                UnityEngine.Debug.Log($"[UACF] Auth token (first run): {_instance.Token}");
            }
        }

        public static void Save(UACFConfig config = null)
        {
            var cfg = config ?? _instance ?? new UACFConfig();
            var path = GetConfigPath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var jo = new JObject
            {
                ["port"] = cfg.Port,
                ["host"] = cfg.Host,
                ["token"] = cfg.Token ?? "",
                ["allowExecute"] = cfg.AllowExecute,
                ["logRequests"] = cfg.LogRequests,
                ["logFile"] = cfg.LogFile ?? "Logs/UACF/session.log"
            };

            File.WriteAllText(path, jo.ToString(Formatting.Indented));
        }

        private static string GenerateToken()
        {
            var bytes = new byte[16];
            new System.Random().NextBytes(bytes);
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant().Substring(0, 24);
        }

        public bool ValidateToken(string authHeader)
        {
            if (string.IsNullOrEmpty(Token)) return true;
            if (string.IsNullOrEmpty(authHeader)) return false;
            var prefix = "Bearer ";
            if (!authHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
            var token = authHeader.Substring(prefix.Length).Trim();
            return token == Token;
        }
    }
}
