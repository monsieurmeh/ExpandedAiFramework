using UnityEngine;


namespace ExpandedAiFramework.WanderingWolfMod
{
    [RegisterTypeInIl2Cpp]
    public class WanderingWolf : BaseWolf
    {
        internal static WanderingWolfSettings Settings = new WanderingWolfSettings();

        protected WanderPath mWanderPath;

        public WanderingWolf(IntPtr ptr) : base(ptr) { }
        protected override float m_MinWaypointDistance { get { return 100.0f; } }
        protected override float m_MaxWaypointDistance { get { return 1000.0f; } }

        public override void Augment()
        {
            mWanderPath = Manager.Instance.GetNearestWanderPath(this);
            mBaseAi.m_Waypoints = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>(mWanderPath.PathPoints.Length);
            for (int i = 0, iMax = mBaseAi.m_Waypoints.Length; i < iMax; i++)
            {
                mBaseAi.m_Waypoints[i] = mWanderPath.PathPoints[i];
            }
            mBaseAi.m_DefaultMode = AiMode.FollowWaypoints;
            mBaseAi.m_StartMode = AiMode.FollowWaypoints;
            mBaseAi.m_CurrentMode = AiMode.FollowWaypoints;
            mBaseAi.m_WaypointCompletionBehaviour = BaseAi.WaypointCompletionBehaviouir.Restart;
            mBaseAi.m_MoveAgent.Warp(mWanderPath.PathPoints[0], 5.0f, true, -1);
            mBaseAi.m_TargetWaypointIndex = 0;
            base.Augment();
        }


        protected override bool TestIsImposterCustom(out bool isImposter)
        {
            isImposter = false;
            return false;
        }
    }
}
