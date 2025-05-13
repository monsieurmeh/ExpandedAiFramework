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
    }
}