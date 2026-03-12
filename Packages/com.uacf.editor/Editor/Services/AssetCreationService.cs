using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UACF.Models;

namespace UACF.Services
{
    public static class AssetCreationService
    {
        public static UacfResponse CreateScriptableObject(JObject p)
        {
            var typeName = p["type"]?.ToString();
            var path = p["path"]?.ToString();
            var overwrite = p["overwrite"]?.Value<bool>() ?? false;
            var properties = p["properties"] as JObject;

            if (string.IsNullOrWhiteSpace(typeName) || string.IsNullOrWhiteSpace(path))
                return UacfResponse.Fail("INVALID_REQUEST", "type and path are required", null, 0);

            var type = TypeResolverService.Instance.Resolve(typeName);
            if (type == null)
                return UacfResponse.Fail("TYPE_NOT_FOUND", $"Type '{typeName}' not found", "Use fully qualified type name or check script compilation", 0);

            if (!typeof(ScriptableObject).IsAssignableFrom(type))
                return UacfResponse.Fail("INVALID_TYPE", $"Type '{typeName}' is not a ScriptableObject", null, 0);

            return CreateAsset(path, overwrite, ".asset", assetPath =>
            {
                var so = ScriptableObject.CreateInstance(type);
                if (so == null)
                    return UacfResponse.Fail("CREATE_FAILED", $"Failed to create ScriptableObject of type '{typeName}'", null, 0);

                ApplyObjectProperties(so, properties);
                AssetDatabase.CreateAsset(so, assetPath);
                return Success(assetPath, type.FullName);
            });
        }

        public static UacfResponse CreatePanelSettings(JObject p)
        {
            var path = p["path"]?.ToString();
            var overwrite = p["overwrite"]?.Value<bool>() ?? false;
            var properties = p["properties"] as JObject;

            if (string.IsNullOrWhiteSpace(path))
                return UacfResponse.Fail("INVALID_REQUEST", "path is required", null, 0);

            var panelType = Type.GetType("UnityEngine.UIElements.PanelSettings, UnityEngine.UIElementsModule")
                            ?? TypeResolverService.Instance.Resolve("UnityEngine.UIElements.PanelSettings")
                            ?? TypeResolverService.Instance.Resolve("PanelSettings");
            if (panelType == null)
                return UacfResponse.Fail("TYPE_NOT_FOUND", "PanelSettings type is not available", "Ensure UI Toolkit is installed and enabled", 0);

            if (!typeof(ScriptableObject).IsAssignableFrom(panelType))
                return UacfResponse.Fail("INVALID_TYPE", "Resolved PanelSettings type is not a ScriptableObject", null, 0);

            return CreateAsset(path, overwrite, ".asset", assetPath =>
            {
                var panel = ScriptableObject.CreateInstance(panelType);
                if (panel == null)
                    return UacfResponse.Fail("CREATE_FAILED", "Failed to create PanelSettings", null, 0);

                ApplyObjectProperties(panel, properties);
                AssetDatabase.CreateAsset(panel, assetPath);
                return Success(assetPath, panelType.FullName);
            });
        }

        public static UacfResponse CreateMaterial(JObject p)
        {
            var path = p["path"]?.ToString();
            var shaderName = p["shader"]?.ToString();
            var overwrite = p["overwrite"]?.Value<bool>() ?? false;
            var properties = p["properties"] as JObject;

            if (string.IsNullOrWhiteSpace(path))
                return UacfResponse.Fail("INVALID_REQUEST", "path is required", null, 0);

            if (string.IsNullOrWhiteSpace(shaderName))
                shaderName = "Standard";

            var shader = Shader.Find(shaderName);
            if (shader == null)
                return UacfResponse.Fail("INVALID_REQUEST", $"Shader '{shaderName}' not found", null, 0);

            return CreateAsset(path, overwrite, ".mat", assetPath =>
            {
                var material = new Material(shader);
                ApplyMaterialProperties(material, properties);
                AssetDatabase.CreateAsset(material, assetPath);
                return Success(assetPath, typeof(Material).FullName);
            });
        }

        public static UacfResponse CreatePhysicMaterial(JObject p)
        {
            var path = p["path"]?.ToString();
            var overwrite = p["overwrite"]?.Value<bool>() ?? false;
            var properties = p["properties"] as JObject;

            if (string.IsNullOrWhiteSpace(path))
                return UacfResponse.Fail("INVALID_REQUEST", "path is required", null, 0);

            return CreateAsset(path, overwrite, ".physicMaterial", assetPath =>
            {
                var name = Path.GetFileNameWithoutExtension(assetPath);
                var material = new PhysicsMaterial(name);
                ApplyObjectProperties(material, properties);
                AssetDatabase.CreateAsset(material, assetPath);
                return Success(assetPath, typeof(PhysicsMaterial).FullName);
            });
        }

        public static UacfResponse CreateFromFile(JObject p)
        {
            var path = p["path"]?.ToString();
            var sourcePath = p["sourcePath"]?.ToString();
            var overwrite = p["overwrite"]?.Value<bool>() ?? false;

            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(sourcePath))
                return UacfResponse.Fail("INVALID_REQUEST", "path and sourcePath are required", null, 0);

            var normalizedPath = path.Replace("\\", "/").Trim();
            if (!normalizedPath.StartsWith("Assets/", StringComparison.Ordinal) && !normalizedPath.StartsWith("Packages/", StringComparison.Ordinal))
                return UacfResponse.Fail("INVALID_REQUEST", "path must be under Assets/ or Packages/", null, 0);

            if (normalizedPath.EndsWith("/"))
                return UacfResponse.Fail("INVALID_REQUEST", "path must point to a file, not a folder", null, 0);

            if (!Path.HasExtension(normalizedPath))
            {
                var sourceExt = Path.GetExtension(sourcePath);
                if (!string.IsNullOrEmpty(sourceExt))
                    normalizedPath += sourceExt;
                else
                    return UacfResponse.Fail("INVALID_REQUEST", "path or sourcePath must have a file extension", null, 0);
            }

            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var destFullPath = Path.Combine(projectRoot, normalizedPath);
            var sourceFullPath = Path.GetFullPath(sourcePath);

            if (!File.Exists(sourceFullPath))
                return UacfResponse.Fail("NOT_FOUND", $"Source file not found: {sourcePath}", null, 0);

            var assetFolder = Path.GetDirectoryName(normalizedPath)?.Replace("\\", "/");
            if (!string.IsNullOrWhiteSpace(assetFolder) && !AssetDatabase.IsValidFolder(assetFolder))
            {
                if (!AssetDatabaseService.CreateFolder(assetFolder))
                    return UacfResponse.Fail("CREATE_FAILED", $"Unable to create folder for '{normalizedPath}'", null, 0);
            }

            var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(normalizedPath);
            if (existing != null)
            {
                if (!overwrite)
                    return UacfResponse.Fail("ASSET_EXISTS", $"Asset already exists at '{normalizedPath}'", "Set overwrite=true to replace", 0);
                if (!AssetDatabase.DeleteAsset(normalizedPath))
                    return UacfResponse.Fail("CREATE_FAILED", $"Failed to delete existing asset at '{normalizedPath}'", null, 0);
            }

            try
            {
                File.Copy(sourceFullPath, destFullPath, overwrite);
                AssetDatabase.Refresh();
                AssetDatabase.ImportAsset(normalizedPath);
                var assetType = AssetDatabase.GetMainAssetTypeAtPath(normalizedPath)?.Name ?? "Unknown";
                return Success(normalizedPath, assetType);
            }
            catch (Exception ex)
            {
                return UacfResponse.Fail("CREATE_FAILED", ex.Message, null, 0);
            }
        }

        public static UacfResponse CreateFont(JObject p)
        {
            var path = p["path"]?.ToString();
            var sourcePath = p["sourcePath"]?.ToString();
            var overwrite = p["overwrite"]?.Value<bool>() ?? false;

            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(sourcePath))
                return UacfResponse.Fail("INVALID_REQUEST", "path and sourcePath are required", null, 0);

            var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
            if (ext != ".ttf" && ext != ".otf")
                return UacfResponse.Fail("INVALID_REQUEST", "sourcePath must be a .ttf or .otf font file", null, 0);

            var normalizedPath = path.Replace("\\", "/").Trim();
            if (!normalizedPath.StartsWith("Assets/", StringComparison.Ordinal) && !normalizedPath.StartsWith("Packages/", StringComparison.Ordinal))
                return UacfResponse.Fail("INVALID_REQUEST", "path must be under Assets/ or Packages/", null, 0);

            if (!Path.HasExtension(normalizedPath))
                normalizedPath += ext;

            return CreateFromFile(new JObject
            {
                ["path"] = normalizedPath,
                ["sourcePath"] = sourcePath,
                ["overwrite"] = overwrite
            });
        }

        public static UacfResponse CreateTMPFontAsset(JObject p)
        {
            var path = p["path"]?.ToString();
            var sourceFont = p["sourceFont"]?.ToString();
            var overwrite = p["overwrite"]?.Value<bool>() ?? false;

            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(sourceFont))
                return UacfResponse.Fail("INVALID_REQUEST", "path and sourceFont are required", null, 0);

            var font = AssetDatabase.LoadAssetAtPath<Font>(sourceFont);
            if (font == null)
                return UacfResponse.Fail("NOT_FOUND", $"Font not found at '{sourceFont}'", "Use asset path to an imported .ttf/.otf font", 0);

            var tmpType = Type.GetType("TMPro.TMP_FontAsset, Unity.TextMeshPro");
            if (tmpType == null)
                return UacfResponse.Fail("TYPE_NOT_FOUND", "TextMeshPro (TMP_FontAsset) is not available", "Add com.unity.textmeshpro package", 0);

            var createMethod = tmpType.GetMethod("CreateFontAsset", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(Font) }, null);
            if (createMethod == null)
                return UacfResponse.Fail("TYPE_NOT_FOUND", "TMP_FontAsset.CreateFontAsset not found", null, 0);

            return CreateAsset(path, overwrite, ".asset", assetPath =>
            {
                var tmpFont = createMethod.Invoke(null, new object[] { font });
                if (tmpFont == null)
                    return UacfResponse.Fail("CREATE_FAILED", "TMP_FontAsset.CreateFontAsset returned null", null, 0);
                AssetDatabase.CreateAsset((UnityEngine.Object)tmpFont, assetPath);
                return Success(assetPath, "TMP_FontAsset");
            });
        }

        public static UacfResponse CreateRenderTexture(JObject p)
        {
            var path = p["path"]?.ToString();
            var width = p["width"]?.Value<int>() ?? 256;
            var height = p["height"]?.Value<int>() ?? 256;
            var depth = p["depth"]?.Value<int>() ?? 24;
            var overwrite = p["overwrite"]?.Value<bool>() ?? false;

            if (string.IsNullOrWhiteSpace(path))
                return UacfResponse.Fail("INVALID_REQUEST", "path is required", null, 0);

            return CreateAsset(path, overwrite, ".renderTexture", assetPath =>
            {
                var rt = new RenderTexture(width, height, depth);
                rt.Create();
                AssetDatabase.CreateAsset(rt, assetPath);
                return Success(assetPath, typeof(RenderTexture).FullName);
            });
        }

        public static UacfResponse CreateCubemap(JObject p)
        {
            var path = p["path"]?.ToString();
            var size = p["size"]?.Value<int>() ?? 128;
            var overwrite = p["overwrite"]?.Value<bool>() ?? false;

            if (string.IsNullOrWhiteSpace(path))
                return UacfResponse.Fail("INVALID_REQUEST", "path is required", null, 0);

            return CreateAsset(path, overwrite, ".cubemap", assetPath =>
            {
                var cubemap = new Cubemap(size, TextureFormat.RGBA32, false);
                AssetDatabase.CreateAsset(cubemap, assetPath);
                return Success(assetPath, typeof(Cubemap).FullName);
            });
        }

        public static UacfResponse CreateAsset(JObject p)
        {
            var path = p["path"]?.ToString();
            var type = p["type"]?.ToString();
            var overwrite = p["overwrite"]?.Value<bool>() ?? false;

            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(type))
                return UacfResponse.Fail("INVALID_REQUEST", "path and type are required", null, 0);

            var typeLower = type.ToLowerInvariant();
            switch (typeLower)
            {
                case "scriptableobject":
                    var subtype = p["subtype"]?.ToString();
                    if (string.IsNullOrWhiteSpace(subtype))
                        return UacfResponse.Fail("INVALID_REQUEST", "subtype is required for scriptableObject", null, 0);
                    var soParams = new JObject { ["path"] = path, ["type"] = subtype, ["overwrite"] = overwrite };
                    if (p["properties"] is JObject props) soParams["properties"] = props;
                    return CreateScriptableObject(soParams);
                case "material":
                    var matParams = new JObject { ["path"] = path, ["overwrite"] = overwrite };
                    if (p["shader"] != null) matParams["shader"] = p["shader"];
                    if (p["properties"] is JObject mp) matParams["properties"] = mp;
                    return CreateMaterial(matParams);
                case "physicmaterial":
                    var pmParams = new JObject { ["path"] = path, ["overwrite"] = overwrite };
                    if (p["properties"] is JObject pmp) pmParams["properties"] = pmp;
                    return CreatePhysicMaterial(pmParams);
                case "animationclip":
                    var animParams = new JObject { ["path"] = path, ["overwrite"] = overwrite };
                    if (p["wrapMode"] != null) animParams["wrapMode"] = p["wrapMode"];
                    if (p["curves"] != null) animParams["curves"] = p["curves"];
                    return CreateAnimationClip(animParams);
                case "panelsettings":
                    var panelParams = new JObject { ["path"] = path, ["overwrite"] = overwrite };
                    if (p["properties"] is JObject pp) panelParams["properties"] = pp;
                    return CreatePanelSettings(panelParams);
                case "font":
                    var fontParams = new JObject { ["path"] = path, ["overwrite"] = overwrite };
                    if (p["sourcePath"] != null) fontParams["sourcePath"] = p["sourcePath"];
                    else return UacfResponse.Fail("INVALID_REQUEST", "sourcePath is required for font", null, 0);
                    return CreateFont(fontParams);
                case "tmpfontasset":
                    var tmpParams = new JObject { ["path"] = path, ["overwrite"] = overwrite };
                    if (p["sourceFont"] != null) tmpParams["sourceFont"] = p["sourceFont"];
                    else return UacfResponse.Fail("INVALID_REQUEST", "sourceFont is required for tmpFontAsset", null, 0);
                    if (p["atlasResolution"] != null) tmpParams["atlasResolution"] = p["atlasResolution"];
                    return CreateTMPFontAsset(tmpParams);
                case "rendertexture":
                    var rtParams = new JObject { ["path"] = path, ["overwrite"] = overwrite };
                    if (p["width"] != null) rtParams["width"] = p["width"];
                    if (p["height"] != null) rtParams["height"] = p["height"];
                    if (p["depth"] != null) rtParams["depth"] = p["depth"];
                    return CreateRenderTexture(rtParams);
                case "cubemap":
                    var cmParams = new JObject { ["path"] = path, ["overwrite"] = overwrite };
                    if (p["size"] != null) cmParams["size"] = p["size"];
                    return CreateCubemap(cmParams);
                default:
                    return UacfResponse.Fail("INVALID_REQUEST", $"Unknown type '{type}'", "Use: scriptableObject, material, physicMaterial, animationClip, panelSettings, font, tmpFontAsset, renderTexture, cubemap", 0);
            }
        }

        public static UacfResponse CreateAnimationClip(JObject p)
        {
            var path = p["path"]?.ToString();
            var overwrite = p["overwrite"]?.Value<bool>() ?? false;
            var wrapMode = p["wrapMode"]?.ToString();
            var curves = p["curves"] as JArray;

            if (string.IsNullOrWhiteSpace(path))
                return UacfResponse.Fail("INVALID_REQUEST", "path is required", null, 0);

            return CreateAsset(path, overwrite, ".anim", assetPath =>
            {
                var clip = new AnimationClip();

                if (!string.IsNullOrWhiteSpace(wrapMode) && Enum.TryParse<WrapMode>(wrapMode, true, out var parsedWrap))
                    clip.wrapMode = parsedWrap;

                if (curves != null)
                {
                    foreach (var c in curves.OfType<JObject>())
                    {
                        var bindingPath = c["path"]?.ToString() ?? "";
                        var property = c["property"]?.ToString();
                        var typeName = c["type"]?.ToString();
                        var keyframesToken = c["keyframes"] as JArray;

                        if (string.IsNullOrWhiteSpace(property) || string.IsNullOrWhiteSpace(typeName) || keyframesToken == null)
                            continue;

                        var bindingType = TypeResolverService.Instance.Resolve(typeName) ?? Type.GetType(typeName);
                        if (bindingType == null)
                            continue;

                        var keyframes = new List<Keyframe>();
                        foreach (var k in keyframesToken.OfType<JObject>())
                        {
                            keyframes.Add(new Keyframe(
                                k["time"]?.Value<float>() ?? 0f,
                                k["value"]?.Value<float>() ?? 0f));
                        }

                        var curve = new AnimationCurve(keyframes.ToArray());
                        clip.SetCurve(bindingPath, bindingType, property, curve);
                    }
                }

                AssetDatabase.CreateAsset(clip, assetPath);
                return Success(assetPath, typeof(AnimationClip).FullName);
            });
        }

        private static UacfResponse CreateAsset(string path, bool overwrite, string defaultExtension, Func<string, UacfResponse> factory)
        {
            var normalizedPath = NormalizeAssetPath(path, defaultExtension);
            if (normalizedPath == null)
                return UacfResponse.Fail("INVALID_REQUEST", "path must be under Assets/ and point to a file", null, 0);

            var folder = Path.GetDirectoryName(normalizedPath)?.Replace("\\", "/");
            if (!string.IsNullOrWhiteSpace(folder) && !AssetDatabaseService.CreateFolder(folder))
                return UacfResponse.Fail("CREATE_FAILED", $"Unable to create folder '{folder}'", null, 0);

            var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(normalizedPath);
            if (existing != null)
            {
                if (!overwrite)
                    return UacfResponse.Fail("ASSET_EXISTS", $"Asset already exists at '{normalizedPath}'", "Set overwrite=true to replace existing asset", 0);

                if (!AssetDatabase.DeleteAsset(normalizedPath))
                    return UacfResponse.Fail("CREATE_FAILED", $"Failed to delete existing asset at '{normalizedPath}'", null, 0);
            }

            var response = factory(normalizedPath);
            if (!response.Ok) return response;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return response;
        }

        private static UacfResponse Success(string path, string type)
        {
            return UacfResponse.Success(new
            {
                path,
                guid = AssetDatabase.AssetPathToGUID(path),
                type
            }, 0);
        }

        private static string NormalizeAssetPath(string path, string defaultExtension)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            var normalized = path.Replace("\\", "/").Trim();
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal))
                return null;

            if (normalized.EndsWith("/"))
                return null;

            if (Path.HasExtension(normalized))
                return normalized;

            return normalized + defaultExtension;
        }

        private static void ApplyObjectProperties(UnityEngine.Object target, JObject properties)
        {
            if (target == null || properties == null) return;

            foreach (var prop in properties.Properties())
            {
                var member = target.GetType().GetProperty(prop.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (member != null && member.CanWrite)
                {
                    var value = ConvertToken(prop.Value, member.PropertyType);
                    if (value != null || !member.PropertyType.IsValueType)
                        member.SetValue(target, value);
                    continue;
                }

                var field = target.GetType().GetField(prop.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (field != null)
                {
                    var value = ConvertToken(prop.Value, field.FieldType);
                    if (value != null || !field.FieldType.IsValueType)
                        field.SetValue(target, value);
                }
            }

            EditorUtility.SetDirty(target);
        }

        private static void ApplyMaterialProperties(Material material, JObject properties)
        {
            if (material == null || properties == null) return;

            foreach (var p in properties.Properties())
            {
                var name = p.Name;
                var token = p.Value;
                if (!material.HasProperty(name)) continue;

                var vector = TryReadVector4(token);
                if (vector.HasValue)
                {
                    var v = vector.Value;
                    if (TryReadColor(token, out var color))
                        material.SetColor(name, color);
                    else
                        material.SetVector(name, v);
                    continue;
                }

                if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float || token.Type == JTokenType.Boolean)
                {
                    material.SetFloat(name, token.Value<float>());
                    continue;
                }

                if (token.Type == JTokenType.String)
                {
                    var assetPath = token.Value<string>();
                    var tex = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
                    if (tex != null)
                        material.SetTexture(name, tex);
                }
            }

            EditorUtility.SetDirty(material);
        }

        private static object ConvertToken(JToken token, Type targetType)
        {
            if (token == null || token.Type == JTokenType.Null) return null;

            if (targetType == typeof(string)) return token.ToString();
            if (targetType == typeof(int)) return token.Value<int>();
            if (targetType == typeof(float)) return token.Value<float>();
            if (targetType == typeof(double)) return token.Value<double>();
            if (targetType == typeof(bool)) return token.Value<bool>();
            if (targetType == typeof(long)) return token.Value<long>();

            if (targetType.IsEnum)
            {
                if (token.Type == JTokenType.String)
                    return Enum.Parse(targetType, token.ToString(), true);
                return Enum.ToObject(targetType, token.Value<int>());
            }

            if (targetType == typeof(Color) && TryReadColor(token, out var color))
                return color;

            if (targetType == typeof(Vector2))
            {
                var v = TryReadVector4(token);
                if (v.HasValue) return new Vector2(v.Value.x, v.Value.y);
            }

            if (targetType == typeof(Vector3))
            {
                var v = TryReadVector4(token);
                if (v.HasValue) return new Vector3(v.Value.x, v.Value.y, v.Value.z);
            }

            if (targetType == typeof(Vector4))
            {
                var v = TryReadVector4(token);
                if (v.HasValue) return v.Value;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(targetType) && token.Type == JTokenType.String)
                return AssetDatabase.LoadAssetAtPath(token.Value<string>(), targetType);

            try
            {
                return token.ToObject(targetType);
            }
            catch
            {
                return null;
            }
        }

        private static bool TryReadColor(JToken token, out Color color)
        {
            color = default;
            if (token is not JObject obj) return false;

            if (obj.TryGetValue("r", StringComparison.OrdinalIgnoreCase, out var rTok) &&
                obj.TryGetValue("g", StringComparison.OrdinalIgnoreCase, out var gTok) &&
                obj.TryGetValue("b", StringComparison.OrdinalIgnoreCase, out var bTok))
            {
                var a = obj.TryGetValue("a", StringComparison.OrdinalIgnoreCase, out var aTok) ? aTok.Value<float>() : 1f;
                color = new Color(rTok.Value<float>(), gTok.Value<float>(), bTok.Value<float>(), a);
                return true;
            }

            return false;
        }

        private static Vector4? TryReadVector4(JToken token)
        {
            if (token is JArray arr)
            {
                if (arr.Count >= 2)
                {
                    var x = arr[0]?.Value<float>() ?? 0f;
                    var y = arr[1]?.Value<float>() ?? 0f;
                    var z = arr.Count > 2 ? arr[2]?.Value<float>() ?? 0f : 0f;
                    var w = arr.Count > 3 ? arr[3]?.Value<float>() ?? 0f : 0f;
                    return new Vector4(x, y, z, w);
                }
            }

            if (token is JObject obj)
            {
                if (obj.TryGetValue("x", StringComparison.OrdinalIgnoreCase, out var xTok) &&
                    obj.TryGetValue("y", StringComparison.OrdinalIgnoreCase, out var yTok))
                {
                    var z = obj.TryGetValue("z", StringComparison.OrdinalIgnoreCase, out var zTok) ? zTok.Value<float>() : 0f;
                    var w = obj.TryGetValue("w", StringComparison.OrdinalIgnoreCase, out var wTok) ? wTok.Value<float>() : 0f;
                    return new Vector4(xTok.Value<float>(), yTok.Value<float>(), z, w);
                }

                if (TryReadColor(obj, out var color))
                    return new Vector4(color.r, color.g, color.b, color.a);
            }

            return null;
        }

        private static bool TryReadColor(JObject obj, out Color color)
        {
            color = default;
            if (obj.TryGetValue("r", StringComparison.OrdinalIgnoreCase, out var rTok) &&
                obj.TryGetValue("g", StringComparison.OrdinalIgnoreCase, out var gTok) &&
                obj.TryGetValue("b", StringComparison.OrdinalIgnoreCase, out var bTok))
            {
                var a = obj.TryGetValue("a", StringComparison.OrdinalIgnoreCase, out var aTok) ? aTok.Value<float>() : 1f;
                color = new Color(rTok.Value<float>(), gTok.Value<float>(), bTok.Value<float>(), a);
                return true;
            }
            return false;
        }
    }
}
