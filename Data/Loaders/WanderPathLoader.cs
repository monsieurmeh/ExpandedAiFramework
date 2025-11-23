using UnityEngine;

namespace ExpandedAiFramework
{
    public class WanderPathLoader : MapDataLoader<WanderPath>
    {
        protected int mWaypointIndex = -1;

        public WanderPathLoader(CustomBaseAi ai, SpawnModDataProxy proxy, DataManager dataManager, Func<WanderPath, bool> filter = null) : base(ai, proxy, dataManager, filter) 
        {
            if (filter == null)
            {
                filter = wp => wp.WanderPathFlags == WanderPath.DefaultFlags;
            }
        }

        protected override bool ValidateDetails()
        {
            if (mProxy.CustomData.Length < 2)
            {
                mAi.LogTraceInstanced($"Not enough length to proxy custom data (waypoint index required)", LogCategoryFlags.Ai);
                return false;
            }
            if (!int.TryParse(mProxy.CustomData[1], out mWaypointIndex))
            {
                mAi.LogTraceInstanced($"Could not parse last waypoint index from proxy", LogCategoryFlags.Ai);
                return false;
            }
            return true;
        }

        protected override void SaveDetails() =>  mProxy.CustomData = [mProxy.CustomData[0], mWaypointIndex.ToString()];
        

        protected override void AttachDetails()
        {
            mAi.BaseAi.m_Waypoints = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>(mData.PathPoints.Length);
            for (int i = 0, iMax = mAi.BaseAi.m_Waypoints.Length; i < iMax; i++)
            {
                mAi.BaseAi.m_Waypoints[i] = mData.PathPoints[i];
            }
        }
    }
}
