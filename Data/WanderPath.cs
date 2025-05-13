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
    }
}