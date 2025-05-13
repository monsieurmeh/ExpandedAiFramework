using UnityEngine;


namespace ExpandedAiFramework.AmbushWolfMod
{
    [RegisterTypeInIl2Cpp]
    public class AmbushWolf : BaseWolf
    {
        internal static AmbushWolfSettings Settings = new AmbushWolfSettings();
        protected HidingSpot mHidingSpot;

        public AmbushWolf(IntPtr ptr) : base(ptr) { }


        public override void Initialize(BaseAi ai, TimeOfDay timeOfDay)
        {
            base.Initialize(ai, timeOfDay);
            mHidingSpot = Manager.Instance.GetNearestHidingSpot(this, 3);
            //mBaseAi.transform.position = mHidingSpot.Position + new Vector3(0, 2, 0);
            //mBaseAi.m_MoveAgent.Warp(mHidingSpot.Position, 5.0f, true, -1);
            EnterHiding();
            LogVerbose("Initialize: Initialized, entering hiding.");
        }


        public override void Augment()
        {
            mBaseAi.m_DefaultMode = (AiMode)AiModeEAF.Hiding;
            mBaseAi.m_StartMode = (AiMode)AiModeEAF.Hiding;
            base.Augment();
        }


        protected override bool TestIsImposterCustom(out bool isImposter)
        {
            isImposter = false;
            return false;
        }


        protected override bool ProcessCustom()
        {
            switch (CurrentMode)
            {
                case (AiMode)AiModeEAF.Hiding:
                    LogVerbose($"ProcessCustom: CurrentMode is {CurrentMode}, routing to ProcessHiding.");
                    ProcessHiding();
                    return false;
                case (AiMode)AiModeEAF.Returning:
                    LogVerbose($"ProcessCustom: CurrentMode is {CurrentMode}, routing to ProcessReturning.");
                    ProcessReturning();
                    return false;
                default:
                    LogVerbose($"ProcessCustom: CurrentMode is {CurrentMode}, deferring.");
                    return true;
            }
        }


        protected override bool GetAiAnimationStateCustom(AiMode mode, out AiAnimationState overrideState)
        {
            switch (mode)
            {
                case (AiMode)AiModeEAF.Hiding:
                    LogVerbose($"GetAiAnimationStateCustom: mode is {mode}, setting overrideState to Paused.");
                    overrideState = AiAnimationState.Paused;
                    return false;
                case (AiMode)AiModeEAF.Returning:
                    LogVerbose($"GetAiAnimationStateCustom: mode is {mode}, setting overrideState to Wander.");
                    overrideState = AiAnimationState.Wander;
                    return false;
                default:
                    LogVerbose($"GetAiAnimationStateCustom: mode is {mode}, deffering");
                    overrideState = AiAnimationState.Invalid;
                    return true;
            }
        }


        protected override bool IsMoveStateCustom(AiMode mode, out bool isMoveState)
        {
            switch (mode)
            {
                case (AiMode)AiModeEAF.Hiding:
                    LogVerbose($"IsMoveStateCustom: mode is {mode}, setting isMoveState false.");
                    isMoveState = false;
                    return false;
                case (AiMode)AiModeEAF.Returning:
                    LogVerbose($"IsMoveStateCustom: mode is {mode}, setting isMoveState true.");
                    isMoveState = true;
                    return false;
                default:
                    LogVerbose($"IsMoveStateCustom: mode is {mode}, deferring.");
                    isMoveState = false;
                    return true;
            }
        }


        protected override bool EnterAiModeCustom(AiMode mode)
        {
            switch (mode)
            {
                case (AiMode)AiModeEAF.Hiding:
                    LogVerbose($"EnterAiModeCustom: mode is {mode}, routing to EnterHiding");
                    EnterHiding();
                    return false;
                case (AiMode)AiModeEAF.Returning:
                    LogVerbose($"EnterAiModeCustom: mode is {mode}, routing to EnterReturning.");
                    EnterReturning();
                    return false;
                default:
                    LogVerbose($"EnterAiModeCustom: mode is {mode}, deferring.");
                    return true;
            }
        }


        protected void ProcessHiding()
        {
            float hidingSpotDistance = Vector3.Distance(mBaseAi.transform.position, mHidingSpot.Position);
            if (hidingSpotDistance >= 2.0f)
            {
                LogVerbose($"ProcessHiding: Far from hiding spot ({hidingSpotDistance}), moving to returning.");
                SetAiMode((AiMode)AiModeEAF.Returning);
                return;
            }
            LogVerbose($"ProcessHiding: Scanning for target...");
            mBaseAi.ScanForNewTarget();
        }


        protected void EnterHiding()
        {
            mBaseAi.MoveAgentStop();
            mBaseAi.ClearTarget();
            mBaseAi.m_MoveAgent.PointTowardsDirection(mHidingSpot.Rotation);
        }


        protected void ProcessReturning()
        {
            mBaseAi.ScanForNewTarget();
            float hidingSpotDistance = Vector3.Distance(mBaseAi.transform.position, mHidingSpot.Position);
            if (hidingSpotDistance >= 2.0f) //todo: eliminate sqrt check, move to simple squared distance as a cached value from a "hiding spot distance" setting
            {
                LogVerbose($"ProcessReturning: To far from hiding spot ({hidingSpotDistance}, continuing.");
                return;
            }
            LogVerbose($"ProcessReturning: Close enough to hiding spot, hiding.");
            SetAiMode((AiMode)AiModeEAF.Hiding);
        }


        protected void EnterReturning()
        {
            mBaseAi.MoveAgentStop();
            mBaseAi.ClearTarget();
            mBaseAi.m_AiGoalSpeed = mBaseAi.m_RunSpeed;
            mBaseAi.StartPath(mHidingSpot.Position, mBaseAi.m_AiGoalSpeed);
        }


        //Vanilla logic moves predators to stalking if player target is detected; I want ambush wolves to RUN at you!
        protected override bool ChangeModeWhenTargetDetectedCustom()
        {
            if (!CurrentTarget.IsPlayer())
            {
                LogVerbose($"ChangeModeWhenTargetDetectedCustom: target is not player, deferring to base.");
                return true;
            }
            LogVerbose($"ChangeModeWhenTargetDetectedCustom: Attacking player target!");
            SetAiMode(AiMode.Attack);
            return false;
        }
    }
}
