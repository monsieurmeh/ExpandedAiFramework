using UnityEngine;

namespace ExpandedAiFramework
{
    public class WanderPathLoader
    {
        protected WanderPath mPath;
        protected CustomBaseAi mAi;
        protected SpawnModDataProxy mModDataProxy;
        protected DataManager mDataManager;

        protected bool mWanderPathConnected = false;
        protected bool mFetchingWanderPath = false;
        protected bool mNewPath = false;

        public WanderPath Path { get { return mPath; } }
        public bool NewPath { get { return mNewPath; } }
        public bool Loaded { get { return mWanderPathConnected; } }

        public WanderPathLoader(CustomBaseAi ai, SpawnModDataProxy proxy, DataManager dataManager) 
        {
            mAi = ai;
            mModDataProxy = proxy;
            mDataManager = dataManager;
        }

        public bool CheckWanderPathReady()
        {
            if (mWanderPathConnected) return true;
            MaybeLoadWanderPath(mModDataProxy);
            return false;
        }


        private void MaybeLoadWanderPath(SpawnModDataProxy proxy)
        {
            if (mFetchingWanderPath) return;

            mFetchingWanderPath = true;
            if (TryLoadSavedWanderPath(proxy)) return;

            mDataManager.ScheduleMapDataRequest<WanderPath>(new GetNearestMapDataRequest<WanderPath>(mAi.transform.position, proxy.Scene, (nearestSpot, result2) =>
            {
                mAi.LogTraceInstanced($"Found NEW nearest hiding spot with guid <<<{nearestSpot}>>>", LogCategoryFlags.Ai);
                AttachWanderPath(nearestSpot);
                mNewPath = true;
            }, false, null, 3));

        }


        private bool TryLoadSavedWanderPath(SpawnModDataProxy proxy)
        {
            if (proxy == null
                || proxy.CustomData == null
                || proxy.CustomData.Length < 2)
            {
                mAi.LogTraceInstanced($"Null proxy, null proxy custom data or not enough length to proxy custom data (guid and waypoint index required)", LogCategoryFlags.Ai);
                return false;
            }
            Guid spotGuid = new Guid((string)proxy.CustomData[0]);
            if (spotGuid == Guid.Empty)
            {
                mAi.LogTraceInstanced($"Proxy spot guid is empty", LogCategoryFlags.Ai);
                return false;
            }
            if (!int.TryParse(proxy.CustomData[1], out int waypointIndex))
            {
                mAi.LogTraceInstanced($"Could not parse last waypoint index from proxy", LogCategoryFlags.Ai);
                return false;
            }
            mAi.BaseAi.m_TargetWaypointIndex = waypointIndex;
            mDataManager.ScheduleMapDataRequest<WanderPath>(new GetDataByGuidRequest<WanderPath>(spotGuid, proxy.Scene, (spot, result) =>
            {
                if (result != RequestResult.Succeeded)
                {
                    mAi.LogTraceInstanced($"Can't get WanderPath with guid <<<{spotGuid}>>> from dictionary, requesting nearest instead...", LogCategoryFlags.Ai);
                    mDataManager.ScheduleMapDataRequest<WanderPath>(new GetNearestMapDataRequest<WanderPath>(mAi.transform.position, proxy.Scene, (nearestSpot, result2) =>
                    {
                        mAi.LogTraceInstanced($"Found NEW nearest WanderPath with guid <<<{nearestSpot}>>>", LogCategoryFlags.Ai);
                        AttachWanderPath(nearestSpot, waypointIndex);
                        mNewPath = true;
                    }, false, (wp => wp.WanderPathFlags == WanderPath.DefaultFlags), 3));
                }
                else
                {
                    mAi.LogTraceInstanced($"Found saved WanderPath with guid <<<{spotGuid}>>>", LogCategoryFlags.Ai);
                    AttachWanderPath(spot, waypointIndex);
                }
            }, false));
            return true;
        }



        public void AttachWanderPath(WanderPath path, int currentIndex = 0)
        {
            mPath = path;
            mFetchingWanderPath = false;
            if (mModDataProxy != null)
            {
                mModDataProxy.CustomData = [path.Guid.ToString(), currentIndex.ToString()];
            }
            mAi.BaseAi.m_Waypoints = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>(path.PathPoints.Length);
            for (int i = 0, iMax = mAi.BaseAi.m_Waypoints.Length; i < iMax; i++)
            {
                mAi.BaseAi.m_Waypoints[i] = path.PathPoints[i];
            }
            mWanderPathConnected = true;
            path.Claim();
        }
    }
}
