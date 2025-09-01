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

        internal static AmbushWolfSettings AmbushWolfSettings;
        protected HidingSpot mHidingSpot;
        protected bool mFetchingHidingSpot;

        public AmbushWolf(IntPtr ptr) : base(ptr) { }
        public override Color DebugHighlightColor { get { return Color.yellow; } }


        public override void Initialize(BaseAi ai, TimeOfDay timeOfDay, SpawnRegion spawnRegion, SpawnModDataProxy
proxy)
        {
            base.Initialize(ai, timeOfDay, spawnRegion, proxy);
            this.LogTraceInstanced($"Initializing AmbushWolf at {spawnRegion.transform.position}");
            mBaseAi.m_DefaultMode = (AiMode)AmbushWolfAiMode.Hide;
            mBaseAi.m_StartMode = (AiMode)AmbushWolfAiMode.Hide;
        }


        protected override bool FirstFrameCustom()
        {
            if (mModDataProxy != null && mModDataProxy.AsyncProcessing)
            {
                return false;
            }

            if (mHidingSpot == null)
            {
                if (!mFetchingHidingSpot)
                {
                    GetHidingSpot(mModDataProxy);
                }
                return false;
            }

            this.LogTraceInstanced($"FirstFrameCustom: Warping to hiding spot at {mHidingSpot.Position} and setting hide mode");
            mBaseAi.m_MoveAgent.transform.position = mHidingSpot.Position;
            mBaseAi.m_MoveAgent.Warp(mHidingSpot.Position, 2.0f, true, -1);

            SetAiMode((AiMode)AmbushWolfAiMode.Hide);
            return true;
        }


        private void GetHidingSpot(SpawnModDataProxy proxy)
        {
            if (TryGetSavedHidingSpot(proxy))
            {
                return;
            }
            mManager.DataManager.ScheduleMapDataRequest<HidingSpot>(new GetNearestMapDataRequest<HidingSpot>(mBaseAi.transform.position, proxy.Scene, (nearestSpot, result2) =>
            {
                this.LogTraceInstanced($"Found NEW nearest hiding spot with guid <<<{nearestSpot}>>>");
                AttachHidingSpot(nearestSpot);
            }, false, 3));
            
        }


        private bool TryGetSavedHidingSpot(SpawnModDataProxy proxy)
        {
            mFetchingHidingSpot = true;
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
            mManager.DataManager.ScheduleMapDataRequest<HidingSpot>(new GetDataByGuidRequest<HidingSpot>(spotGuid, proxy.Scene, (spot, result) =>
            {
                if (result != RequestResult.Succeeded)
                {
                    this.LogTraceInstanced($"Can't get hiding spot with guid <<<{spotGuid}>>> from dictionary, requesting nearest instead...");
                    mManager.DataManager.ScheduleMapDataRequest<HidingSpot>(new GetNearestMapDataRequest<HidingSpot>(mBaseAi.transform.position, proxy.Scene, (nearestSpot, result2) =>
                    {
                        this.LogTraceInstanced($"Found NEW nearest hiding spot with guid <<<{nearestSpot}>>>");
                        AttachHidingSpot(nearestSpot);
                    }, false, 3));
                }
                else
                {
                    this.LogTraceInstanced($"Found saved hiding spot with guid <<<{spotGuid}>>>");
                    AttachHidingSpot(spot);
                }
            }, false));
            return true;
        }


        public void AttachHidingSpot(HidingSpot hidingSpot)
        {

            mFetchingHidingSpot = false;
            if (hidingSpot == null)
            {
                this.LogWarningInstanced("Received NULL hiding spot, aborting!");
                return;
            }
            this.LogTraceInstanced($"Attached hiding spot: {hidingSpot}");
            mHidingSpot = hidingSpot;

            if (mModDataProxy != null)
            {
                mModDataProxy.CustomData = [mHidingSpot.Guid.ToString()];
                this.LogTraceInstanced($"Saved spot {mHidingSpot} to proxy data");
            }
            hidingSpot.Claim();
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
                    this.LogVerboseInstanced($"ProcessCustom: CurrentMode is {CurrentMode}, routing to ProcessHiding.");
                    ProcessHiding();
                    return false;
                case (AiMode)AmbushWolfAiMode.Return:
                    this.LogVerboseInstanced($"ProcessCustom: CurrentMode is {CurrentMode}, routing to ProcessReturning.");
                    ProcessReturning();
                    return false;
                default:
                    this.LogVerboseInstanced($"ProcessCustom: CurrentMode is {CurrentMode}, deferring.");
                    return base.ProcessCustom();
            }
        }


        protected override bool GetAiAnimationStateCustom(AiMode mode, out AiAnimationState overrideState)
        {
            switch (mode)
            {
                case (AiMode)AmbushWolfAiMode.Hide:
                    this.LogVerboseInstanced($"GetAiAnimationStateCustom: mode is {mode}, setting overrideState to Paused.");
                    overrideState = AiAnimationState.Paused;
                    return false;
                case (AiMode)AmbushWolfAiMode.Return:
                    this.LogVerboseInstanced($"GetAiAnimationStateCustom: mode is {mode}, setting overrideState to Wander.");
                    overrideState = AiAnimationState.Wander;
                    return false;
                default:
                    this.LogVerboseInstanced($"GetAiAnimationStateCustom: mode is {mode}, deffering");
                    overrideState = AiAnimationState.Invalid;
                    return true;
            }
        }


        protected override bool IsMoveStateCustom(AiMode mode, out bool isMoveState)
        {
            switch (mode)
            {
                case (AiMode)AmbushWolfAiMode.Hide:
                    this.LogVerboseInstanced($"IsMoveStateCustom: mode is {mode}, setting isMoveState false.");
                    isMoveState = false;
                    return false;
                case (AiMode)AmbushWolfAiMode.Return:
                    this.LogVerboseInstanced($"IsMoveStateCustom: mode is {mode}, setting isMoveState true.");
                    isMoveState = true;
                    return false;
                default:
                    this.LogVerboseInstanced($"IsMoveStateCustom: mode is {mode}, deferring.");
                    isMoveState = false;
                    return true;
            }
        }


        protected override bool EnterAiModeCustom(AiMode mode)
        {
            switch (mode)
            {
                case (AiMode)AmbushWolfAiMode.Hide:
                    this.LogVerboseInstanced($"EnterAiModeCustom: mode is {mode}, routing to EnterHiding");
                    EnterHiding();
                    return false;
                case (AiMode)AmbushWolfAiMode.Return:
                    this.LogVerboseInstanced($"EnterAiModeCustom: mode is {mode}, routing to EnterReturning.");
                    EnterReturning();
                    return false;
                default:
                    this.LogVerboseInstanced($"EnterAiModeCustom: mode is {mode}, deferring.");
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
                this.LogVerboseInstanced($"ProcessReturning: To far from hiding spot ({hidingSpotDistance}, continuing.");
                return;
            }
            this.LogVerboseInstanced($"ProcessReturning: Close enough to hiding spot, hiding.");
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
                this.LogVerboseInstanced($"Ambush wolves run from larger threats!");
                SetAiMode(AiMode.Flee);
                return false;
            }
            if (!CurrentTarget.IsPlayer())
            {
                this.LogVerboseInstanced($"ChangeModeWhenTargetDetectedCustom: target is not player, IGNORING. Ambush wolves want YOU!");
                return false;
            }
            this.LogVerboseInstanced($"ChangeModeWhenTargetDetectedCustom: Attacking player target!");
            SetAiMode(AiMode.Attack);
            return false;
        }
        */
    }
}
