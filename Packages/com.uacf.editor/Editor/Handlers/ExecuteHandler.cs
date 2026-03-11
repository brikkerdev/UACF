using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UACF.Config;
using UACF.Core;
using UACF.Models;
using UACF.Services;

namespace UACF.Handlers
{
    public static class ExecuteHandler
    {
        public static void Register(ActionDispatcher dispatcher)
        {
            dispatcher.Register("execute", HandleExecute);
            dispatcher.Register("execute.validate", HandleValidate);
            dispatcher.Register("execute.method", HandleMethod);
        }

        private static Task<UacfResponse> HandleExecute(JObject p)
        {
            if (!UACFConfig.Instance.AllowExecute)
                return Task.FromResult(UacfResponse.Fail("FORBIDDEN", "execute is disabled", "Set allowExecute: true in config.json", 0));

            return Task.FromResult(UacfResponse.Fail("NOT_IMPLEMENTED",
                "Arbitrary C# execution is not yet implemented",
                "Use execute.method to call static methods, or execute.validate to check syntax", 0));
        }

        private static Task<UacfResponse> HandleValidate(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var code = p["code"]?.ToString();
                if (string.IsNullOrEmpty(code))
                    return UacfResponse.Fail("INVALID_REQUEST", "code is required", null, 0);

                var svc = CompilationService.Instance;
                var result = svc.GetLastResult();
                return UacfResponse.Success(new
                {
                    hasErrors = result.HasErrors,
                    errors = result.Errors.Select(e => new { file = e.File, line = e.Line, message = e.Message }).ToArray(),
                    warnings = result.Warnings.Select(w => new { file = w.File, line = w.Line, message = w.Message }).ToArray()
                }, 0);
            });
        }

        private static Task<UacfResponse> HandleMethod(JObject p)
        {
            if (!UACFConfig.Instance.AllowExecute)
                return Task.FromResult(UacfResponse.Fail("FORBIDDEN", "execute is disabled", "Set allowExecute: true in config.json", 0));

            return MainThreadDispatcher.Enqueue(() =>
            {
                var typeName = p["type"]?.ToString();
                var methodName = p["method"]?.ToString();
                if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(methodName))
                    return UacfResponse.Fail("INVALID_REQUEST", "type and method are required", null, 0);

                var type = TypeResolverService.Instance.Resolve(typeName) ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return new Type[0]; } })
                    .FirstOrDefault(t => t.Name == typeName || t.FullName == typeName);

                if (type == null)
                    return UacfResponse.Fail("TYPE_NOT_FOUND", $"Type '{typeName}' not found", null, 0);

                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod);
                if (method == null)
                    return UacfResponse.Fail("METHOD_NOT_FOUND", $"Static method '{methodName}' not found on {typeName}", null, 0);

                var argsToken = p["args"] as JArray;
                object[] args = null;
                if (argsToken != null)
                {
                    var paramTypes = method.GetParameters();
                    args = new object[paramTypes.Length];
                    for (int i = 0; i < Math.Min(argsToken.Count, paramTypes.Length); i++)
                    {
                        var paramType = paramTypes[i].ParameterType;
                        var val = argsToken[i];
                        if (paramType == typeof(string)) args[i] = val?.ToString();
                        else if (paramType == typeof(int)) args[i] = val?.Value<int>() ?? 0;
                        else if (paramType == typeof(float)) args[i] = val?.Value<float>() ?? 0f;
                        else if (paramType == typeof(bool)) args[i] = val?.Value<bool>() ?? false;
                        else args[i] = val?.ToObject(paramType);
                    }
                }

                try
                {
                    var result = method.Invoke(null, args);
                    return UacfResponse.Success(new { result }, 0);
                }
                catch (Exception ex)
                {
                    var inner = ex.InnerException ?? ex;
                    return UacfResponse.Fail("INVOCATION_ERROR", inner.Message, inner.StackTrace, 0);
                }
            });
        }
    }
}
