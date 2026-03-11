using System;
using UnityEditor;
using UnityEngine;

namespace UACF.Config
{
    public enum LogLevel
    {
        None,
        Error,
        Warning,
        Info,
        Debug
    }

    public class UACFSettings : ScriptableSingleton<UACFSettings>
    {
        [SerializeField] private int _port = 7890;
        [SerializeField] private bool _autoStart = true;
        [SerializeField] private bool _logRequests = true;
        [SerializeField] private bool _logResponses = false;
        [SerializeField] private int _requestTimeoutSeconds = 30;
        [SerializeField] private int _compileTimeoutSeconds = 120;
        [SerializeField] private string[] _allowedOrigins = { "*" };
        [SerializeField] private bool _enableBatchEndpoint = true;
        [SerializeField] private LogLevel _logLevel = LogLevel.Info;

        public int Port { get => _port; set => _port = value; }
        public bool AutoStart { get => _autoStart; set => _autoStart = value; }
        public bool LogRequests { get => _logRequests; set => _logRequests = value; }
        public bool LogResponses { get => _logResponses; set => _logResponses = value; }
        public int RequestTimeoutSeconds { get => _requestTimeoutSeconds; set => _requestTimeoutSeconds = value; }
        public int CompileTimeoutSeconds { get => _compileTimeoutSeconds; set => _compileTimeoutSeconds = value; }
        public string[] AllowedOrigins { get => _allowedOrigins; set => _allowedOrigins = value; }
        public bool EnableBatchEndpoint { get => _enableBatchEndpoint; set => _enableBatchEndpoint = value; }
        public LogLevel LogLevel { get => _logLevel; set => _logLevel = value; }

        public void Save()
        {
            Save(true);
        }
    }
}
