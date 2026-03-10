using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace UACF.Services
{
    public class AssetDatabaseService
    {
        public static void Refresh()
        {
            AssetDatabase.Refresh();
        }

        public static string[] FindAssets(string filter, string path = null)
        {
            var guids = string.IsNullOrEmpty(path)
                ? AssetDatabase.FindAssets(filter)
                : AssetDatabase.FindAssets(filter, new[] { path });
            var result = new List<string>();
            foreach (var guid in guids)
            {
                if (!string.IsNullOrEmpty(guid))
                    result.Add(AssetDatabase.GUIDToAssetPath(guid));
            }
            return result.ToArray();
        }

        public static (string guid, string path, string type)[] FindAssetsWithDetails(string filter, string path = null)
        {
            var guids = string.IsNullOrEmpty(path)
                ? AssetDatabase.FindAssets(filter)
                : AssetDatabase.FindAssets(filter, new[] { path });
            var result = new List<(string, string, string)>();
            foreach (var guid in guids)
            {
                if (string.IsNullOrEmpty(guid)) continue;
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                var typeName = obj != null ? obj.GetType().Name : "Unknown";
                result.Add((guid, assetPath, typeName));
            }
            return result.ToArray();
        }

        public static bool CreateFolder(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (AssetDatabase.IsValidFolder(path)) return true;
            var parts = path.Split('/');
            var current = "";
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                var next = string.IsNullOrEmpty(current) ? part : current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(string.IsNullOrEmpty(current) ? "Assets" : current, part);
                }
                current = next;
            }
            return AssetDatabase.IsValidFolder(path);
        }

        public static bool DeleteAsset(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return AssetDatabase.DeleteAsset(path);
        }
    }
}
