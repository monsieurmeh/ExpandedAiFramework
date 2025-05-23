using UnityEngine;
using UnityEngine.AI;


namespace ExpandedAiFramework.AmbushWolfMod
{
    [RegisterTypeInIl2Cpp]
    public class AmbushWolf : BaseWolf
    {
        private enum AmbushWolfAiMode : int
        {
            Hide = (int)AiMode.Disabled + 1,
            Return,
            COUNT
        }

        internal static AmbushWolfSettings AmbushWolfSettings = new AmbushWolfSettings();
        protected HidingSpot mHidingSpot;

        public AmbushWolf(IntPtr ptr) : base(ptr) { }


        public override void Initialize(BaseAi ai, TimeOfDay timeOfDay, SpawnRegion spawnRegion, SpawnModDataProxy proxy)//, EAFManager manager)
        {
            base.Initialize(ai, timeOfDay, spawnRegion, proxy);//, manager); 
            mHidingSpot = mManager.GetNearestHidingSpot(this, 3, false); 
            mBaseAi.m_DefaultMode = (AiMode)AmbushWolfAiMode.Hide;
            mBaseAi.m_StartMode = (AiMode)AmbushWolfAiMode.Hide;
        }


        protected override bool FirstFrameCustom()
        {
            mBaseAi.m_MoveAgent.transform.position = mHidingSpot.Position;
            mBaseAi.m_MoveAgent.Warp(mHidingSpot.Position, 2.0f, true, -1);
            SetAiMode((AiMode)AmbushWolfAiMode.Hide);
            return true;
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
                case (AiMode)AmbushWolfAiMode.Hide:
                    LogVerbose($"ProcessCustom: CurrentMode is {CurrentMode}, routing to ProcessHiding.");
                    ProcessHiding();
                    return false;
                case (AiMode)AmbushWolfAiMode.Return:
                    LogVerbose($"ProcessCustom: CurrentMode is {CurrentMode}, routing to ProcessReturning.");
                    ProcessReturning();
                    return false;
                default:
                    LogVerbose($"ProcessCustom: CurrentMode is {CurrentMode}, deferring.");
                    return base.ProcessCustom();
            }
        }


        protected override bool GetAiAnimationStateCustom(AiMode mode, out AiAnimationState overrideState)
        {
            switch (mode)
            {
                case (AiMode)AmbushWolfAiMode.Hide:
                    LogVerbose($"GetAiAnimationStateCustom: mode is {mode}, setting overrideState to Paused.");
                    overrideState = AiAnimationState.Paused;
                    return false;
                case (AiMode)AmbushWolfAiMode.Return:
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
                case (AiMode)AmbushWolfAiMode.Hide:
                    LogVerbose($"IsMoveStateCustom: mode is {mode}, setting isMoveState false.");
                    isMoveState = false;
                    return false;
                case (AiMode)AmbushWolfAiMode.Return:
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
                case (AiMode)AmbushWolfAiMode.Hide:
                    LogVerbose($"EnterAiModeCustom: mode is {mode}, routing to EnterHiding");
                    EnterHiding();
                    return false;
                case (AiMode)AmbushWolfAiMode.Return:
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
                SetAiMode((AiMode)AmbushWolfAiMode.Return);
                return;
            }
            LogVerbose($"ProcessHiding: Scanning for target...");
            mBaseAi.ScanForNewTarget();
        }


        protected void EnterHiding()
        {
            mBaseAi.MoveAgentStop();
            mBaseAi.ClearTarget();
            mBaseAi.m_MoveAgent.transform.rotation = mHidingSpot.Rotation;
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
            SetAiMode((AiMode)AmbushWolfAiMode.Hide);
        }


        protected void EnterReturning()
        {
            mBaseAi.MoveAgentStop();
            mBaseAi.ClearTarget();
            mBaseAi.m_AiGoalSpeed = mBaseAi.m_RunSpeed;
            mBaseAi.StartPath(mHidingSpot.Position, mBaseAi.m_AiGoalSpeed);
        }

        /* Reimplement when it actually gets called by CustomBaseAi
        //Vanilla logic moves predators to stalking if player target is detected; I want ambush wolves to RUN at you!
        protected override bool ChangeModeWhenTargetDetectedCustom()
        {
            if (CurrentTarget.IsBear() || CurrentTarget.IsCougar() || CurrentTarget.IsMoose())
            {
                LogVerbose($"Ambush wolves run from larger threats!");
                SetAiMode(AiMode.Flee);
                return false;
            }
            if (!CurrentTarget.IsPlayer())
            {
                LogVerbose($"ChangeModeWhenTargetDetectedCustom: target is not player, IGNORING. Ambush wolves want YOU!");
                return false;
            }
            LogVerbose($"ChangeModeWhenTargetDetectedCustom: Attacking player target!");
            SetAiMode(AiMode.Attack);
            return false;
        }
        */
    }
}
