using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace UACF.Services
{
    public class TypeResolverService
    {
        private static TypeResolverService _instance;
        public static TypeResolverService Instance => _instance ??= new TypeResolverService();

        private readonly Dictionary<string, Type> _cache = new Dictionary<string, Type>();

        private TypeResolverService()
        {
            CompilationPipeline.compilationFinished += _ => _cache.Clear();
        }

        public Type Resolve(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            if (_cache.TryGetValue(typeName, out var cached)) return cached;

            var type = ResolveInternal(typeName);
            if (type != null)
                _cache[typeName] = type;
            return type;
        }

        private Type ResolveInternal(string typeName)
        {
            var t = Type.GetType(typeName);
            if (t != null) return t;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    t = asm.GetTypes().FirstOrDefault(x =>
                        x.Name == typeName || x.FullName == typeName);
                    if (t != null) return t;
                }
                catch { }
            }

            var namespaces = new[] { "UnityEngine", "UnityEngine.UI", "UnityEngine.Rendering", "UnityEngine.EventSystems" };
            foreach (var ns in namespaces)
            {
                t = Type.GetType(ns + "." + typeName);
                if (t != null) return t;
            }

            var guids = AssetDatabase.FindAssets("t:MonoScript " + typeName);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script?.GetClass()?.Name == typeName)
                    return script.GetClass();
            }

            return null;
        }

        public string[] GetSuggestions(string invalidName, int maxDistance = 3)
        {
            var suggestions = new List<(string, int)>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var t in asm.GetTypes().Where(x => typeof(Component).IsAssignableFrom(x)))
                    {
                        var dist = LevenshteinDistance(invalidName, t.Name);
                        if (dist <= maxDistance)
                            suggestions.Add((t.Name, dist));
                    }
                }
                catch { }
            }
            return suggestions.OrderBy(x => x.Item2).Select(x => x.Item1).Distinct().Take(5).ToArray();
        }

        private static int LevenshteinDistance(string a, string b)
        {
            var d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;
            for (int i = 1; i <= a.Length; i++)
                for (int j = 1; j <= b.Length; j++)
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            return d[a.Length, b.Length];
        }
    }
}
