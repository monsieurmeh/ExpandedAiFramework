using UnityEngine;


namespace ExpandedAiFramework
{
    public class HidingSpot : MapData
    {
        private Quaternion mRotation;

        public Quaternion Rotation { get { return mRotation; } }
        public Vector3 Position { get { return mAnchorPosition; } }
        public HidingSpot() : base() { }

        public HidingSpot(string name, Vector3 pos, Quaternion rot, string scene, bool transient = false) : base(name, scene, pos, transient)
        {
            mRotation = rot;
        }
    }
}
