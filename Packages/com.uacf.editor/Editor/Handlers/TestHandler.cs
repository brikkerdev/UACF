using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UACF.Core;
using UACF.Models;

namespace UACF.Handlers
{
    public static class TestHandler
    {
        public static void Register(ActionDispatcher dispatcher)
        {
            dispatcher.Register("tests.run", HandleRun);
            dispatcher.Register("tests.results", HandleResults);
        }

        private static Task<UacfResponse> HandleRun(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                try
                {
                    var testRunnerType = Type.GetType("UnityEditor.TestTools.TestRunner.Api.TestRunnerApi, UnityEditor.TestRunner");
                    if (testRunnerType == null)
                        return UacfResponse.Success(new { message = "Test Runner API not available" }, 0);

                    var api = ScriptableObject.CreateInstance(testRunnerType);
                    var filterType = Type.GetType("UnityEditor.TestTools.TestRunner.Api.Filter, UnityEditor.TestRunner");
                    var filter = filterType != null ? Activator.CreateInstance(filterType) : null;

                    var filterStr = p["filter"]?.ToString();
                    if (filter != null && !string.IsNullOrEmpty(filterStr))
                    {
                        var testNamesProp = filterType?.GetProperty("testNames");
                        testNamesProp?.SetValue(filter, new[] { filterStr });
                    }

                    var settingsType = Type.GetType("UnityEditor.TestTools.TestRunner.Api.ExecutionSettings, UnityEditor.TestRunner");
                    var settings = settingsType != null && filter != null
                        ? Activator.CreateInstance(settingsType, filter)
                        : null;

                    var executeMethod = testRunnerType.GetMethod("Execute", new[] { settingsType });
                    if (executeMethod != null && settings != null)
                        executeMethod.Invoke(api, new[] { settings });

                    if (api is UnityEngine.Object obj)
                        UnityEngine.Object.DestroyImmediate(obj);

                    return UacfResponse.Success(new { started = true }, 0);
                }
                catch (Exception ex)
                {
                    return UacfResponse.Fail("TEST_RUN_FAILED", ex.Message, null, 0);
                }
            });
        }

        private static Task<UacfResponse> HandleResults(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                return UacfResponse.Success(new
                {
                    message = "Use Unity Test Runner window for results. tests.run starts async execution."
                }, 0);
            });
        }
    }
}
