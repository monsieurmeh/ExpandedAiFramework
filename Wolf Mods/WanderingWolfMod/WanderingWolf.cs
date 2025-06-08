using Il2Cpp;
using UnityEngine;


namespace ExpandedAiFramework.WanderingWolfMod
{
    [RegisterTypeInIl2Cpp]
    public class WanderingWolf : BaseWolf
    {
        internal static WanderingWolfSettings WanderingWolfSettings = new WanderingWolfSettings();

        protected WanderPath mWanderPath;
        protected bool mWanderPathConnected = false;
            
        public WanderingWolf(IntPtr ptr) : base(ptr) { }
        protected override float m_MinWaypointDistance { get { return 100.0f; } }
        protected override float m_MaxWaypointDistance { get { return 1000.0f; } }
        

        public override void Initialize(BaseAi ai, TimeOfDay timeOfDay, SpawnRegion spawnRegion, SpawnModDataProxy proxy)//, EAFManager manager)
        {
            base.Initialize(ai, timeOfDay, spawnRegion, proxy);//, manager);

            mBaseAi.m_DefaultMode = AiMode.FollowWaypoints;
            mBaseAi.m_StartMode = AiMode.FollowWaypoints;
            mBaseAi.m_CurrentMode = AiMode.FollowWaypoints;
            mBaseAi.m_WaypointCompletionBehaviour = BaseAi.WaypointCompletionBehaviouir.Restart;
            mBaseAi.m_TargetWaypointIndex = 0;
        }


        private bool TryGetSavedWanderPath(SpawnModDataProxy proxy)
        {
            if (proxy == null
                || proxy.CustomData == null
                || proxy.CustomData.Length == 0)
            { 
                return false;
            }
            Guid spotGuid = (Guid)proxy.CustomData[0];
            if (spotGuid == Guid.Empty)
            {
                return false;
            }
            if (!mManager.DataManager.AvailableWanderPaths.TryGetValue(spotGuid, out WanderPath wanderPath))
            {
                return false;
            }
            AttachWanderPath(wanderPath);
            return true;
        }


        protected override bool FirstFrameCustom()
        {
            if (mModDataProxy != null && mModDataProxy.AsyncProcessing)
            {
                return false;
            }

            if (!mWanderPathConnected)
            {
                if (!TryGetSavedWanderPath(mModDataProxy))
                {
                    mManager.DataManager.GetNearestWanderPathAsync(mBaseAi.transform.position, WanderPathTypes.IndividualPath, AttachWanderPath, 3);
                }
            }
            mBaseAi.m_MoveAgent.transform.position = mWanderPath.PathPoints[0];
            mBaseAi.m_MoveAgent.Warp(mWanderPath.PathPoints[0], 2.0f, true, -1);
            SetDefaultAiMode();
            return true;
        }


        public void AttachWanderPath(WanderPath path)
        {
            mWanderPath = path; 
            if (mModDataProxy != null)
            {
                mModDataProxy.CustomData = [path.Guid];
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
                    LogVerbose($"ProcessCustom: CurrentMode is {CurrentMode}, routing to ProcessFollowWaypointsCustom.");
                    return ProcessFollowWaypointsCustom();
                default:
                    LogVerbose($"ProcessCustom: CurrentMode is {CurrentMode}, deferring.");
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
