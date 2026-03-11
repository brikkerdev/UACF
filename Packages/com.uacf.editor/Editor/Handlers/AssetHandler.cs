using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UACF.Core;
using UACF.Models;
using UACF.Services;

namespace UACF.Handlers
{
    public static class AssetHandler
    {
        public static void Register(ActionDispatcher dispatcher)
        {
            dispatcher.Register("asset.find", HandleFind);
            dispatcher.Register("asset.info", HandleInfo);
            dispatcher.Register("asset.tree", HandleTree);
            dispatcher.Register("asset.file.write", HandleFileWrite);
            dispatcher.Register("asset.file.read", HandleFileRead);
            dispatcher.Register("asset.file.move", HandleFileMove);
            dispatcher.Register("asset.file.delete", HandleFileDelete);
            dispatcher.Register("asset.folder.create", HandleFolderCreate);
            dispatcher.Register("asset.refresh", HandleRefresh);
            dispatcher.Register("asset.create.scriptableObject", HandleCreateScriptableObject);
            dispatcher.Register("asset.create.panelSettings", HandleCreatePanelSettings);
            dispatcher.Register("asset.create.material", HandleCreateMaterial);
            dispatcher.Register("asset.create.physicMaterial", HandleCreatePhysicMaterial);
            dispatcher.Register("asset.create.animationClip", HandleCreateAnimationClip);
        }

        private static Task<UacfResponse> HandleFind(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var type = p["type"]?.ToString();
                var name = p["name"]?.ToString();
                var folder = p["folder"]?.ToString();
                var filter = BuildSearchFilter(type, name);
                if (string.IsNullOrEmpty(filter)) filter = "t:Object";
                var paths = AssetDatabaseService.FindAssets(filter, folder);
                var assets = paths.Select(path =>
                {
                    var guid = AssetDatabase.AssetPathToGUID(path);
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    return new { path, guid, type = obj?.GetType().Name ?? "Unknown" };
                }).ToArray();
                return UacfResponse.Success(new { assets }, 0);
            });
        }

        private static string BuildSearchFilter(string type, string name)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(type))
            {
                var t = type.ToLowerInvariant();
                if (t == "prefab") parts.Add("t:Prefab");
                else if (t == "material") parts.Add("t:Material");
                else if (t == "script") parts.Add("t:MonoScript");
                else if (t == "texture") parts.Add("t:Texture2D");
                else parts.Add($"t:{type}");
            }
            if (!string.IsNullOrEmpty(name)) parts.Add(name);
            return parts.Count > 0 ? string.Join(" ", parts) : "";
        }

        private static Task<UacfResponse> HandleInfo(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var path = p["path"]?.ToString();
                if (string.IsNullOrEmpty(path))
                    return UacfResponse.Fail("INVALID_REQUEST", "path is required", null, 0);

                if (!AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path))
                    return UacfResponse.Fail("NOT_FOUND", "Asset not found", null, 0);

                var guid = AssetDatabase.AssetPathToGUID(path);
                var deps = AssetDatabase.GetDependencies(path, false);
                var fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), path);
                var fileSize = File.Exists(fullPath) ? new FileInfo(fullPath).Length : 0;

                return UacfResponse.Success(new
                {
                    path,
                    guid,
                    type = AssetDatabase.GetMainAssetTypeAtPath(path)?.Name ?? "Unknown",
                    fileSize,
                    dependencies = deps
                }, 0);
            });
        }

        private static Task<UacfResponse> HandleTree(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var root = p["path"]?.ToString() ?? "Assets";
                var depth = p["depth"]?.Value<int>() ?? 2;
                var tree = BuildFolderTree(root, 0, depth);
                return UacfResponse.Success(new { tree }, 0);
            });
        }

        private static object BuildFolderTree(string path, int currentDepth, int maxDepth)
        {
            var children = new List<object>();
            if (currentDepth < maxDepth)
            {
                var subFolders = AssetDatabase.GetSubFolders(path);
                foreach (var sub in subFolders)
                    children.Add(BuildFolderTree(sub, currentDepth + 1, maxDepth));
            }
            return new { path, children };
        }

        private static async Task<UacfResponse> HandleFileWrite(JObject p)
        {
            var path = p["path"]?.ToString();
            var content = p["content"]?.ToString() ?? "";
            if (string.IsNullOrEmpty(path))
                return UacfResponse.Fail("INVALID_REQUEST", "path is required", null, 0);

            if (!path.StartsWith("Assets/") && !path.StartsWith("Packages/"))
                return UacfResponse.Fail("INVALID_REQUEST", "path must be under Assets/ or Packages/", null, 0);

            var compileAffecting = IsCompileAffectingPath(path);
            // By default, do not trigger compile on every .cs/.asmdef write.
            // Use asset.refresh once after a batch of edits.
            var refresh = p["refresh"]?.Value<bool?>() ?? !compileAffecting;
            var waitForCompilation = p["waitForCompilation"]?.Value<bool?>() ?? false;

            await MainThreadDispatcher.Enqueue(() =>
            {
                var projectRoot = Path.GetDirectoryName(Application.dataPath);
                var fullPath = Path.Combine(projectRoot, path);
                var dir = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(fullPath, content);
                if (refresh)
                    AssetDatabase.Refresh();
            });

            if (!waitForCompilation)
                return UacfResponse.Success(new
                {
                    written = true,
                    path,
                    refreshed = refresh,
                    waitedForCompilation = false
                }, 0);

            var timeout = p["compileTimeoutSeconds"]?.Value<int?>() ?? UACF.Config.UACFSettings.instance.CompileTimeoutSeconds;
            try
            {
                if (!refresh)
                {
                    await MainThreadDispatcher.Enqueue(() => AssetDatabase.Refresh());
                    refresh = true;
                }

                var compile = await CompilationService.Instance.WaitForCompilationToFinishAsync(timeout);
                return UacfResponse.Success(new
                {
                    written = true,
                    path,
                    refreshed = refresh,
                    waitedForCompilation = true,
                    compilation = BuildCompilationPayload(compile)
                }, 0);
            }
            catch (TimeoutException ex)
            {
                return UacfResponse.Fail("TIMEOUT", ex.Message, "Compilation is still running. Retry with a higher compileTimeoutSeconds.", 0);
            }
        }

        private static Task<UacfResponse> HandleFileRead(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var path = p["path"]?.ToString();
                if (string.IsNullOrEmpty(path))
                    return UacfResponse.Fail("INVALID_REQUEST", "path is required", null, 0);

                var normPath = path.StartsWith("Assets/") || path.StartsWith("Packages/") ? path : "Assets/" + path;
                var projectRoot = Path.GetDirectoryName(Application.dataPath);
                var fullPath = Path.Combine(projectRoot, normPath);

                if (!File.Exists(fullPath))
                    return UacfResponse.Fail("NOT_FOUND", "File not found", null, 0);

                var content = File.ReadAllText(fullPath);
                return UacfResponse.Success(new { path = normPath, content }, 0);
            });
        }

        private static Task<UacfResponse> HandleFileMove(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var from = p["from"]?.ToString();
                var to = p["to"]?.ToString();
                if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                    return UacfResponse.Fail("INVALID_REQUEST", "from and to are required", null, 0);

                var result = AssetDatabase.MoveAsset(from, to);
                if (!string.IsNullOrEmpty(result))
                    return UacfResponse.Fail("MOVE_FAILED", result, null, 0);
                return UacfResponse.Success(new { moved = true }, 0);
            });
        }

        private static Task<UacfResponse> HandleFileDelete(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var path = p["path"]?.ToString();
                if (string.IsNullOrEmpty(path))
                    return UacfResponse.Fail("INVALID_REQUEST", "path is required", null, 0);

                var ok = AssetDatabaseService.DeleteAsset(path);
                return UacfResponse.Success(new { deleted = ok }, 0);
            });
        }

        private static Task<UacfResponse> HandleFolderCreate(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() =>
            {
                var path = p["path"]?.ToString();
                if (string.IsNullOrEmpty(path))
                    return UacfResponse.Fail("INVALID_REQUEST", "path is required", null, 0);

                var ok = AssetDatabaseService.CreateFolder(path);
                return UacfResponse.Success(new { created = ok }, 0);
            });
        }

        private static async Task<UacfResponse> HandleRefresh(JObject p)
        {
            var waitForCompilation = p["waitForCompilation"]?.Value<bool?>() ?? true;
            var timeout = p["compileTimeoutSeconds"]?.Value<int?>() ?? UACF.Config.UACFSettings.instance.CompileTimeoutSeconds;

            await MainThreadDispatcher.Enqueue(() =>
            {
                AssetDatabase.Refresh();
            });

            if (!waitForCompilation)
                return UacfResponse.Success(new { refreshed = true, waitedForCompilation = false }, 0);

            try
            {
                var compile = await CompilationService.Instance.WaitForCompilationToFinishAsync(timeout);
                return UacfResponse.Success(new
                {
                    refreshed = true,
                    waitedForCompilation = true,
                    compilation = BuildCompilationPayload(compile)
                }, 0);
            }
            catch (TimeoutException ex)
            {
                return UacfResponse.Fail("TIMEOUT", ex.Message, "Compilation is still running. Retry with a higher compileTimeoutSeconds.", 0);
            }
        }

        private static Task<UacfResponse> HandleCreateScriptableObject(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() => AssetCreationService.CreateScriptableObject(p));
        }

        private static Task<UacfResponse> HandleCreatePanelSettings(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() => AssetCreationService.CreatePanelSettings(p));
        }

        private static Task<UacfResponse> HandleCreateMaterial(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() => AssetCreationService.CreateMaterial(p));
        }

        private static Task<UacfResponse> HandleCreatePhysicMaterial(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() => AssetCreationService.CreatePhysicMaterial(p));
        }

        private static Task<UacfResponse> HandleCreateAnimationClip(JObject p)
        {
            return MainThreadDispatcher.Enqueue(() => AssetCreationService.CreateAnimationClip(p));
        }

        private static bool IsCompileAffectingPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            var ext = Path.GetExtension(path).ToLowerInvariant();
            switch (ext)
            {
                case ".cs":
                case ".asmdef":
                case ".asmref":
                case ".rsp":
                case ".dll":
                    return true;
                default:
                    return false;
            }
        }

        private static object BuildCompilationPayload(CompileResult result)
        {
            return new
            {
                hasErrors = result.HasErrors,
                errorCount = result.ErrorCount,
                warningCount = result.WarningCount,
                durationMs = result.DurationMs,
                errors = (result.Errors ?? new CompileError[0]).Select(e => new
                {
                    file = e.File,
                    line = e.Line,
                    column = e.Column,
                    message = e.Message,
                    severity = e.Severity
                }).ToArray(),
                warnings = (result.Warnings ?? new CompileError[0]).Select(w => new
                {
                    file = w.File,
                    line = w.Line,
                    column = w.Column,
                    message = w.Message,
                    severity = w.Severity
                }).ToArray()
            };
        }
    }
}
