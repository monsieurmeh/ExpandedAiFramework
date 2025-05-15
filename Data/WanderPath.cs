using UnityEngine;


namespace ExpandedAiFramework
{

    [Serializable]
    public class WanderPath
    {
        public string Name;
        public string Scene;
        public Vector3[] PathPoints;

        public WanderPath() { }

        public WanderPath(string name, Vector3[] pathPoints, string scene)
        {
            Name = name;
            Scene = scene;
            PathPoints = pathPoints;
        }


        public static implicit operator Vector3(WanderPath path) => path.PathPoints[0];


        public override string ToString()
        {
            return $"WanderPath {Name} starting at {PathPoints[0]} with {PathPoints.Length} path points";
        }

        public override int GetHashCode() => (Name, Scene).GetHashCode();
        public override bool Equals(object obj) => this.Equals(obj as WanderPath);
        public bool Equals(WanderPath spot)
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
        public static bool operator ==(WanderPath lhs, WanderPath rhs)
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
        public static bool operator !=(WanderPath lhs, WanderPath rhs) => !(lhs == rhs);
    }
}