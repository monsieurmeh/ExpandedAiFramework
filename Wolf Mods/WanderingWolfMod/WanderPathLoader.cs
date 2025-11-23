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
        protected int mWaypointIndex = -1;

        public WanderPath Path { get { return mPath; } }
        public bool NewPath { get { return mNewPath; } }

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

            mDataManager.ScheduleMapDataRequest<WanderPath>(new GetNearestMapDataRequest<WanderPath>(mAi.transform.position, proxy.Scene, AttachNewWanderPath, false, null, 3));
        }


        private bool TryLoadSavedWanderPath(SpawnModDataProxy proxy)
        {
            if (!ValidateSavedWanderPathData(proxy, out Guid spotGuid)) return false;

            mAi.BaseAi.m_TargetWaypointIndex = mWaypointIndex;
            LoadSavedOrGetNewWanderPath(spotGuid, proxy);
            return true;
        }

        private bool ValidateSavedWanderPathData(SpawnModDataProxy proxy, out Guid spotGuid)
        {
            spotGuid = new Guid();
            if (proxy == null)
            {
                mAi.LogTraceInstanced($"Null proxy", LogCategoryFlags.Ai);
                return false;
            }
            if (proxy.CustomData == null)
            {
                mAi.LogTraceInstanced($"Null proxy custom data", LogCategoryFlags.Ai);
                return false;
            }
            if (proxy.CustomData.Length < 2)
            {
                mAi.LogTraceInstanced($"Not enough length to proxy custom data (guid and waypoint index required)", LogCategoryFlags.Ai);
                return false;
            }
            if (spotGuid == Guid.Empty)
            {
                mAi.LogTraceInstanced($"Proxy spot guid is empty", LogCategoryFlags.Ai);
                return false;
            }
            if (!int.TryParse(proxy.CustomData[1], out mWaypointIndex))
            {
                mAi.LogTraceInstanced($"Could not parse last waypoint index from proxy", LogCategoryFlags.Ai);
                return false;
            }
            return true;
        }

        private void LoadSavedOrGetNewWanderPath(Guid spotGuid, SpawnModDataProxy proxy)
        {
            mDataManager.ScheduleMapDataRequest<WanderPath>(new GetDataByGuidRequest<WanderPath>(spotGuid, proxy.Scene, (spot, result) =>
            {
                if (result != RequestResult.Succeeded)
                {
                    mAi.LogTraceInstanced($"Can't get WanderPath with guid <<<{spotGuid}>>> from dictionary, requesting nearest instead...", LogCategoryFlags.Ai);
                    mDataManager.ScheduleMapDataRequest<WanderPath>(new GetNearestMapDataRequest<WanderPath>(mAi.transform.position, proxy.Scene, AttachNewWanderPath, false, (wp => wp.WanderPathFlags == WanderPath.DefaultFlags), 3));
                }
                else
                {
                    AttachWanderPath(spot, result);
                }
            }, false));
        }


        private void AttachNewWanderPath(WanderPath path, RequestResult result)
        {
            mNewPath = true;
            AttachWanderPath(path, result);
        }

        private void AttachWanderPath(WanderPath path, RequestResult result)
        {
            mPath = path;
            mFetchingWanderPath = false;
            if (mModDataProxy != null)
            {
                mModDataProxy.CustomData = [path.Guid.ToString(), mWaypointIndex.ToString()];
            }
            mAi.BaseAi.m_Waypoints = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>(path.PathPoints.Length);
            for (int i = 0, iMax = mAi.BaseAi.m_Waypoints.Length; i < iMax; i++)
            {
                mAi.BaseAi.m_Waypoints[i] = path.PathPoints[i];
            }
            mWanderPathConnected = true;
            path.Claim();
            mAi.LogTraceInstanced($"Claimed WanderPath with guid <<<{path.Guid}>>>", LogCategoryFlags.Ai);
        }
    }
}
