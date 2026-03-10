using System;
using Unity.Plastic.Newtonsoft.Json;

namespace UACF.Models
{
    [Serializable]
    public class GameObjectInfo
    {
        [JsonProperty("instance_id")]
        public int InstanceId;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("active")]
        public bool Active;

        [JsonProperty("tag")]
        public string Tag;

        [JsonProperty("layer")]
        public int Layer;

        [JsonProperty("layer_name")]
        public string LayerName;

        [JsonProperty("static")]
        public bool Static;

        [JsonProperty("transform")]
        public TransformInfo Transform;

        [JsonProperty("components")]
        public string[] Components;

        [JsonProperty("children")]
        public GameObjectInfo[] Children;

        [JsonProperty("path")]
        public string Path;

        [JsonProperty("active_self")]
        public bool ActiveSelf;

        [JsonProperty("active_hierarchy")]
        public bool ActiveHierarchy;
    }

    [Serializable]
    public class TransformInfo
    {
        [JsonProperty("local_position")]
        public Vector3Json LocalPosition;

        [JsonProperty("local_rotation")]
        public QuaternionJson LocalRotation;

        [JsonProperty("local_scale")]
        public Vector3Json LocalScale;

        [JsonProperty("position")]
        public Vector3Json Position;

        [JsonProperty("rotation")]
        public QuaternionJson Rotation;

        [JsonProperty("scale")]
        public Vector3Json Scale;
    }

    [Serializable]
    public class Vector3Json
    {
        [JsonProperty("x")]
        public float X;

        [JsonProperty("y")]
        public float Y;

        [JsonProperty("z")]
        public float Z;
    }

    [Serializable]
    public class QuaternionJson
    {
        [JsonProperty("x")]
        public float X;

        [JsonProperty("y")]
        public float Y;

        [JsonProperty("z")]
        public float Z;

        [JsonProperty("w")]
        public float W;
    }
}
