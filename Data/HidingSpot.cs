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

        public override int GetHashCode() => (Name, Scene).GetHashCode();
        public override bool Equals(object obj) => this.Equals(obj as HidingSpot);
        public bool Equals(HidingSpot spot)
        {
            if (spot is null)
            {
                return false;
            }

            if (GetType() != spot.GetType())
            {
                return false;
            }

            return (Name == spot.Name) && (Scene == spot.Scene);
        }
        public static bool operator ==(HidingSpot lhs, HidingSpot rhs)
        {
            if (lhs is null)
            {
                if (rhs is null)
                {
                    return true;
                }
                return false;
            }
            return lhs.Equals(rhs);
        }
        public static bool operator !=(HidingSpot lhs, HidingSpot rhs) => !(lhs == rhs);
    }
}