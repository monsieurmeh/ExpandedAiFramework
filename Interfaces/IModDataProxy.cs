using UnityEngine;

namespace ExpandedAiFramework
{
    public interface IModDataProxy : ISerializedData
    {
        public string[] CustomData { get; set; }
        public Vector3 CurrentPosition { get; set; }
    }
}
