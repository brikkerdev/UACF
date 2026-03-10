using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UACF.Models;

namespace UACF.Services
{
    public class CompilationService
    {
        private static CompilationService _instance;
        public static CompilationService Instance => _instance ??= new CompilationService();

        private readonly List<CompileError> _lastErrors = new List<CompileError>();
        private readonly List<CompileError> _lastWarnings = new List<CompileError>();
        private bool _isCompiling;
        private DateTime _lastCompileTime;
        private bool _lastCompileSuccess;
        private TaskCompletionSource<bool> _compileTcs;

        public bool IsCompiling => _isCompiling;
        public DateTime LastCompileTime => _lastCompileTime;
        public bool LastCompileSuccess => _lastCompileSuccess;
        public IReadOnlyList<CompileError> LastErrors => _lastErrors;
        public IReadOnlyList<CompileError> LastWarnings => _lastWarnings;

        private CompilationService()
        {
            CompilationPipeline.compilationStarted += _ =>
            {
                _isCompiling = true;
                _lastErrors.Clear();
                _lastWarnings.Clear();
            };
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
        }

        private void OnAssemblyCompilationFinished(string path, CompilerMessage[] messages)
        {
            foreach (var msg in messages)
            {
                var err = new CompileError
                {
                    Message = msg.message,
                    File = msg.file,
                    Line = msg.line,
                    Column = msg.column,
                    Severity = msg.type == CompilerMessageType.Error ? "error" : "warning",
                    Id = ""
                };
                if (msg.type == CompilerMessageType.Error)
                    _lastErrors.Add(err);
                else
                    _lastWarnings.Add(err);
            }
        }

        private void OnCompilationFinished(object obj)
        {
            _isCompiling = false;
            _lastCompileTime = DateTime.UtcNow;
            _lastCompileSuccess = _lastErrors.Count == 0;
            _compileTcs?.TrySetResult(true);
        }

        public async Task<CompileResult> RequestCompilationAsync(int timeoutSeconds = 60)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            _lastErrors.Clear();
            _lastWarnings.Clear();
            _compileTcs = new TaskCompletionSource<bool>();

            AssetDatabase.Refresh();

            if (!EditorApplication.isCompiling)
            {
                _lastCompileTime = DateTime.UtcNow;
                _lastCompileSuccess = true;
                sw.Stop();
                return new CompileResult
                {
                    Compiled = true,
                    HasErrors = false,
                    ErrorCount = 0,
                    WarningCount = 0,
                    Errors = new CompileError[0],
                    Warnings = new CompileError[0],
                    DurationMs = sw.ElapsedMilliseconds
                };
            }

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
            {
                cts.Token.Register(() => _compileTcs?.TrySetCanceled());
                await _compileTcs.Task;
            }

            sw.Stop();
            return new CompileResult
            {
                Compiled = true,
                HasErrors = _lastErrors.Count > 0,
                ErrorCount = _lastErrors.Count,
                WarningCount = _lastWarnings.Count,
                Errors = _lastErrors.ToArray(),
                Warnings = _lastWarnings.ToArray(),
                DurationMs = sw.ElapsedMilliseconds
            };
        }

        public CompileResult GetLastResult()
        {
            return new CompileResult
            {
                Compiled = true,
                HasErrors = _lastErrors.Count > 0,
                ErrorCount = _lastErrors.Count,
                WarningCount = _lastWarnings.Count,
                Errors = _lastErrors.ToArray(),
                Warnings = _lastWarnings.ToArray(),
                DurationMs = 0
            };
        }
    }

    public class CompileResult
    {
        public bool Compiled;
        public bool HasErrors;
        public int ErrorCount;
        public int WarningCount;
        public CompileError[] Errors;
        public CompileError[] Warnings;
        public long DurationMs;
    }
}
