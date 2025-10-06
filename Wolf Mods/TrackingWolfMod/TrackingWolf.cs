using UnityEngine;


namespace ExpandedAiFramework.TrackingWolfMod
{
    [RegisterTypeInIl2Cpp]
    public class TrackingWolf : BaseWolf
    {
        internal static TrackingWolfSettings TrackingWolfSettings;

        protected float m_TimeSinceLastSmellCheck = 0.0f;
        protected float m_TimeSinceLastStruggle = 0.0f;

        public TrackingWolf(IntPtr ptr) : base(ptr) { }
        public override Color DebugHighlightColor { get { return Color.red; } }

        protected override bool ProcessCustom()
        {
            switch (CurrentMode)
            {
                case AiMode.InvestigateSmell:
                    this.LogTraceInstanced($"ProcessCustom: CurrentMode is {CurrentMode}, routing to ProcessInvestigateSmellCustom.", LogCategoryFlags.Ai);
                    ProcessInvestigateSmellCustom();
                    return false;
                default:
                    this.LogTraceInstanced($"ProcessCustom: CurrentMode is {CurrentMode}, deferring.", LogCategoryFlags.Ai);
                    return base.ProcessCustom();
            }
        }


        protected override bool EnterAiModeCustom(AiMode mode)
        {
            if (mode == AiMode.Struggle)
            {
                mManager.LastPlayerStruggleTime = Time.time;
            }
            return true;
        }


        protected override bool TestIsImposterCustom(out bool isImposter)
        {
            isImposter = false;
            return false;
        }


        protected void ProcessInvestigateSmellCustom()
        {
            if (!mBaseAi.CanPlayerBeReached(GameManager.m_PlayerManager.m_LastPlayerPosition))
            {
                this.LogTraceInstanced($"ProcessInvestigateSmellCustom: Cant reach player, swapping back to default mode...", LogCategoryFlags.Ai);
                SetDefaultAiMode();
                return;
            }
            if (mBaseAi.m_SmellTarget == null || !mBaseAi.m_SmellTarget.IsPlayer())
            {
                mBaseAi.m_SmellTarget = GameManager.m_PlayerManager.m_AiTarget;
            }
            if (mBaseAi.m_SmellTarget == null)
            {
                this.LogTraceInstanced($"ProcessInvestigateSmellCustom: smell target is null, setting default ai mode.", LogCategoryFlags.Ai);
                return;
            }
            float dist;
            if (!mBaseAi.m_HasInvestigateSmellPath)
            {
                if (!AiUtils.GetClosestNavmeshPos(out Vector3 navMeshPos, mBaseAi.m_SmellTarget.transform.position, mBaseAi.m_SmellTarget.transform.position))
                {
                    this.LogTraceInstanced($"ProcessInvestigateSmellCustom: Unable to get closest navmesh point, setting default ai mode.", LogCategoryFlags.Ai);
                    SetDefaultAiMode();
                    return;
                }
                mBaseAi.m_PathingToSmellTargetPos = navMeshPos;
                dist = Vector3.Distance(mBaseAi.m_CachedTransform.position, mBaseAi.m_PathingToSmellTargetPos);
                if (dist < mBaseAi.m_MinSmellDistance) //todo: cache squared minSmellDist and eliminate sqrt check
                {
                    this.LogTraceInstanced($"ProcessInvestigateSmellCustom: Distance from pos ({mBaseAi.m_CachedTransform.position}) to target ({mBaseAi.m_CachedTransform.position}) [{dist}] is less than minSmellDistance ({mBaseAi.m_MinSmellDistance}), trying attack or returning", LogCategoryFlags.Ai);
                    if (mBaseAi.CanSeeTarget())
                    {
                        this.LogTraceInstanced($"ProcessInvestigateSmellCustom: Can see target, attacking!", LogCategoryFlags.Ai);
                        SetAiMode(AiMode.Attack);
                    }
                    return;
                }
                mBaseAi.StartPath(mBaseAi.m_PathingToSmellTargetPos, mBaseAi.m_StalkSpeed);
                mBaseAi.m_HasInvestigateSmellPath = true;
            }
            dist = Vector3.Distance(mBaseAi.m_CachedTransform.position, mBaseAi.m_PathingToSmellTargetPos);
            if (dist < mBaseAi.m_MinSmellDistance)
            {
                this.LogTraceInstanced($"ProcessInvestigateSmellCustom: Distance from pos ({mBaseAi.m_CachedTransform.position}) to target ({mBaseAi.m_CachedTransform.position}) [{dist}] is less than minSmellDistance ({mBaseAi.m_MinSmellDistance}), stopping move agent and resetting calcs", LogCategoryFlags.Ai);
                mBaseAi.m_HasInvestigateSmellPath = false;
                mBaseAi.MoveAgentStop();
            }
            ScanForNewTarget();
            if (mBaseAi.CanSeeTarget() && mBaseAi.m_CurrentTarget.IsPlayer())
            {
                this.LogTraceInstanced($"ProcessInvestigateSmellCustom: Can see PLAYER target, attacking!", LogCategoryFlags.Ai);
                SetAiMode(AiMode.Attack);
            }
        }


        protected override bool PostProcessCustom()
        {
            if (Time.time - mManager.LastPlayerStruggleTime <= TrackingWolfSettings.PostStruggleFleePeriodSeconds && CurrentMode != AiMode.Flee)
            {
                SetAiMode(AiMode.Flee);
                return true;
            }
            if (m_TimeSinceLastSmellCheck <= 2.0f)
            {
                m_TimeSinceLastSmellCheck += Time.deltaTime;
                return true;
            }
            else
            {
                m_TimeSinceLastSmellCheck = 0;
            }
            if (CurrentMode.ToFlag().NoneOf(AiModeFlags.TypicalDontInterrupt))
            {
                if (mBaseAi.CanSeeTarget() && mBaseAi.m_CurrentTarget.IsPlayer())
                {
                    this.LogTraceInstanced("PostProcessCustom: Player spotted, entering stalking state!");
                    SetAiMode(AiMode.Stalking);
                    return true;
                }
                if (mBaseAi.CanPathfindToPosition(GameManager.m_PlayerManager.m_LastPlayerPosition))
                {
                    this.LogTraceInstanced($"PostProcessCustom: Smell target reachable, moving to investigate.", LogCategoryFlags.Ai);
                    mBaseAi.m_SmellTarget = GameManager.m_PlayerManager.m_AiTarget;
                    mBaseAi.m_AiTarget = mBaseAi.m_SmellTarget;
                    SetAiMode(AiMode.InvestigateSmell);
                }
                else if (CurrentMode != AiMode.InvestigateSmell)
                {
                    this.LogTraceInstanced($"PostProcessCustom: Cant reach smell target, moving to wander.", LogCategoryFlags.Ai);
                    SetAiMode(AiMode.Wander);
                }
            }
            return true;
        }

        /* Reimplement when it's actually called by CustomBaseAi
        //Vanilla logic moves predators to stalking if player target is detected; I want tracking wolves to RUN at you!
        protected override bool ChangeModeWhenTargetDetectedCustom()
        {
            if (CurrentTarget.IsBear() || CurrentTarget.IsCougar() || CurrentTarget.IsMoose())
            {
                this.LogTraceInstanced($"Tracking wolves run from larger threats!", LogCategoryFlags.Ai);
                SetAiMode(AiMode.Flee);
                return false;
            }
            return true;
        }
        /=*/
    }
}
