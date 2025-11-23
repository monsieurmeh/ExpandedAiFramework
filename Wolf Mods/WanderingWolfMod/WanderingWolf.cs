using Il2Cpp;
using UnityEngine;


namespace ExpandedAiFramework.WanderingWolfMod
{
    [RegisterTypeInIl2Cpp]
    public class WanderingWolf : BaseWolf
    {
        internal static WanderingWolfSettings WanderingWolfSettings;

        protected WanderPath mWanderPath;
        protected bool mWanderPathConnected = false;
        protected bool mFetchingWanderPath = false;
        protected bool mWarpToFirstPoint = false;


        public WanderingWolf(IntPtr ptr) : base(ptr) { }
        protected override float m_MinWaypointDistance { get { return 100.0f; } }
        protected override float m_MaxWaypointDistance { get { return 1000.0f; } }
        public override Color DebugHighlightColor { get { return Color.green; } }


        public override void Initialize(BaseAi ai, TimeOfDay timeOfDay, SpawnRegion spawnRegion, SpawnModDataProxy proxy)//, EAFManager manager)
        {
            base.Initialize(ai, timeOfDay, spawnRegion, proxy);//, manager);

            mBaseAi.m_DefaultMode = AiMode.FollowWaypoints;
            mBaseAi.m_StartMode = AiMode.FollowWaypoints;
            mBaseAi.m_CurrentMode = AiMode.FollowWaypoints;
            mBaseAi.m_WaypointCompletionBehaviour = BaseAi.WaypointCompletionBehaviouir.Restart;
            mBaseAi.m_TargetWaypointIndex = 0;
        }

        protected override bool FirstFrameCustom()
        {
            if (mModDataProxy != null && mModDataProxy.AsyncProcessing) return false;
            if (!TryLoadWanderPath()) return false; // Halt execution until this passes

            MaybeWarpToFirstPoint();
            SetDefaultAiMode();
            return true;
        }

        private bool TryLoadWanderPath()
        {
            if (mWanderPathConnected) return true;
            MaybeGetWanderpath(mModDataProxy);
            return false;
        }


        private void MaybeWarpToFirstPoint()
        {
            if (!mWarpToFirstPoint) return;

            this.LogTraceInstanced($"FirstFrameCustom: Warping to wanderpath start at {mWanderPath.PathPoints[mBaseAi.m_TargetWaypointIndex]} and setting wander mode", LogCategoryFlags.Ai);
            mBaseAi.m_MoveAgent.transform.position = mWanderPath.PathPoints[mBaseAi.m_TargetWaypointIndex];
            mBaseAi.m_MoveAgent.Warp(mWanderPath.PathPoints[mBaseAi.m_TargetWaypointIndex], 2.0f, true, -1);
        }


        private void MaybeGetWanderpath(SpawnModDataProxy proxy)
        {
            if (mFetchingWanderPath) return;

            if (TryGetSavedWanderPath(proxy))
            {
                return;
            }
            mManager.DataManager.ScheduleMapDataRequest<WanderPath>(new GetNearestMapDataRequest<WanderPath>(mBaseAi.transform.position, proxy.Scene, (nearestSpot, result2) =>
            {
                this.LogTraceInstanced($"Found NEW nearest hiding spot with guid <<<{nearestSpot}>>>", LogCategoryFlags.Ai);
                AttachWanderPath(nearestSpot);
            }, false, null, 3));

        }


        private bool TryGetSavedWanderPath(SpawnModDataProxy proxy)
        {
            mFetchingWanderPath = true;
            if (proxy == null
                || proxy.CustomData == null
                || proxy.CustomData.Length < 2)
            {
                this.LogTraceInstanced($"Null proxy, null proxy custom data or not enough length to proxy custom data (guid and waypoint index required)", LogCategoryFlags.Ai);
                return false;
            }
            Guid spotGuid = new Guid((string)proxy.CustomData[0]);
            if (spotGuid == Guid.Empty)
            {
                this.LogTraceInstanced($"Proxy spot guid is empty", LogCategoryFlags.Ai);
                return false;
            }
            if (!int.TryParse(proxy.CustomData[1], out int waypointIndex))
            {
                this.LogTraceInstanced($"Could not parse last waypoint index from proxy", LogCategoryFlags.Ai);
                return false;
            }
            mBaseAi.m_TargetWaypointIndex = waypointIndex;
            mManager.DataManager.ScheduleMapDataRequest<WanderPath>(new GetDataByGuidRequest<WanderPath>(spotGuid, proxy.Scene, (spot, result) =>
            {
                if (result != RequestResult.Succeeded)
                {
                    this.LogTraceInstanced($"Can't get WanderPath with guid <<<{spotGuid}>>> from dictionary, requesting nearest instead...", LogCategoryFlags.Ai);
                    mManager.DataManager.ScheduleMapDataRequest<WanderPath>(new GetNearestMapDataRequest<WanderPath>(mBaseAi.transform.position, proxy.Scene, (nearestSpot, result2) =>
                    {
                        this.LogTraceInstanced($"Found NEW nearest WanderPath with guid <<<{nearestSpot}>>>", LogCategoryFlags.Ai);
                        AttachWanderPath(nearestSpot, waypointIndex);
                    }, false, (wp => wp.WanderPathFlags == WanderPath.DefaultFlags), 3));
                }
                else
                {
                    this.LogTraceInstanced($"Found saved WanderPath with guid <<<{spotGuid}>>>", LogCategoryFlags.Ai);
                    AttachWanderPath(spot, waypointIndex);
                }
            }, false));
            return true;
        }



        public void AttachWanderPath(WanderPath path, int currentIndex = 0)
        {
            mWanderPath = path;
            mFetchingWanderPath = false;
            if (mModDataProxy != null)
            {
                mModDataProxy.CustomData = [path.Guid.ToString(), currentIndex.ToString()];
            }
            mBaseAi.m_Waypoints = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>(mWanderPath.PathPoints.Length);
            for (int i = 0, iMax = mBaseAi.m_Waypoints.Length; i < iMax; i++)
            {
                mBaseAi.m_Waypoints[i] = mWanderPath.PathPoints[i];
            }
            mWanderPathConnected = true;
            path.Claim();
        }


        protected override bool ProcessCustom()
        {
            switch (CurrentMode)
            {
                case AiMode.FollowWaypoints:
                    this.LogTraceInstanced($"ProcessCustom: CurrentMode is {CurrentMode}, routing to ProcessFollowWaypointsCustom.", LogCategoryFlags.Ai);
                    return ProcessFollowWaypointsCustom();
                default:
                    this.LogTraceInstanced($"ProcessCustom: CurrentMode is {CurrentMode}, deferring.", LogCategoryFlags.Ai);
                    return base.ProcessCustom();
            }
        }

        protected bool ProcessFollowWaypointsCustom()
        {
            if (!mWanderPathConnected)
            {
                // Prevent vanilla running without wander path connected
                return false;
            }
            return true;
        }


        protected override bool TestIsImposterCustom(out bool isImposter)
        {
            isImposter = false;
            return false;
        }
    }
}
