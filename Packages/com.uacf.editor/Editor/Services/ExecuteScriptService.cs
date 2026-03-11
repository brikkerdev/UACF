using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UACF.Models;
using UnityEditor;
using UnityEngine;

namespace UACF.Services
{
    public class ExecuteScriptService
    {
        private static ExecuteScriptService _instance;
        public static ExecuteScriptService Instance => _instance ??= new ExecuteScriptService();

        private static readonly string[] DefaultImports =
        {
            "System",
            "System.Linq",
            "System.Collections",
            "System.Collections.Generic",
            "UnityEngine",
            "UnityEditor"
        };

        private static readonly string[] RoslynAssemblyNames =
        {
            "Microsoft.CodeAnalysis",
            "Microsoft.CodeAnalysis.CSharp",
            "Microsoft.CodeAnalysis.Scripting",
            "Microsoft.CodeAnalysis.CSharp.Scripting"
        };

        private readonly object _initLock = new object();
        private bool _initialized;
        private string _initError;
        private Type _csharpScriptType;
        private Type _scriptOptionsType;
        private PropertyInfo _scriptOptionsDefaultProperty;
        private MethodInfo _scriptOptionsAddImportsMethod;
        private MethodInfo _scriptOptionsAddReferencesMethod;
        private MethodInfo _evaluateAsyncMethod;
        private Assembly[] _referenceAssemblies;
        private object _baseScriptOptions;

        private ExecuteScriptService()
        {
        }

        public UacfResponse Execute(string code, string returnExpression, string[] userUsings, int timeoutMs)
        {
            var sw = Stopwatch.StartNew();
            if (!EnsureInitialized())
                return UacfResponse.Fail("NOT_AVAILABLE", _initError ?? "Roslyn scripting is not available", "Install Microsoft.CodeAnalysis scripting assemblies for this Unity Editor.", 0);

            try
            {
                var script = BuildScript(code, returnExpression);
                var options = BuildScriptOptions(userUsings);
                var result = EvaluateInternal(script, options, timeoutMs);
                return UacfResponse.Success(new { result = MakeSerializableResult(result) }, sw.Elapsed.TotalSeconds);
            }
            catch (Exception ex)
            {
                var root = UnwrapException(ex);
                if (root is OperationCanceledException)
                    return UacfResponse.Fail("TIMEOUT", $"Execution exceeded timeout ({timeoutMs}ms)", "Execution cancellation is best-effort. Infinite loops that block the main thread cannot be force-stopped.", sw.Elapsed.TotalSeconds);

                if (root is NotImplementedException)
                    return UacfResponse.Fail("NOT_AVAILABLE", root.Message, "Current Roslyn runtime in this Unity installation does not support required operation.", sw.Elapsed.TotalSeconds);

                if (IsCompilationError(root))
                    return UacfResponse.Fail("COMPILATION_ERROR", GetCompilationDiagnostics(root), null, sw.Elapsed.TotalSeconds);

                return UacfResponse.Fail("INVOCATION_ERROR", root.Message, root.StackTrace, sw.Elapsed.TotalSeconds);
            }
        }

        private bool EnsureInitialized()
        {
            if (_initialized) return true;
            lock (_initLock)
            {
                if (_initialized) return true;
                try
                {
                    EnsureRoslynAssembliesLoaded();
                    _csharpScriptType = ResolveType("Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript, Microsoft.CodeAnalysis.CSharp.Scripting");
                    _scriptOptionsType = ResolveType("Microsoft.CodeAnalysis.Scripting.ScriptOptions, Microsoft.CodeAnalysis.Scripting");

                    if (_csharpScriptType == null || _scriptOptionsType == null)
                    {
                        _initError = "Failed to resolve Roslyn scripting types.";
                        return false;
                    }

                    _scriptOptionsDefaultProperty = _scriptOptionsType.GetProperty("Default", BindingFlags.Public | BindingFlags.Static);
                    _scriptOptionsAddImportsMethod = _scriptOptionsType.GetMethod("AddImports", new[] { typeof(IEnumerable<string>) });
                    _scriptOptionsAddReferencesMethod = _scriptOptionsType.GetMethod("AddReferences", new[] { typeof(IEnumerable<Assembly>) });

                    _evaluateAsyncMethod = _csharpScriptType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .FirstOrDefault(m =>
                        {
                            if (m.Name != "EvaluateAsync" || !m.IsGenericMethodDefinition)
                                return false;

                            var ps = m.GetParameters();
                            return ps.Length == 5 &&
                                   ps[0].ParameterType == typeof(string) &&
                                   ps[1].ParameterType == _scriptOptionsType &&
                                   ps[2].ParameterType == typeof(object) &&
                                   ps[3].ParameterType == typeof(Type) &&
                                   ps[4].ParameterType == typeof(CancellationToken);
                        });

                    if (_scriptOptionsDefaultProperty == null || _scriptOptionsAddImportsMethod == null || _scriptOptionsAddReferencesMethod == null || _evaluateAsyncMethod == null)
                    {
                        _initError = "Failed to bind required Roslyn API methods.";
                        return false;
                    }

                    _referenceAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a =>
                        {
                            if (a == null || a.IsDynamic) return false;
                            var location = a.Location;
                            return !string.IsNullOrWhiteSpace(location) && File.Exists(location);
                        })
                        .ToArray();

                    var options = _scriptOptionsDefaultProperty.GetValue(null, null);
                    options = _scriptOptionsAddImportsMethod.Invoke(options, new object[] { DefaultImports });
                    options = _scriptOptionsAddReferencesMethod.Invoke(options, new object[] { _referenceAssemblies });
                    _baseScriptOptions = options;

                    _initialized = true;
                    return true;
                }
                catch (Exception ex)
                {
                    _initError = $"Roslyn initialization failed: {UnwrapException(ex).Message}";
                    return false;
                }
            }
        }

        private static Type ResolveType(string qualifiedName)
        {
            var type = Type.GetType(qualifiedName, false);
            if (type != null) return type;

            var shortName = qualifiedName.Split(',')[0].Trim();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = asm.GetType(shortName, false);
                    if (type != null) return type;
                }
                catch
                {
                }
            }

            return null;
        }

        private static string BuildScript(string code, string returnExpression)
        {
            var script = (code ?? string.Empty).Trim();
            var hasReturnExpression = !string.IsNullOrWhiteSpace(returnExpression);
            if (!hasReturnExpression && LooksLikeExpression(script))
                return "return (" + script + ");";

            if (!hasReturnExpression)
                return script;

            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(script))
            {
                sb.AppendLine(script);
            }
            sb.Append("return (");
            sb.Append(returnExpression.Trim());
            sb.Append(");");
            return sb.ToString();
        }

        private object BuildScriptOptions(string[] userUsings)
        {
            var options = _baseScriptOptions;
            if (userUsings == null || userUsings.Length == 0)
                return options;

            var imports = userUsings
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (imports.Length == 0)
                return options;

            return _scriptOptionsAddImportsMethod.Invoke(options, new object[] { imports });
        }

        private object EvaluateInternal(string script, object options, int timeoutMs)
        {
            using var cts = timeoutMs > 0 ? new CancellationTokenSource(timeoutMs) : null;
            var token = cts?.Token ?? CancellationToken.None;
            var genericEval = _evaluateAsyncMethod.MakeGenericMethod(typeof(object));

            object taskObj;
            try
            {
                taskObj = genericEval.Invoke(null, new object[] { script, options, null, null, token });
            }
            catch (TargetInvocationException ex)
            {
                throw UnwrapException(ex);
            }

            var task = taskObj as Task;
            if (task == null)
                throw new InvalidOperationException("Roslyn EvaluateAsync did not return Task.");

            task.GetAwaiter().GetResult();
            var resultProperty = taskObj.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
            return resultProperty?.GetValue(taskObj, null);
        }

        private static bool LooksLikeExpression(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            if (code.Contains("\n") || code.Contains("\r")) return false;
            if (code.EndsWith(";")) return false;
            if (code.Contains("{") || code.Contains("}")) return false;

            var statements = new[]
            {
                "if ", "for ", "while ", "switch ", "foreach ", "using ", "try ", "return ", "class ", "struct "
            };
            return !statements.Any(s => code.StartsWith(s, StringComparison.Ordinal));
        }

        private static bool IsCompilationError(Exception ex)
        {
            return ex != null && ex.GetType().FullName == "Microsoft.CodeAnalysis.Scripting.CompilationErrorException";
        }

        private static string GetCompilationDiagnostics(Exception ex)
        {
            if (ex == null) return "Compilation failed";
            var diagnosticsProperty = ex.GetType().GetProperty("Diagnostics", BindingFlags.Public | BindingFlags.Instance);
            var diagnosticsEnumerable = diagnosticsProperty?.GetValue(ex) as System.Collections.IEnumerable;
            if (diagnosticsEnumerable == null)
                return ex.Message;

            var messages = new List<string>();
            foreach (var diag in diagnosticsEnumerable)
                messages.Add(diag?.ToString());

            return messages.Count > 0 ? string.Join("\n", messages) : ex.Message;
        }

        private static Exception UnwrapException(Exception ex)
        {
            if (ex is TargetInvocationException tie && tie.InnerException != null)
                return UnwrapException(tie.InnerException);
            if (ex is AggregateException ae && ae.InnerException != null)
                return UnwrapException(ae.InnerException);
            return ex;
        }

        private static object MakeSerializableResult(object value)
        {
            if (value == null) return null;

            var type = value.GetType();
            if (type.IsPrimitive || value is string || value is decimal)
                return value;

            if (type.IsEnum)
                return value.ToString();

            if (value is UnityEngine.Object unityObject)
            {
                var assetPath = AssetDatabase.GetAssetPath(unityObject);
                return new
                {
                    name = unityObject.name,
                    instanceId = unityObject.GetInstanceID(),
                    type = unityObject.GetType().FullName,
                    assetPath = string.IsNullOrEmpty(assetPath) ? null : assetPath
                };
            }

            return value;
        }

        private static void EnsureRoslynAssembliesLoaded()
        {
            foreach (var assemblyName in RoslynAssemblyNames)
            {
                if (AppDomain.CurrentDomain.GetAssemblies().Any(a => string.Equals(a.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase)))
                    continue;

                try
                {
                    Assembly.Load(assemblyName);
                    continue;
                }
                catch
                {
                }

                Exception lastLoadError = null;
                foreach (var candidate in FindAssemblyPaths(assemblyName + ".dll"))
                {
                    try
                    {
                        Assembly.LoadFrom(candidate);
                        lastLoadError = null;
                        break;
                    }
                    catch (Exception ex)
                    {
                        lastLoadError = ex;
                    }
                }

                if (lastLoadError != null && !AppDomain.CurrentDomain.GetAssemblies().Any(a => string.Equals(a.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException($"Failed to load {assemblyName}: {lastLoadError.Message}", lastLoadError);
            }
        }

        private static IEnumerable<string> FindAssemblyPaths(string fileName)
        {
            var roots = new List<string>();
            if (!string.IsNullOrWhiteSpace(EditorApplication.applicationContentsPath))
            {
                var appContents = EditorApplication.applicationContentsPath;
                roots.Add(Path.Combine(appContents, "MonoBleedingEdge", "lib", "mono", "4.5"));
                roots.Add(Path.Combine(appContents, "MonoBleedingEdge", "lib", "mono", "unityjit"));
                roots.Add(Path.Combine(appContents, "NetStandard", "compat", "2.1.0", "shims", "netstandard"));
                roots.Add(Path.Combine(appContents, "Tools", "Roslyn"));
                roots.Add(Path.Combine(appContents, "Tools"));
                roots.Add(appContents);
            }

            var managedDir = Path.GetDirectoryName(typeof(Editor).Assembly.Location);
            if (!string.IsNullOrWhiteSpace(managedDir))
            {
                roots.Add(managedDir);
            }

            var candidates = new List<string>();
            foreach (var root in roots.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                        continue;

                    var directPath = Path.Combine(root, fileName);
                    if (File.Exists(directPath))
                        candidates.Add(directPath);

                    var files = Directory.GetFiles(root, fileName, SearchOption.AllDirectories);
                    if (files.Length > 0)
                        candidates.AddRange(files);
                }
                catch
                {
                }
            }

            return candidates
                .Where(File.Exists)
                .Where(p => p.IndexOf("DotNetSdkRoslyn", StringComparison.OrdinalIgnoreCase) < 0)
                .Where(p => p.IndexOf("ref", StringComparison.OrdinalIgnoreCase) < 0)
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }
    }
}
