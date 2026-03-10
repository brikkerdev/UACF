using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UACF.Core;
using UACF.Services;

namespace UACF.Handlers
{
    public static class ProjectHandler
    {
        public static void Register(RequestRouter router)
        {
            router.Register("GET", "/api/project/settings", HandleSettings);
            router.Register("POST", "/api/project/add-tag", HandleAddTag);
            router.Register("POST", "/api/project/set-layer", HandleSetLayer);
        }

        private static async Task HandleSettings(RequestContext ctx)
        {
            var category = ctx.QueryParams.TryGetValue("category", out var c) ? c : null;

            var data = await MainThreadDispatcher.Enqueue<object>(() =>
            {
                if (category == "tags")
                {
                    var tags = UnityEditorInternal.InternalEditorUtility.tags;
                    return new { tags = tags.ToList() };
                }
                if (category == "layers")
                {
                    var layers = new Dictionary<string, string>();
                    for (int i = 0; i < 32; i++)
                    {
                        var name = LayerMask.LayerToName(i);
                        if (!string.IsNullOrEmpty(name))
                            layers[i.ToString()] = name;
                    }
                    return new { layers = layers };
                }
                if (category == "sorting_layers")
                {
                    var layers = SortingLayer.layers.Select(s => s.name).ToArray();
                    return new { sorting_layers = layers };
                }
                return new { tags = UnityEditorInternal.InternalEditorUtility.tags.ToList(), layers = new Dictionary<string, string>() };
            });
            ctx.RespondOk(data);
        }

        private static async Task HandleAddTag(RequestContext ctx)
        {
            var body = await ctx.ReadBodyAsync<JObject>();
            var tag = body?["tag"]?.ToString();
            if (string.IsNullOrEmpty(tag))
            {
                ResponseHelper.InvalidRequest(ctx, "tag is required");
                return;
            }

            var result = await MainThreadDispatcher.Enqueue<object>(() =>
            {
                var tags = UnityEditorInternal.InternalEditorUtility.tags.ToList();
                if (tags.Contains(tag)) return new { added = false, message = "Tag already exists" };
                SerializedObject tagManager = new SerializedObject(
                    AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                SerializedProperty tagsProp = tagManager.FindProperty("tags");
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                tagManager.ApplyModifiedProperties();
                return new { added = true };
            });
            ctx.RespondOk(result);
        }

        private static async Task HandleSetLayer(RequestContext ctx)
        {
            var body = await ctx.ReadBodyAsync<JObject>();
            var layerIndex = body?["layer_index"]?.Value<int>() ?? -1;
            var name = body?["name"]?.ToString();
            if (layerIndex < 0 || layerIndex > 31 || string.IsNullOrEmpty(name))
            {
                ResponseHelper.InvalidRequest(ctx, "layer_index (0-31) and name are required");
                return;
            }

            var result = await MainThreadDispatcher.Enqueue<object>(() =>
            {
                SerializedObject tagManager = new SerializedObject(
                    AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                SerializedProperty layersProp = tagManager.FindProperty("layers");
                if (layerIndex < layersProp.arraySize)
                {
                    layersProp.GetArrayElementAtIndex(layerIndex).stringValue = name;
                    tagManager.ApplyModifiedProperties();
                    return new { set = true };
                }
                return new { set = false };
            });
            ctx.RespondOk(result);
        }
    }
}
