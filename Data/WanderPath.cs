using MelonLoader.TinyJSON;
using UnityEngine;


namespace ExpandedAiFramework
{
    public enum WanderPathTypes : int
    {
        IndividualPath = 0,
        SpawnRegionMigrationPath,
        PackPack,
        COUNT
    }

    [Serializable]
    public class WanderPath : MapData
    {
        [Include] private Vector3[] mPathPoints;
        [Include] private WanderPathTypes mWanderPathType = WanderPathTypes.IndividualPath;

        public Vector3[] PathPoints { get { return mPathPoints; } }
        public WanderPathTypes WanderPathType { get { return mWanderPathType; } }

        public WanderPath() { }

        public WanderPath(string name, Vector3[] pathPoints, string scene, string filePath, WanderPathTypes wanderPathType = WanderPathTypes.IndividualPath, bool transient = false) : base(name, scene, pathPoints[0], filePath, transient)
        {
            mPathPoints = pathPoints;
            mWanderPathType = wanderPathType;
            mCachedString += $" with type {WanderPathType}";
        }
    }
}