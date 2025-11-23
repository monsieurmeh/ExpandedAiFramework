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
            MaybeLoadWanderPath();
            return false;
        }


        private void MaybeLoadWanderPath()
        {
            if (mFetchingWanderPath) return;
            mFetchingWanderPath = true;

            if (!TryLoadSavedWanderPath()) RequestNearestWanderPath();
        }


        private bool TryLoadSavedWanderPath()
        {
            if (!ValidateSavedWanderPathData(mModDataProxy, out Guid spotGuid)) return false;

            mAi.BaseAi.m_TargetWaypointIndex = mWaypointIndex;
            RequestSavedWanderPath(spotGuid);
            return true;
        }

        private void RequestSavedWanderPath(Guid spotGuid)
        {
            mDataManager.ScheduleMapDataRequest<WanderPath>(
                new GetNearestMapDataRequest<WanderPath>(
                    mAi.transform.position,
                    mModDataProxy.Scene,
                    OnSavedWanderPathResult,
                    false,
                    wp => wp.WanderPathFlags == WanderPath.DefaultFlags,
                    3
                )
            );
        }

        private void OnSavedWanderPathResult(WanderPath path, RequestResult result)
        {
            if (result == RequestResult.Succeeded)
            {
                AttachWanderPath(path, result);
            }
            else
            {
                mAi.LogTraceInstanced($"Failed to fetch saved WanderPath, requesting nearest...", LogCategoryFlags.Ai);
                RequestNearestWanderPath();
            }
        }

        private void RequestNearestWanderPath()
        {
            mDataManager.ScheduleMapDataRequest<WanderPath>(
                new GetNearestMapDataRequest<WanderPath>(
                    mAi.transform.position,
                    mModDataProxy.Scene,
                    OnNearestWanderPathResult,
                    false,
                    wp => wp.WanderPathFlags == WanderPath.DefaultFlags,
                    3
                )
            );
        }

        private void OnNearestWanderPathResult(WanderPath path, RequestResult result)
        {
            mNewPath = true;
            AttachWanderPath(path, result);
        }

        private void AttachWanderPath(WanderPath path, RequestResult result)
        {
            mPath = path;
            mFetchingWanderPath = false;
            if (result != RequestResult.Succeeded)
            {
                mAi.LogErrorInstanced($"Failed to attach wander path!");
                return;
            }
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

        private bool ValidateSavedWanderPathData(SpawnModDataProxy proxy, out Guid spotGuid)
        {
            spotGuid = Guid.Empty;
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
            if (!Guid.TryParse(proxy.CustomData[0], out spotGuid))
            {
                mAi.LogTraceInstanced($"Could not parse spot guid", LogCategoryFlags.Ai);
                return false;
            }
            if (!int.TryParse(proxy.CustomData[1], out mWaypointIndex))
            {
                mAi.LogTraceInstanced($"Could not parse last waypoint index from proxy", LogCategoryFlags.Ai);
                return false;
            }
            return true;
        }
    }
}
