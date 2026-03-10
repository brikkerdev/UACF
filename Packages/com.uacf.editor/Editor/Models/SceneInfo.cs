using System;
using Unity.Plastic.Newtonsoft.Json;

namespace UACF.Models
{
    [Serializable]
    public class SceneInfo
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("path")]
        public string Path;

        [JsonProperty("is_loaded")]
        public bool IsLoaded;

        [JsonProperty("is_dirty")]
        public bool IsDirty;

        [JsonProperty("is_active")]
        public bool IsActive;

        [JsonProperty("root_count")]
        public int RootCount;

        [JsonProperty("build_index")]
        public int BuildIndex;
    }

    [Serializable]
    public class HierarchyResponse
    {
        [JsonProperty("scene")]
        public string Scene;

        [JsonProperty("objects")]
        public GameObjectInfo[] Objects;

        [JsonProperty("total_count")]
        public int TotalCount;
    }
}
