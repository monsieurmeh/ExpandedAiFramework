using MelonLoader.TinyJSON;
using UnityEngine;


namespace ExpandedAiFramework
{
    [Serializable]
    public class HidingSpot : MapData
    {
        [Include] private Quaternion mRotation;

        public Quaternion Rotation { get { return mRotation; } }
        public Vector3 Position { get { return mAnchorPosition; } }
        public HidingSpot() { }

        public HidingSpot(string name, Vector3 pos, Quaternion rot, string scene, bool transient = false) : base(name, scene, pos, transient)
        {
            mRotation = rot;
            UpdateCachedString();
        }
    }
}
