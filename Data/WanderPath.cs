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

        public WanderPath(string name, Vector3[] pathPoints, string scene, WanderPathTypes wanderPathType = WanderPathTypes.IndividualPath, bool transient = false) : base(name, scene, pathPoints[0], transient)
        {
            mPathPoints = pathPoints;
            mWanderPathType = wanderPathType;
            UpdateCachedString();
        }

        public override void UpdateCachedString()
        {
            mAnchorPosition = mPathPoints[0]; //REMOVE THIS!!!
            base.UpdateCachedString();
            mCachedString += $" with type {WanderPathType}";
        }
    }
}