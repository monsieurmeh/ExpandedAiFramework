using UnityEngine;
using static MelonLoader.bHaptics;


namespace ExpandedAiFramework
{
    [Serializable]
    public class HidingSpot
    {
        public string Name;
        public string Scene;
        public Vector3 Position;
        public Quaternion Rotation;

        public HidingSpot() { }

        public HidingSpot(string name, Vector3 pos, Quaternion rot, string scene)
        {
            Name = name;
            Scene = scene;
            Position = pos;
            Rotation = rot;
        }


        public override string ToString()
        {
            return $"HidingSpot {Name} at {Position}";
        }
    }
}