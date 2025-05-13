using UnityEngine;


namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public partial class CustomAiBase : MonoBehaviour, ICustomAi 
    {
        public CustomAiBase(IntPtr intPtr) : base(intPtr) { }
        
        public virtual void Initialize(BaseAi ai, TimeOfDay timeOfDay)
        {
            mBaseAi = ai;
            mTimeOfDay = timeOfDay;
        }

        protected BaseAi mBaseAi;
        protected TimeOfDay mTimeOfDay;
        protected float mTimeSinceCheckForTargetInPatrolWaypointsMode = 0.0f;



        public BaseAi BaseAi { get { return mBaseAi; } }
        public Component Self { get { return this; } }


        public virtual void Augment() 
        {
            OnAugmentDebug();
        }


        public virtual void UnAugment() { }


        public virtual void Update()
        {
            OnUpdateDebug();
            if (mBaseAi == null)
            {
                return;
            }
            if (!UpdateCustom())
            {
                return;
            }
            if (GameManager.m_IsPaused)
            {
                return;
            }
            if (GameManager.s_IsGameplaySuspended)
            {
                return;
            }
            if (GameManager.s_IsAISuspended)
            {
                return;
            }
            if ((!mBaseAi.IsMoveAgent() || !mBaseAi.m_MoveAgent.enabled && !mBaseAi.m_NavMeshAgent) && (!mBaseAi.m_FirstFrame && CurrentMode == AiMode.Dead))
            {
                ProcessDead();
                return;

            }
            if (mBaseAi.m_ForceToCorpse)
            {
                mBaseAi.m_CurrentHP = 0.0f;
                mBaseAi.StickPivotToGround();
                SetAiMode(AiMode.Dead);
                if (mBaseAi.GetHitInfoUnderPivot(out RaycastHit hitInfo))
                {
                    mBaseAi.AlignTransformWithNormal(hitInfo.point, hitInfo.normal, CurrentMode != AiMode.Dead, true);
                }
                mBaseAi.m_ForceToCorpse = false;
                GameAudioManager.StopAllSoundsFromGameObject(mBaseAi.gameObject);
            }
            if (mBaseAi.m_FirstFrame)
            {
                FirstFrame();
            }
            if (!mBaseAi.IsImposter() && mBaseAi.m_ImposterAnimatorDisabled)
            {
                mBaseAi.m_ImposterAnimatorDisabled = false;
                mBaseAi.m_Animator.cullingMode = mBaseAi.m_ImposterCullingMode;
            }
            else if (!mBaseAi.m_ImposterAnimatorDisabled)
            {
                mBaseAi.m_ImposterCullingMode = mBaseAi.m_Animator.cullingMode;
                mBaseAi.m_Animator.cullingMode = AnimatorCullingMode.CullCompletely;
                mBaseAi.m_ImposterAnimatorDisabled = true;
            }
            mBaseAi.Timberwolf?.MaybeForceHideAndSeek();
            ProcessCurrentAiMode();
            mBaseAi.UpdateAnim();
            if (CurrentTarget != null)
            {
                CurrentTarget.m_BaseAiTargetingMe = mBaseAi;
            }
        }


        protected void ProcessCurrentAiMode()
        {
            PreProcess();
            Process();
            PostProcess();
        }


        #region Helpers/Internal Accessors

        public static float SquaredDistance(Vector3 a, Vector3 b)
        {
            return ((a.x - b.x) * (a.x - b.x)) + ((a.y - b.y) * (a.y - b.y)) + ((a.z - b.z) * (a.z - b.z));
        }


        protected AiTarget CurrentTarget { get { return mBaseAi.m_CurrentTarget; } }
        protected AiMode CurrentMode { get { return mBaseAi.m_CurrentMode; } set { mBaseAi.m_CurrentMode = value; } }
        protected AiMode PreviousMode { get { return mBaseAi.m_PreviousMode; } set { mBaseAi.m_PreviousMode = value; } }
        protected string Name { get { return mBaseAi.gameObject?.name ?? "NULL"; } }


        protected void ClearTargetAndSetDefaultAiMode()
        {
            mBaseAi.ClearTarget();
            SetDefaultAiMode();
            return;
        }


        protected void SetDefaultAiMode()
        {
            SetAiMode(mBaseAi.m_DefaultMode);
        }


        protected bool CheckSceneTransitionStarted(PlayerManager playerManager)
        {
            if (playerManager.m_SceneTransitionStarted)
            {
                SetDefaultAiMode();
                return true;
            }
            return false;
        }


        protected bool CheckTargetDead()
        {
            if (CurrentTarget.IsDead())
            {
                ProcessTargetDead();
                return true;
            }
            return false;
        }


        protected bool TryGetTargetPosition(out Vector3 targetPosition)
        {
            if (CurrentTarget.transform == null)
            {
                targetPosition = Vector3.zero;
                return false;
            }
            targetPosition = CurrentTarget.transform.position;
            return true;
        }


        protected void RefreshTargetPosition()
        {
            if (TryGetTargetPosition(out Vector3 targetPosition))
            {
                mBaseAi.MaybeAdjustTargetPosition(targetPosition);
            }
        }


        protected bool CanReachTarget(Vector3 targetPosition)
        {
            return mBaseAi.CanPlayerBeReached(targetPosition);
        }


        protected bool TryGetInnerRadiusForHoldGroundCause(HoldGroundReason reason, out float innerRadius)
        {
            innerRadius = 0.0f;
            switch (reason)
            {
                case HoldGroundReason.RedFlare:
                    innerRadius = m_HoldGroundDistanceFromFlare;
                    break;
                case HoldGroundReason.Torch:
                    innerRadius = m_HoldGroundDistanceFromTorch;
                    break;
                case HoldGroundReason.SafeHaven:
                    innerRadius = m_MinDistanceToKeepWithSafeHaven + (TryGetPlayerSafeHaven("ProcessAttack", out AuroraField playerSafeHaven) ? playerSafeHaven.m_ExtentRadius : 0.0f);
                    break;
                case HoldGroundReason.NearbyAuroraField:
                    innerRadius = MathF.Max(0.0f, (TryGetContainingAuroraField("ProcessAttack", out AuroraField containingAuroraField) ? containingAuroraField.m_ExtentRadius : 0.0f) - m_ExtraMarginForStopInField);
                    break;
                case HoldGroundReason.InsideAuroraField:
                    innerRadius = MathF.Max(0.0f, (TryGetContainingAuroraField("ProcessAttack", out AuroraField containingAuroraField2) ? containingAuroraField2.m_ExtentRadius : 0.0f) - mBaseAi.m_OuterDistanceFromField);
                    break;
                case HoldGroundReason.Spear:
                    innerRadius = m_HoldGroundDistanceFromSpear;
                    break;
                case HoldGroundReason.Fire:
                    innerRadius = m_HoldGroundDistanceFromFire;
                    break;
                case HoldGroundReason.BlueFlare:
                    innerRadius = m_HoldGroundDistanceFromBlueFlare;
                    break;
                case HoldGroundReason.BlueFlareOnGround:
                    innerRadius = m_HoldGroundDistanceFromBlueFlareOnGround;
                    break;
                case HoldGroundReason.RedFlareOnGround:
                    innerRadius = m_HoldGroundDistanceFromFlareOnGround;
                    break;
                case HoldGroundReason.TorchOnGround:
                    innerRadius = m_HoldGroundDistanceFromTorchOnGround;
                    break;
            }
            return true;
        }


        protected bool TryGetOuterRadiusForHoldGroundCause(HoldGroundReason reason, out float outerRadius)
        {
            outerRadius = 0.0f;
            switch (reason)
            {
                case HoldGroundReason.RedFlare:
                    outerRadius = m_HoldGroundOuterDistanceFromFlare;
                    break;
                case HoldGroundReason.Torch:
                    outerRadius = m_HoldGroundOuterDistanceFromTorch;
                    break;
                case HoldGroundReason.SafeHaven:
                    outerRadius = mBaseAi.m_OuterDistanceFromField + m_MinDistanceToKeepWithSafeHaven + (TryGetPlayerSafeHaven("ProcessAttack", out AuroraField playerSafeHaven) ? playerSafeHaven.m_ExtentRadius : 0.0f);
                    break;
                case HoldGroundReason.NearbyAuroraField:
                    if (!TryGetContainingAuroraField("ProcessAttack", out AuroraField containingAuroraField))
                        return false;
                    outerRadius = mBaseAi.m_OuterDistanceFromField + containingAuroraField.m_ExtentRadius;
                    break;
                case HoldGroundReason.InsideAuroraField:
                    if (!TryGetContainingAuroraField("ProcessAttack", out AuroraField containingAuroraField2))
                        return false;
                    outerRadius = mBaseAi.m_OuterDistanceFromField + containingAuroraField2.m_ExtentRadius + m_ExtraMarginForStopInField;
                    break;
                case HoldGroundReason.Spear:
                    outerRadius = m_HoldGroundOuterDistanceFromSpear;
                    break;
                case HoldGroundReason.Fire:
                    outerRadius = m_HoldGroundOuterDistanceFromFire;
                    break;
                case HoldGroundReason.BlueFlare:
                    outerRadius = m_HoldGroundOuterDistanceFromBlueFlare;
                    break;
                case HoldGroundReason.BlueFlareOnGround:
                    outerRadius = m_HoldGroundOuterDistanceFromBlueFlareOnGround;
                    break;
                case HoldGroundReason.RedFlareOnGround:
                    outerRadius = m_HoldGroundOuterDistanceFromFlareOnGround;
                    break;
                case HoldGroundReason.TorchOnGround:
                    outerRadius = m_HoldGroundOuterDistanceFromTorchOnGround;
                    break;
            }
            return true;
        }


        protected bool TryGetHoldGroundReasonPosition(HoldGroundReason reason, out Vector3 newTargetPosition)
        {
            newTargetPosition = Vector3.zero;
            switch (mBaseAi.m_HoldGroundReason)
            {
                case HoldGroundReason.SafeHaven:
                    if (!TryGetPlayerSafeHaven("ProcessAttack", out AuroraField playerSafeHaven))
                        return false;
                    newTargetPosition = playerSafeHaven.transform.position;
                    break;
                case HoldGroundReason.NearbyAuroraField:
                    if (!TryGetContainingAuroraField("ProcessAttack", out AuroraField containingAuroraField))
                        return false;
                    newTargetPosition = containingAuroraField.transform.position;
                    break;
                case HoldGroundReason.InsideAuroraField:
                    if (!TryGetContainingAuroraField("ProcessAttack", out AuroraField containingAuroraField2))
                        return false;
                    newTargetPosition = containingAuroraField2.transform.position;
                    break;
                case HoldGroundReason.RedFlare:
                case HoldGroundReason.Torch:
                case HoldGroundReason.Spear:
                case HoldGroundReason.Fire:
                case HoldGroundReason.BlueFlare:
                case HoldGroundReason.BlueFlareOnGround:
                case HoldGroundReason.RedFlareOnGround:
                case HoldGroundReason.TorchOnGround:
                    newTargetPosition = CurrentTarget.GetEyePos();
                    break;
            }
            return true;
        }


        protected bool ShouldHoldGround()
        {
            if (!mBaseAi.CanHoldGround())
            {
                return false;
            }
            if (mBaseAi.IsInFlashLight())
            {
                if (mBaseAi.Timberwolf != null)
                {
                    //Log($"[ProcessAttack] Flee, timberwolf!");
                    SetAiMode(AiMode.Flee);
                    return true; //stops next attack action
                }
            }
            if (mBaseAi.MaybeHoldGroundDueToSafeHaven())
            {
                //Log($"[ProcessAttack] Holding ground due to safe haven, aborting...");
                return true;
            }
            if (mBaseAi.m_HoldGroundCooldownSeconds < Time.time - mBaseAi.m_LastTimeWasHoldingGround)
            {
                if (MaybeHoldGroundForAttack(HoldGroundReason.Spear, MaybeHoldGroundForSpear))
                {
                    //Log($"[ProcessAttack] Holding ground due spear threat, aborting...");
                    return true;
                }
            }
            if (mBaseAi.m_HoldGroundCooldownSeconds < Time.time - mBaseAi.m_LastTimeWasHoldingGround)
            {
                if (MaybeHoldGroundForAttack(HoldGroundReason.Torch, (radius) => mBaseAi.MaybeHoldGroundForTorch(radius)))
                {
                    //Log($"[ProcessAttack] Holding ground due torch, aborting...");
                    return true;
                }
                if (MaybeHoldGroundForAttack(HoldGroundReason.TorchOnGround, (radius) => mBaseAi.MaybeHoldGroundForTorchOnGround(radius)))
                {
                    //Log($"[ProcessAttack] Holding ground due to torch on ground, aborting...");
                    return true;
                }
            }
            if (mBaseAi.m_HoldGroundCooldownSeconds < Time.time - mBaseAi.m_LastTimeWasHoldingGround)
            {
                if (MaybeHoldGroundForAttack(HoldGroundReason.RedFlare, (radius) => mBaseAi.MaybeHoldGroundForRedFlare(radius)))
                {
                    //Log($"[ProcessAttack] Holding due to red flare, aborting...");
                    return true;
                }
                if (MaybeHoldGroundForAttack(HoldGroundReason.RedFlareOnGround, (radius) => mBaseAi.MaybeHoldGroundForRedFlareOnGround(radius)))
                {
                    //Log($"[ProcessAttack] Holding due to red flare on ground, aborting...");
                    return true;
                }
            }
            if (mBaseAi.m_HoldGroundCooldownSeconds < Time.time - mBaseAi.m_LastTimeWasHoldingGround)
            {
                if (MaybeHoldGroundForAttack(HoldGroundReason.Fire, (radius) => mBaseAi.MaybeHoldGroundForRedFlare(radius)))
                {
                    //Log($"[ProcessAttack] Holding due to fire, aborting...");
                    return true;
                }
            }
            if (mBaseAi.m_HoldGroundCooldownSeconds < Time.time - mBaseAi.m_LastTimeWasHoldingGround)
            {
                if (MaybeHoldGroundForAttack(HoldGroundReason.BlueFlare, (radius) => mBaseAi.MaybeHoldGroundForBlueFlare(radius)))
                {
                    //Log($"[ProcessAttack] Holding due to blue flare , aborting...");
                    return true;
                }
                if (MaybeHoldGroundForAttack(HoldGroundReason.BlueFlareOnGround, (radius) => mBaseAi.MaybeHoldGroundForBlueFlareOnGround(radius)))
                {
                    //Log($"[ProcessAttack] Holding due to blue flare on ground, aborting...");
                    return true;
                }
            }

            if (mBaseAi.m_HoldGroundReason != HoldGroundReason.None)
            {
                if (!mBaseAi.m_UseSlowdownForHold)
                {
                    if (BaseAi.m_AllowSlowdownForHold && !mBaseAi.IsInFlashLight())
                    {
                        RefreshTargetPosition();
                        if (mBaseAi.m_HoldGroundReason != HoldGroundReason.None)
                        {
                            if (TryGetHoldGroundReasonPosition(mBaseAi.m_HoldGroundReason, out Vector3 newTargetPosition))
                                return false;
                            mBaseAi.m_CurrentRadius = Vector3.Distance(mBaseAi.transform.position, newTargetPosition);
                            if (!TryGetOuterRadiusForHoldGroundCause(mBaseAi.m_HoldGroundReason, out float outerRadius))
                                return false;
                            if (!TryGetInnerRadiusForHoldGroundCause(mBaseAi.m_HoldGroundReason, out float innerRadius))
                                return false;
                            float slowdownRatio = (mBaseAi.m_CurrentRadius - innerRadius) / (outerRadius - innerRadius);
                            slowdownRatio = Mathf.Clamp01(slowdownRatio);
                            slowdownRatio = Mathf.Sqrt(slowdownRatio);
                            mBaseAi.m_SpeedWhileStopping = slowdownRatio * mBaseAi.m_SpeedLimitAtOuterRadius;
                            if (mBaseAi.m_CurrentRadius - innerRadius <= mBaseAi.m_MinDistanceToHoldFromInnerRadius)
                            {
                                if (mBaseAi.m_AiGoalSpeed >= 0.0001f)
                                {
                                    //Log($"[ProcessAttack] player is too close for hold ground, approaching...");
                                    mBaseAi.StartPath(mBaseAi.m_AdjustedTargetPosition, 0.0f, null);
                                    return false;
                                }
                                SetAiMode(AiMode.HoldGround);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        #endregion


        #region Process Error-Catch Helpers

        protected virtual void Fail(string context, string message)
        {
            Log($"[BaseAi.{context}.Fail]: {message}", ComplexLogger.FlaggedLoggingLevel.Error, true);
        }


        protected virtual bool CheckCurrentTargetInvalid(string failContext)
        {
            return CheckCurrentTargetNull(failContext) || CheckCurrentTargetGameObjectNull(failContext);
        }


        protected virtual bool CheckCurrentTargetNull(string failContext)
        {
            if (CurrentTarget == null)
            {
                Fail(failContext, "Null target!");
                SetDefaultAiMode();
                return true;
            }
            return false;
        }


        protected virtual bool CheckCurrentTargetGameObjectNull(string failContext)
        {
            if (CurrentTarget.gameObject == null)
            {
                Fail(failContext, "Null target gameobject!");
                SetDefaultAiMode();
                return true;
            }
            return false;
        }


        protected virtual bool TryGetPlayerSafeHaven(string failContext, out AuroraField playerSaveHaven)
        {
            playerSaveHaven = mBaseAi.m_PlayerSafeHaven;
            if (playerSaveHaven == null)
            {
                Fail(failContext, "Null player safe haven!");
                SetDefaultAiMode();
                return false;
            }
            return true;
        }


        protected virtual bool TryGetContainingAuroraField(string failContext, out AuroraField containingAuroraField)
        {
            containingAuroraField = mBaseAi.m_ContainingAuroraField;
            if (containingAuroraField == null)
            {
                Fail(failContext, "Null containing Aurora Field!");
                SetDefaultAiMode();
                return false;
            }
            return true;
        }

        #endregion
    }
}