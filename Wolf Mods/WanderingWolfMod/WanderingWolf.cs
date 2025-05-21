using UnityEngine;


namespace ExpandedAiFramework.WanderingWolfMod
{
    [RegisterTypeInIl2Cpp]
    public class WanderingWolf : BaseWolf
    {
        internal static WanderingWolfSettings WanderingWolfSettings = new WanderingWolfSettings();

        protected WanderPath mWanderPath;
            
        public WanderingWolf(IntPtr ptr) : base(ptr) { }
        protected override float m_MinWaypointDistance { get { return 100.0f; } }
        protected override float m_MaxWaypointDistance { get { return 1000.0f; } }
        

        public override void Initialize(BaseAi ai, TimeOfDay timeOfDay, SpawnRegion spawnRegion)//, EAFManager manager)
        {
            base.Initialize(ai, timeOfDay, spawnRegion);//, manager);
            mWanderPath = mManager.GetNearestWanderPath(this, 3, false); 
            mBaseAi.m_Waypoints = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>(mWanderPath.PathPoints.Length);
            for (int i = 0, iMax = mBaseAi.m_Waypoints.Length; i < iMax; i++)
            {
                mBaseAi.m_Waypoints[i] = mWanderPath.PathPoints[i];
            }
            mBaseAi.m_DefaultMode = AiMode.FollowWaypoints;
            mBaseAi.m_StartMode = AiMode.FollowWaypoints;
            mBaseAi.m_CurrentMode = AiMode.FollowWaypoints;
            mBaseAi.m_WaypointCompletionBehaviour = BaseAi.WaypointCompletionBehaviouir.Restart;
            mBaseAi.m_TargetWaypointIndex = 0;
        }


        protected override bool ProcessCustom()
        {
            switch (CurrentMode)
            {
                case AiMode.Wander:
                    LogVerbose($"ProcessCustom: CurrentMode is {CurrentMode}, routing to ProcessWanderCustom.");
                    ProcessWanderCustom();
                    return false;
                default:
                    LogVerbose($"ProcessCustom: CurrentMode is {CurrentMode}, deferring.");
                    return base.ProcessCustom();
            }
        }


        protected override bool FirstFrameCustom()
        {
            mBaseAi.m_MoveAgent.transform.position = mWanderPath;
            mBaseAi.m_MoveAgent.Warp(mWanderPath, 2.0f, true, -1);
            SetDefaultAiMode();
            return true;
        }


        protected override bool TestIsImposterCustom(out bool isImposter)
        {
            isImposter = false;
            return false;
        }

        /* Reimplement when it actually gets called by CustomBaseAi
        //Vanilla logic moves predators to stalking if player target is detected; I want ambush wolves to RUN at you!
        protected override bool ChangeModeWhenTargetDetectedCustom()
        {
            if (CurrentTarget.IsBear() || CurrentTarget.IsCougar() || CurrentTarget.IsBear())
            {
                LogVerbose($"Wandering wolves run from larger threats!");
                SetAiMode(AiMode.Flee);
                return false;
            }
            return true;
        }
        */


        protected void ProcessWanderCustom()
        {
            bool hasNewWanderPos = false;
            Vector3 wanderPos = Vector3.zero;
            mBaseAi.ClearTarget();
            MaybeImposter();
            if (mBaseAi.IsImposter())
            {
                mBaseAi.m_AiGoalSpeed = 0.0f;
                return;
            }
            mBaseAi.m_AiGoalSpeed = mBaseAi.m_WalkSpeed;
            if (mBaseAi.m_TimeInModeSeconds > 1.0f)
            {
                ScanForNewTarget();
            }
            if ((mBaseAi.m_WanderingAroundPos == false) &&
               (mBaseAi.m_PickedWanderDestination != false))
            {
                mBaseAi.ScanForSmells();
            }
            if (mBaseAi.m_NextCheckMovedDistanceTime < Time.time)
            {
                if (Utility.SquaredDistance(mBaseAi.m_CachedTransform.position, mBaseAi.m_PositionAtLastMoveCheck) < 0.04f)
                {
                    mBaseAi.m_PickedWanderDestination = false;
                }
                mBaseAi.m_NextCheckMovedDistanceTime = Time.time + 1.0f;
                mBaseAi.m_PositionAtLastMoveCheck = mBaseAi.m_CachedTransform.position;
            }
            if (mBaseAi.m_PickedWanderDestination == false)
            {
                /*
                if (mBaseAi.Moose != null && !mBaseAi.m_UseWanderAwayFromPos)
                {
                    hasNewWanderPos = mBaseAi.Moose?.MaybeSelectScratchingStump(out wanderPos) ?? false;
                    if (hasNewWanderPos)
                    {
                        mBaseAi.m_CurrentWanderPos = wanderPos;
                    }
                }
                */
                if (!mBaseAi.m_UseWanderAwayFromPos)
                {
                    if (mBaseAi.m_UseWanderToPos)
                    {
                        mBaseAi.m_CurrentWanderPos = mBaseAi.transform.position;
                        hasNewWanderPos = AiUtils.GetClosestNavmeshPos(out wanderPos, mBaseAi.m_WanderToPos, mBaseAi.m_CachedTransform.position);
                        if (hasNewWanderPos)
                        {
                            mBaseAi.m_CurrentWanderPos = wanderPos;
                        }
                        mBaseAi.m_UseWanderToPos = false;
                    }
                }
                else
                {
                    hasNewWanderPos = mBaseAi.PickWanderDestinationAwayFromPoint(out wanderPos, mBaseAi.m_WanderAwayFromPos);
                    if (hasNewWanderPos)
                    {
                        mBaseAi.m_CurrentWanderPos = wanderPos;
                    }
                    mBaseAi.m_UseWanderAwayFromPos = false;
                }
                if ((hasNewWanderPos) || mBaseAi.PickWanderDestination(out wanderPos))
                {
                    if (!hasNewWanderPos)
                    {
                        hasNewWanderPos = true;
                        mBaseAi.m_CurrentWanderPos = wanderPos;
                    }
                    if (mBaseAi.m_WildlifeMode == WildlifeMode.Aurora)
                    {
                        hasNewWanderPos = mBaseAi.MaybeMoveWanderPosOutsideOfField(out wanderPos, mBaseAi.m_CurrentWanderPos);

                    }
                }
                if (!hasNewWanderPos)
                {
                    mBaseAi.m_CurrentWanderPos = mBaseAi.transform.position;
                    hasNewWanderPos = AiUtils.GetClosestNavmeshPos(out wanderPos, mBaseAi.m_CachedTransform.position, mBaseAi.m_CachedTransform.position);
                    if (!hasNewWanderPos)
                    {
                        mBaseAi.MoveAgentStop();
                        SetDefaultAiMode();
                        return;
                    }
                    mBaseAi.m_CurrentWanderPos = wanderPos;
                }
                if (!mBaseAi.m_WanderUseTurnRadius)
                {
                    hasNewWanderPos = mBaseAi.StartPath(mBaseAi.m_CurrentWanderPos, mBaseAi.m_WalkSpeed);
                }
                else
                {
                    mBaseAi.m_WanderTurnTargets = AiUtils.GetPointsForGradualTurn(mBaseAi.transform, mBaseAi.m_CurrentWanderPos, mBaseAi.m_WanderTurnRadius, mBaseAi.m_WanderTurnSegmentAngle);
                    mBaseAi.m_WanderCurrentTarget = 0;
                    if (mBaseAi.m_WanderTurnTargets.Count == 0)
                    {
                        return;
                    }
                    hasNewWanderPos = mBaseAi.StartPath(mBaseAi.m_WanderTurnTargets[0], mBaseAi.m_WalkSpeed);
                }

                if (!hasNewWanderPos)
                {
                    SetDefaultAiMode();
                    return;
                }

                mBaseAi.m_PickedWanderDestination = true;
            }

            if (mBaseAi.m_MoveAgent.m_DestinationReached)
            {
                bool pathStarted = false;
                if (mBaseAi.m_WanderUseTurnRadius)
                {
                    mBaseAi.m_WanderCurrentTarget += 1;
                    if (mBaseAi.m_WanderCurrentTarget < BaseAi.m_WanderTurnTargets.Length)
                    {
                        mBaseAi.StartPath(mBaseAi.m_WanderTurnTargets[mBaseAi.m_WanderCurrentTarget], mBaseAi.m_WalkSpeed);
                        pathStarted = true;
                    }
                }
                if (!pathStarted)
                {
                    mBaseAi.m_PickedWanderDestination = false;
                }
            }

            if (mBaseAi.m_WanderDurationHours > 0.0001f && mBaseAi.m_WanderDurationHours < mBaseAi.m_ElapsedWanderHours)
            {
                mBaseAi.m_ElapsedWanderHours = 0.0f;
                mBaseAi.m_WanderDurationHours = 0.0f;
                mBaseAi.m_WanderingAroundPos = false;
                SetDefaultAiMode();
                return;
            }

            mBaseAi.MaybeHoldGroundAuroraField();
            mBaseAi.MaybeEnterWanderPause();
            //yknow... occurs to me I maybe could have tried using that magic "MaybeForceStalkPlayer" command for the tracking wolf... 
            /*
            if (mBaseAi.Bear?.ShouldAlwaysStalkPlayer() ?? false)
            {
                mBaseAi.MaybeForceStalkPlayer();
            }
            */
            UniStormWeatherSystem uniStormWeatherSystem = mTimeOfDay.m_WeatherSystem;
            mBaseAi.m_ElapsedWanderHours += (24.0f / (uniStormWeatherSystem.m_DayLengthScale * uniStormWeatherSystem.m_DayLength)) * Time.deltaTime;
        }
    }
}
