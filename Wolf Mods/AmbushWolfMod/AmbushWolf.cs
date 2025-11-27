using UnityEngine;


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

        internal static AmbushWolfSettings AmbushWolfSettings;

        protected MapDataLoader<HidingSpot> mHidingSpotLoader;
        protected HidingSpot mHidingSpot;

        public AmbushWolf(IntPtr ptr) : base(ptr) { }
        public override Color DebugHighlightColor { get { return Color.yellow; } }


        public override void Initialize(BaseAi ai, TimeOfDay timeOfDay, SpawnRegion spawnRegion, SpawnModDataProxy
proxy)
        {
            base.Initialize(ai, timeOfDay, spawnRegion, proxy);
            this.LogTraceInstanced($"Initializing AmbushWolf at {spawnRegion.transform.position}", LogCategoryFlags.Ai);
            mHidingSpotLoader = new MapDataLoader<HidingSpot>(this, mModDataProxy, mManager.DataManager);
            mBaseAi.m_DefaultMode = (AiMode)AmbushWolfAiMode.Hide;
            mBaseAi.m_StartMode = (AiMode)AmbushWolfAiMode.Hide;
        }


        protected override bool FirstFrameCustom()
        {
            if (!mHidingSpotLoader.Connected()) return false; // Gate AI functionality here until wanderpath is ready

            mHidingSpot = mHidingSpotLoader.Data;
            MaybeWarpToFirstPoint();
            SetDefaultAiMode();
            return true;
        }


        private void MaybeWarpToFirstPoint()
        {
            if (!mHidingSpotLoader.New) return;

            this.LogTraceInstanced($"FirstFrameCustom: Warping to hidingspot start at {mHidingSpot.Position} and setting hiding mode", LogCategoryFlags.Ai);
            mBaseAi.m_MoveAgent.transform.position = mHidingSpot.Position;
            mBaseAi.m_MoveAgent.Warp(mHidingSpot.Position, 2.0f, true, -1);
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
                    this.LogTraceInstanced($"ProcessCustom: CurrentMode is {CurrentMode}, routing to ProcessHiding.", LogCategoryFlags.Ai | LogCategoryFlags.UpdateLoop);
                    ProcessHiding();
                    return false;
                case (AiMode)AmbushWolfAiMode.Return:
                    this.LogTraceInstanced($"ProcessCustom: CurrentMode is {CurrentMode}, routing to ProcessReturning.", LogCategoryFlags.Ai | LogCategoryFlags.UpdateLoop);
                    ProcessReturning();
                    return false;
                default:
                    this.LogTraceInstanced($"ProcessCustom: CurrentMode is {CurrentMode}, deferring.", LogCategoryFlags.Ai | LogCategoryFlags.UpdateLoop);
                    return base.ProcessCustom();
            }
        }


        protected override bool GetAiAnimationStateCustom(AiMode mode, out AiAnimationState overrideState)
        {
            switch (mode)
            {
                case (AiMode)AmbushWolfAiMode.Hide:
                    this.LogTraceInstanced($"GetAiAnimationStateCustom: mode is {mode}, setting overrideState to Paused.", LogCategoryFlags.Ai | LogCategoryFlags.UpdateLoop);
                    overrideState = AiAnimationState.Paused;
                    return false;
                case (AiMode)AmbushWolfAiMode.Return:
                    this.LogTraceInstanced($"GetAiAnimationStateCustom: mode is {mode}, setting overrideState to Wander.", LogCategoryFlags.Ai | LogCategoryFlags.UpdateLoop);
                    overrideState = AiAnimationState.Wander;
                    return false;
                default:
                    this.LogTraceInstanced($"GetAiAnimationStateCustom: mode is {mode}, deffering", LogCategoryFlags.Ai | LogCategoryFlags.UpdateLoop);
                    overrideState = AiAnimationState.Invalid;
                    return true;
            }
        }


        protected override bool IsMoveStateCustom(AiMode mode, out bool isMoveState)
        {
            switch (mode)
            {
                case (AiMode)AmbushWolfAiMode.Hide:
                    this.LogTraceInstanced($"IsMoveStateCustom: mode is {mode}, setting isMoveState false.", LogCategoryFlags.Ai | LogCategoryFlags.UpdateLoop);
                    isMoveState = false;
                    return false;
                case (AiMode)AmbushWolfAiMode.Return:
                    this.LogTraceInstanced($"IsMoveStateCustom: mode is {mode}, setting isMoveState true.", LogCategoryFlags.Ai | LogCategoryFlags.UpdateLoop);
                    isMoveState = true;
                    return false;
                default:
                    this.LogTraceInstanced($"IsMoveStateCustom: mode is {mode}, deferring.", LogCategoryFlags.Ai | LogCategoryFlags.UpdateLoop);
                    isMoveState = false;
                    return true;
            }
        }


        protected override bool EnterAiModeCustom(AiMode mode)
        {
            switch (mode)
            {
                case (AiMode)AmbushWolfAiMode.Hide:
                    this.LogTraceInstanced($"EnterAiModeCustom: mode is {mode}, routing to EnterHiding", LogCategoryFlags.Ai | LogCategoryFlags.UpdateLoop);
                    EnterHiding();
                    return false;
                case (AiMode)AmbushWolfAiMode.Return:
                    this.LogTraceInstanced($"EnterAiModeCustom: mode is {mode}, routing to EnterReturning.", LogCategoryFlags.Ai | LogCategoryFlags.UpdateLoop);
                    EnterReturning();
                    return false;
                default:
                    this.LogTraceInstanced($"EnterAiModeCustom: mode is {mode}, deferring.", LogCategoryFlags.Ai | LogCategoryFlags.UpdateLoop);
                    return true;
            }
        }


        protected void ProcessHiding()
        {
            if (mHidingSpot == null)
            {
                //Dont do anythign yet till you have a hiding spot
                //this.LogErrorInstanced("ProcessHiding called but no hiding spot assigned!");
                return;
            }

            float hidingSpotDistance = Vector3.Distance(mBaseAi.transform.position, mHidingSpot.Position);

            if (hidingSpotDistance >= 2.0f)
            {
                SetAiMode((AiMode)AmbushWolfAiMode.Return);
                return;
            }

            mBaseAi.ScanForNewTarget();
        }


        protected void EnterHiding()
        {
            if (mHidingSpot == null)
            {
                this.LogErrorInstanced("EnterHiding called but no hiding spot assigned!");
                return;
            }
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
                this.LogTraceInstanced($"ProcessReturning: To far from hiding spot ({hidingSpotDistance}, continuing.", LogCategoryFlags.Ai | LogCategoryFlags.UpdateLoop);
                return;
            }
            this.LogTraceInstanced($"ProcessReturning: Close enough to hiding spot, hiding.", LogCategoryFlags.Ai | LogCategoryFlags.UpdateLoop);
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
                this.LogTraceInstanced($"Ambush wolves run from larger threats!", LogCategoryFlags.Ai);
                SetAiMode(AiMode.Flee);
                return false;
            }
            if (!CurrentTarget.IsPlayer())
            {
                this.LogTraceInstanced($"ChangeModeWhenTargetDetectedCustom: target is not player, IGNORING. Ambush wolves want YOU!", LogCategoryFlags.Ai);
                return false;
            }
            this.LogTraceInstanced($"ChangeModeWhenTargetDetectedCustom: Attacking player target!", LogCategoryFlags.Ai);
            SetAiMode(AiMode.Attack);
            return false;
        }
        */
    }
}
