using UnityEngine;


namespace ExpandedAiFramework
{
    public interface IMapData : ISerializedData
    {
        string Name { get; }
        bool Transient { get; }
        public Vector3 AnchorPosition { get; }
        public bool Claimed { get; }
        public bool Claim();
    }
}
