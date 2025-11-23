using Il2Cpp;
using UnityEngine;


namespace ExpandedAiFramework.WanderingWolfMod
{
    [RegisterTypeInIl2Cpp]
    public class WanderingWolf : BaseWolf
    {
        internal static WanderingWolfSettings WanderingWolfSettings;

        protected WanderPath mWanderPath;
        protected WanderPathLoader mWanderPathLoader;


        public WanderingWolf(IntPtr ptr) : base(ptr) { }
        protected override float m_MinWaypointDistance { get { return 100.0f; } }
        protected override float m_MaxWaypointDistance { get { return 1000.0f; } }
        public override Color DebugHighlightColor { get { return Color.green; } }


        public override void Initialize(BaseAi ai, TimeOfDay timeOfDay, SpawnRegion spawnRegion, SpawnModDataProxy proxy)//, EAFManager manager)
        {
            base.Initialize(ai, timeOfDay, spawnRegion, proxy);//, manager);
            mWanderPathLoader = new WanderPathLoader(this, proxy, mManager.DataManager);

            mBaseAi.m_DefaultMode = AiMode.FollowWaypoints;
            mBaseAi.m_StartMode = AiMode.FollowWaypoints;
            mBaseAi.m_CurrentMode = AiMode.FollowWaypoints;
            mBaseAi.m_WaypointCompletionBehaviour = BaseAi.WaypointCompletionBehaviouir.Restart;
            mBaseAi.m_TargetWaypointIndex = 0;
        }

        protected override bool FirstFrameCustom()
        {
            if (mModDataProxy != null && mModDataProxy.AsyncProcessing) return false;
            if (!mWanderPathLoader.CheckWanderPathReady()) return false; // Gate AI functionality here until wanderpath is ready

            mWanderPath = mWanderPathLoader.Path;
            MaybeWarpToFirstPoint();
            SetDefaultAiMode();
            return true;
        }


        private void MaybeWarpToFirstPoint()
        {
            if (!mWanderPathLoader.NewPath) return;

            this.LogTraceInstanced($"FirstFrameCustom: Warping to wanderpath start at {mWanderPath.PathPoints[mBaseAi.m_TargetWaypointIndex]} and setting wander mode", LogCategoryFlags.Ai);
            mBaseAi.m_MoveAgent.transform.position = mWanderPath.PathPoints[mBaseAi.m_TargetWaypointIndex];
            mBaseAi.m_MoveAgent.Warp(mWanderPath.PathPoints[mBaseAi.m_TargetWaypointIndex], 2.0f, true, -1);
        }


        protected override bool ProcessCustom()
        {
            switch (CurrentMode)
            {
                case AiMode.FollowWaypoints:
                    return mWanderPathLoader.Loaded;
                default:
                    return base.ProcessCustom();
            }
        }


        protected override bool TestIsImposterCustom(out bool isImposter)
        {
            isImposter = false;
            return false;
        }
    }
}
