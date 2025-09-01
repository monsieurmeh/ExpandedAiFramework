﻿using Il2Cpp;
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
            if (mModDataProxy != null && mModDataProxy.AsyncProcessing)
            {
                return false;
            }

            if (!mWanderPathConnected)
            {
                if (!mFetchingWanderPath)
                {
                    GetWanderpath(mModDataProxy);
                }
                return false;
            }

            mBaseAi.m_MoveAgent.transform.position = mWanderPath.PathPoints[0];
            mBaseAi.m_MoveAgent.Warp(mWanderPath.PathPoints[0], 2.0f, true, -1);
            SetDefaultAiMode();
            return true;
        }


        private void GetWanderpath(SpawnModDataProxy proxy)
        {
            if (TryGetSavedWanderPath(proxy))
            {
                return;
            }
            mManager.DataManager.ScheduleMapDataRequest<WanderPath>(new GetNearestMapDataRequest<WanderPath>(mBaseAi.transform.position, proxy.Scene, (nearestSpot, result2) =>
            {
                this.LogTraceInstanced($"Found NEW nearest hiding spot with guid <<<{nearestSpot}>>>");
                AttachWanderPath(nearestSpot);
            }, false, 3));

        }


        private bool TryGetSavedWanderPath(SpawnModDataProxy proxy)
        {
            mFetchingWanderPath = true;
            if (proxy == null
                || proxy.CustomData == null
                || proxy.CustomData.Length == 0)
            {
                this.LogTraceInstanced($"Null proxy, null proxy custom data or no length to proxy custom data");
                return false;
            }
            Guid spotGuid = new Guid((string)proxy.CustomData[0]);
            if (spotGuid == Guid.Empty)
            {
                this.LogTraceInstanced($"Proxy spot guid is empty");
                return false;
            }
            mManager.DataManager.ScheduleMapDataRequest<WanderPath>(new GetDataByGuidRequest<WanderPath>(spotGuid, proxy.Scene, (spot, result) =>
            {
                if (result != RequestResult.Succeeded)
                {
                    this.LogTraceInstanced($"Can't get WanderPath with guid <<<{spotGuid}>>> from dictionary, requesting nearest instead...");
                    mManager.DataManager.ScheduleMapDataRequest<WanderPath>(new GetNearestMapDataRequest<WanderPath>(mBaseAi.transform.position, proxy.Scene, (nearestSpot, result2) =>
                    {
                        this.LogTraceInstanced($"Found NEW nearest WanderPath with guid <<<{nearestSpot}>>>");
                        AttachWanderPath(nearestSpot);
                    }, false, 3));
                }
                else
                {
                    this.LogTraceInstanced($"Found saved WanderPath with guid <<<{spotGuid}>>>");
                    AttachWanderPath(spot);
                }
            }, false));
            return true;
        }



        public void AttachWanderPath(WanderPath path)
        {
            mWanderPath = path;
            mFetchingWanderPath = false;
            if (mModDataProxy != null)
            {
                mModDataProxy.CustomData = [path.Guid.ToString()];
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
                    this.LogVerboseInstanced($"ProcessCustom: CurrentMode is {CurrentMode}, routing to ProcessFollowWaypointsCustom.");
                    return ProcessFollowWaypointsCustom();
                default:
                    this.LogVerboseInstanced($"ProcessCustom: CurrentMode is {CurrentMode}, deferring.");
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
