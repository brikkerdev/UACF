using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UACF.Core;
using UACF.Models;

namespace UACF.Handlers
{
    public static class ProjectHandler
    {
        public static void Register(ActionDispatcher dispatcher)
        {
            dispatcher.Register("project.info", HandleInfo);
            dispatcher.Register("project.tags", HandleTags);
            dispatcher.Register("project.layers", HandleLayers);
            dispatcher.Register("project.settings.get", HandleSettingsGet);
            dispatcher.Register("project.settings.set", HandleSettingsSet);
        }

        private static Task<UacfResponse> HandleInfo(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                    .Select(pkg => new { name = pkg.name, version = pkg.version })
                    .ToArray();

                return UacfResponse.Success(new
                {
                    unityVersion = Application.unityVersion,
                    projectName = System.IO.Path.GetFileNameWithoutExtension(Application.dataPath),
                    projectPath = System.IO.Path.GetDirectoryName(Application.dataPath),
                    renderPipeline = GetRenderPipeline(),
                    targetPlatform = Application.platform.ToString(),
                    packages
                }, 0);
            });
        }

        private static string GetRenderPipeline()
        {
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
                return UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().Name;
            return "Built-in";
        }

        private static Task<UacfResponse> HandleTags(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var tags = UnityEditorInternal.InternalEditorUtility.tags;
                return UacfResponse.Success(new { tags }, 0);
            });
        }

        private static Task<UacfResponse> HandleLayers(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var layers = new System.Collections.Generic.Dictionary<string, int>();
                for (int i = 0; i < 32; i++)
                {
                    var name = LayerMask.LayerToName(i);
                    if (!string.IsNullOrEmpty(name))
                        layers[name] = i;
                }
                return UacfResponse.Success(new { layers }, 0);
            });
        }

        private static Task<UacfResponse> HandleSettingsGet(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var category = p["category"]?.ToString();
                if (string.IsNullOrEmpty(category))
                    return UacfResponse.Fail("INVALID_REQUEST", "category is required", null, 0);

                var settings = GetSettingsByCategory(category);
                return UacfResponse.Success(new { category, properties = settings }, 0);
            });
        }

        private static object GetSettingsByCategory(string category)
        {
            switch (category.ToLowerInvariant())
            {
                case "physics":
                    return new
                    {
                        gravity = new[] { Physics.gravity.x, Physics.gravity.y, Physics.gravity.z },
                        defaultContactOffset = Physics.defaultContactOffset,
                        bounceThreshold = Physics.bounceThreshold
                    };
                case "time":
                    return new
                    {
                        fixedDeltaTime = Time.fixedDeltaTime,
                        timeScale = Time.timeScale
                    };
                default:
                    return new { message = $"Category '{category}' not implemented" };
            }
        }

        private static Task<UacfResponse> HandleSettingsSet(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var category = p["category"]?.ToString();
                var props = p["properties"] as JObject;
                if (string.IsNullOrEmpty(category) || props == null)
                    return UacfResponse.Fail("INVALID_REQUEST", "category and properties are required", null, 0);

                if (category.ToLowerInvariant() == "physics")
                {
                    var gravity = props["gravity"] as JArray;
                    if (gravity != null && gravity.Count >= 3)
                        Physics.gravity = new Vector3(gravity[0].Value<float>(), gravity[1].Value<float>(), gravity[2].Value<float>());
                }

                return UacfResponse.Success(new { updated = true }, 0);
            });
        }
    }
}
