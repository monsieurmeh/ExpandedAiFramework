using UnityEngine;


namespace ExpandedAiFramework
{
    [Serializable]
    public class HidingSpot
    {
        public string Name;
        public string Scene;
        public Vector3 Position;
        public Vector2 Rotation;

        public HidingSpot() { }

        public HidingSpot(string name, Vector3 pos, Vector2 rot, string scene)
        {
            Name = name;
            Scene = scene;
            Position = pos;
            Rotation = rot;
        }


        public HidingSpot(string name, float x, float y, float z, float rx, float ry, string scene)
        {
            Name = name;
            Scene = scene;
            Position = new Vector3(x, y, z);
            Rotation = new Vector2(rx, ry);
        }
    }
}