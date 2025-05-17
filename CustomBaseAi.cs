using ComplexLogger;
using Il2Cpp;
using UnityEngine;


namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class CustomAiBase : MonoBehaviour, ICustomAi
    {
        public CustomAiBase(IntPtr intPtr) : base(intPtr) { }

        protected BaseAi mBaseAi;
        protected TimeOfDay mTimeOfDay;
        protected EAFManager mManager;
        protected float mTimeSinceCheckForTargetInPatrolWaypointsMode = 0.0f;

        public BaseAi BaseAi { get { return mBaseAi; } }
        public Component Self { get { return this; } }

        //ML is fighting me on dependency injection, doesn't want to "support" injecting my manager class for whatever reason. Feh
        // Occasionally the spawn region is needed during initial setup, and it doesn't always seem to set itself until after the spawn process, so it's being passed here just in case
        public virtual void Initialize(BaseAi ai, TimeOfDay timeOfDay, SpawnRegion spawnRegion)//, EAFManager manager)
        {
            mBaseAi = ai;
            mTimeOfDay = timeOfDay;
            mManager = Manager;// manager;
            OnAugmentDebug();
        }


        public virtual void Despawn(float despawnTime) { } //Override this if you need to handle any kind of longer term tracking


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
                mBaseAi.ProcessDead();
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


        #region BaseAi Overrides & Replacements

        #region Setup

        protected void FirstFrame()
        {
            if (!FirstFrameCustom())
            {
                return;
            }
            if (CurrentMode != AiMode.Dead)
            {
                mBaseAi.StickCharacterControllerToGround();
                if (mBaseAi.GetHitInfoUnderCharacterController(out RaycastHit hitInfo, FindGroundType.FirstTime))
                {
                    mBaseAi.AlignTransformWithNormal(hitInfo.point, hitInfo.normal, CurrentMode != AiMode.Dead, true);
                }
            }
            mBaseAi.DoCustomModeModifiers();
            mBaseAi.MoveAgentStop();
            mBaseAi.m_MoveAgent.m_DestinationReached = true;
            mBaseAi.m_FirstFrame = false;
        }

        #endregion


        #region ProcessCurrentAiMode

        protected void ProcessCurrentAiMode()
        {
            PreProcess();
            Process();
            PostProcess();
        }
        

        protected void PreProcess()
        {
            if (!PreProcessCustom())
            {
                return;
            }
            float deltaTime = Time.deltaTime;
            mBaseAi.m_TimeInModeSeconds += deltaTime;
            mBaseAi.m_TimeInModeTODHours = (24.0f / (mTimeOfDay.m_WeatherSystem.m_DayLengthScale * mTimeOfDay.m_WeatherSystem.m_DayLength)) * deltaTime + mBaseAi.m_TimeInModeTODHours;

            if (mBaseAi.m_CurrentHP <= 0.0001f)
            {
                if (CurrentMode == AiMode.Dead)
                {
                    return;
                }
                SetAiMode(AiMode.Dead);
            }

            if (CurrentMode != AiMode.Dead)
            {
                mBaseAi.MaybeRestoreTargetAfterSpear();
                MaybeHoldGround();
                mBaseAi.MaybeAttemptDodge();
                ProcessWounds(deltaTime);
                ProcessBleeding(deltaTime);
                mBaseAi.m_SuppressFootStepDetectionAndSmellSecondsRemaining -= deltaTime;
                GameAudioManager.SetAudioSourceTransform(mBaseAi.m_EmitterProxy, mBaseAi.m_CachedTransform);
            }
        }


        protected void Process()
        {
            if (!ProcessCustom())
            {
                return;
            }
            switch (CurrentMode)
            {
                case AiMode.Attack: mBaseAi.ProcessAttack(); break;
                case AiMode.Dead: mBaseAi.ProcessDead(); break;
                case AiMode.Feeding: mBaseAi.ProcessFeeding(); break;
                case AiMode.Flee: mBaseAi.ProcessFlee(); break;
                case AiMode.FollowWaypoints: mBaseAi.ProcessFollowWaypoints(); break;
                case AiMode.HoldGround: mBaseAi.ProcessHoldGround(); break;
                case AiMode.Idle: mBaseAi.ProcessIdle(); break;
                case AiMode.Investigate: mBaseAi.ProcessInvestigate(); break;
                case AiMode.InvestigateFood: mBaseAi.ProcessInvestigateFood(); break;
                case AiMode.InvestigateSmell: mBaseAi.ProcessInvestigateSmell(); break;
                case AiMode.Rooted: mBaseAi.ProcessRooted(); break;
                case AiMode.Sleep: mBaseAi.ProcessSleep(); break;
                case AiMode.Stalking: mBaseAi.ProcessStalking(); break;
                case AiMode.Struggle: mBaseAi.ProcessStruggle(); break;
                case AiMode.Wander: mBaseAi.ProcessWander(); break;
                case AiMode.WanderPaused: mBaseAi.ProcessWanderPaused(); break;
                case AiMode.GoToPoint: mBaseAi.ProcessGoToPoint(); break;
                case AiMode.InteractWithProp: mBaseAi.ProcessInteractWithProp(); break;
                case AiMode.ScriptedSequence: mBaseAi.ProcessScriptedSequence(); break;
                case AiMode.Stunned: mBaseAi.ProcessStunned(); break;
                case AiMode.ScratchingAntlers: mBaseAi.Moose?.ProcessScratchingAntlers(); break;
                case AiMode.PatrolPointsOfInterest: mBaseAi.ProcessPatrolPointsOfInterest(); break;
                case AiMode.HideAndSeek: mBaseAi.Timberwolf?.ProcessHideAndSeek(); break;
                case AiMode.JoinPack: mBaseAi.Timberwolf?.ProcessJoinPack(); break;
                case AiMode.PassingAttack: mBaseAi.Timberwolf?.ProcessPassingAttack(); break;
                case AiMode.Howl: mBaseAi.BaseWolf?.ProcessHowl(); break;
            }
        }


        protected void PostProcess()
        {
            if (!PostProcessCustom())
            {
                return;
            }
            if (CurrentMode == AiMode.Dead || CurrentMode == AiMode.ScriptedSequence)
            {
                return;
            }
            if (mBaseAi.IsImposter())
            {
                return;
            }
            SetAnimationParameters();
            if (mBaseAi.m_SpeedForPathfindingOverride)
            {
                return;
            }

            if (mBaseAi.m_MoveAgent == null)
            {
                return;
            }
            mBaseAi.m_MoveAgent.m_MaxSpeed = mBaseAi.m_SpeedFromMecanimBone != null ? mBaseAi.GetSpeedFromMecanimBone() : mBaseAi.m_AiGoalSpeed;
            mBaseAi.m_MoveAgent.m_RotationSpeed =
                (CurrentMode != AiMode.Wander
                && mBaseAi.m_WanderTurnTargets != null
                && mBaseAi.m_WanderTurnTargets.Count > 1
                && mBaseAi.m_WanderCurrentTarget > mBaseAi.m_WanderTurnTargets.Count)
                ? mBaseAi.m_WanderTurnSpeedDegreesPerSecond
                : mBaseAi.m_TurnSpeedDegreesPerSecond;
            mBaseAi.AnimSetFloat(mBaseAi.m_AnimParameter_ActualSpeed, mBaseAi.m_MoveAgent.m_MaxSpeed);
        }

        #endregion


        #region SetAiMode

        protected AiMode PreprocessNewAiMode(AiMode mode)
        {
            if (!PreprocesSetAiModeCustom(mode, out mode))
            {
                //LogDebug($"ProcessSetAiModeCustom injection, routing mode to {mode}");
                return mode;
            }
            if (mode > AiMode.Disabled)
            {
                //Vanilla logic does not know how to pre-process new ai modes, return early here with current mode
                //LogDebug($"Custom AI mode {mode} not handled by custom implementation, deferring to current mode as a fallback");
                return mode;
            }
            if (mode == AiMode.Flee)
            {
                if (CurrentMode == AiMode.Flee && mBaseAi.m_FleeReason == AiFleeReason.AfterPassingAttack)
                {
                    //LogDebug($"Ai is fleeign after passing attack, preventing change mode to {mode}");
                    return AiMode.None;
                }
            }
            else if (mode == AiMode.Attack)
            {
                if (mBaseAi.IsTooScaredToAttack())
                {
                    //LogDebug($"Ai is too scared to attack, preventing change mode to {mode}");
                    return AiMode.None;
                }
                bool skip = false;
                if (mBaseAi.Timberwolf != null)
                {
                    if (mBaseAi.m_CurrentTarget.IsPlayer())
                    {
                        if (CurrentMode == AiMode.Attack)
                        {
                            //LogDebug($"Ai is timberwolf that is already attacking, preventing re-entry to mode {mode}");
                            return AiMode.None;
                        }
                        if (PackManager.InPack(mBaseAi.m_PackAnimal))
                        {
                            if (!GameManager.m_PackManager.CanAttack(mBaseAi.m_PackAnimal, false))
                            {
                                //LogDebug($"Ai is timberwolf that can't attack due to pack mechanics, changing {mode} to HoldGround");
                                mode = AiMode.HoldGround;
                            }
                        }
                        else
                        {
                            //LogDebug($"AI is timberwolf without a pack, routing {mode} to Flee");
                            mode = AiMode.Flee;
                        }
                        skip = true;
                    }
                }
                if (!skip)
                {
                    if (MaybeHoldGround())
                    {
                        //LogDebug($"MaybeHoldGround returned true and set aimode itself, preventing mode change to {mode}");
                        return AiMode.None;
                    }
                    if (!mBaseAi.CanPathfindToPosition(mBaseAi.m_CurrentTarget?.transform?.position ?? Vector3.positiveInfinity, MoveAgent.PathRequirement.FullPath))
                    {
                        //LogDebug($"Can't reach target, changing {mode} to {mBaseAi.m_DefaultMode}");
                        mBaseAi.CantReachTarget();
                        return mBaseAi.m_DefaultMode;
                    }
                }
            }
            else if (mode == AiMode.Wander && mBaseAi.Timberwolf != null && PackManager.InPack(mBaseAi.m_PackAnimal) && GameManager.m_PackManager.IsPackCombatRestricted(mBaseAi.m_PackAnimal))
            {
                //LogDebug($"Special AI timberwolf hold ground trigger, routing mode change from {mode} to {AiMode.HoldGround}");
                mode = AiMode.HoldGround;
            }
            if (mode == AiMode.Wander || mode == AiMode.Flee)
            {
                if (mBaseAi.m_DefaultMode == AiMode.PatrolPointsOfInterest)
                {
                    mode = AiMode.PatrolPointsOfInterest;
                }
            }
            else if (mode == AiMode.None)
            {
                //LogDebug($"Mode change of AiMode.None not caught during preprocessing, changing to idle");
                mode = AiMode.Idle;
            }
            /* weird HL bug catch?
            else if (mode == AiMode.Howl)
            {
                if (mBaseAi.BaseWolf != null)
                {
                    return AiMode.None;
                }
            }
            */
            //These ones aren't really as important, they only apply when triggering the current state which really shouldn't do anything anyways
            if (mode == CurrentMode)
            {
                if (CurrentMode != AiMode.Flee)
                {
                    //LogDebug($"Trying to set AiMode to current mode {mode} which is not AiMode.Flee, triggering early out");
                    return AiMode.None;
                }
                if (mBaseAi.m_UseRetreatSpeedInFlee == false)
                {
                    //LogDebug($"Trying to set AiMode to current mode {mode} and m_UseRetreatSpeedInFlee is false, triggering early out");
                    return AiMode.None;
                }
                mBaseAi.m_UseRetreatSpeedInFlee = false;
                mBaseAi.m_AiGoalSpeed = mBaseAi.GetFleeSpeed();
                //LogDebug($"Trying to set AiMode to current mode {mode}, triggering early out after ajusting flee speed");
                return AiMode.None;
            }
            if (CurrentMode == AiMode.Stunned && mBaseAi.IsStunTimerActive() && mode != AiMode.Dead && mode != AiMode.ScriptedSequence)
            {
                //LogDebug($"Trying to set AiMode to mode {mode} while stunned and stun timer is active, triggering early out");
                return AiMode.None;
            }
            return mode;
        }


        public void SetAiMode(AiMode mode)
        {
            mode = PreprocessNewAiMode(mode);
            if (mode == AiMode.None)
            {
                //LogDebug($"ProcessNewAiMode returned AiMode.None, early-outting setAiMode");
                return;
            }
            ExitAiMode(CurrentMode);
            EnterAiMode(mode);
            PreviousMode = CurrentMode;
            CurrentMode = mode;
            mBaseAi.m_TimeInModeSeconds = 0.0f;
            mBaseAi.m_TimeInModeTODHours = 0.0f;
            GameAudioManager.SetAiStateSwitch(CurrentMode, GameAudioManager.GetSoundEmitterProxyFromGameObject(mBaseAi.gameObject));
        }


        protected void EnterAiMode(AiMode mode)
        {
            if (!EnterAiModeCustom(mode))
            {
                return;
            }
            switch (mode)
            {
                case AiMode.Attack: mBaseAi.EnterAttack(); break;
                case AiMode.Dead: mBaseAi.EnterDead(); break;
                case AiMode.Feeding: mBaseAi.EnterFeeding(); break;
                case AiMode.Flee: mBaseAi.EnterFlee(); break;
                case AiMode.FollowWaypoints: mBaseAi.EnterFollowWaypoints(); break;
                case AiMode.HoldGround: mBaseAi.EnterHoldGround(); break;
                case AiMode.Idle: mBaseAi.EnterIdle(); break;
                case AiMode.Investigate: mBaseAi.EnterInvestigate(); break;
                case AiMode.InvestigateFood: mBaseAi.EnterInvestigateFood(); break;
                case AiMode.InvestigateSmell: mBaseAi.EnterInvestigateSmell(); break;
                case AiMode.Rooted: mBaseAi.EnterRooted(); break;
                case AiMode.Sleep: mBaseAi.EnterSleep(); break;
                case AiMode.Stalking: mBaseAi.EnterStalking(); break;
                case AiMode.Struggle: mBaseAi.EnterStruggle(); break;
                case AiMode.Wander: mBaseAi.EnterWander(); break;
                case AiMode.WanderPaused: mBaseAi.EnterWanderPaused(); break;
                case AiMode.GoToPoint: mBaseAi.EnterGoToPoint(); break;
                case AiMode.InteractWithProp: mBaseAi.EnterInteractWithProp(); break;
                case AiMode.ScriptedSequence: mBaseAi.EnterScriptedSequence(); break;
                case AiMode.Stunned: mBaseAi.EnterStunned(); break;
                case AiMode.ScratchingAntlers: mBaseAi.Moose?.EnterScratchingAntlers(); break;
                case AiMode.PatrolPointsOfInterest: mBaseAi.EnterPatrolPointsOfInterest(); break;
                case AiMode.HideAndSeek: mBaseAi.Timberwolf?.EnterHideAndSeek(); break;
                case AiMode.JoinPack: mBaseAi.Timberwolf?.EnterJoinPack(); break;
                case AiMode.PassingAttack: mBaseAi.Timberwolf?.EnterPassingAttack(); break;
                case AiMode.Howl: mBaseAi.BaseWolf?.EnterHowl(); break;
            }
        }


        protected void ExitAiMode(AiMode mode)
        {
            if (!ExitAiModeCustom(mode))
            {
                return;
            }
            switch (mode)
            {
                case AiMode.Attack: mBaseAi.ExitAttack(); break;
                case AiMode.Dead: mBaseAi.ExitDead(); break;
                case AiMode.Feeding: mBaseAi.ExitFeeding(); break;
                case AiMode.Flee: mBaseAi.ExitFlee(); break;
                case AiMode.FollowWaypoints: mBaseAi.ExitFollowWaypoints(); break;
                case AiMode.HoldGround: mBaseAi.ExitHoldGround(); break;
                case AiMode.Idle: mBaseAi.ExitIdle(); break;
                case AiMode.Investigate: mBaseAi.ExitInvestigate(); break;
                case AiMode.InvestigateFood: mBaseAi.ExitInvestigateFood(); break;
                case AiMode.InvestigateSmell: mBaseAi.ExitInvestigateSmell(); break;
                case AiMode.Rooted: break;
                case AiMode.Sleep: mBaseAi.ExitSleep(); break;
                case AiMode.Stalking: mBaseAi.ExitStalking(); break;
                case AiMode.Struggle: mBaseAi.ExitStruggle(); break;
                case AiMode.Wander: mBaseAi.ExitWander(); break;
                case AiMode.WanderPaused: mBaseAi.ExitWanderPaused(); break;
                case AiMode.GoToPoint: mBaseAi.ExitGoToPoint(); break;
                case AiMode.InteractWithProp: mBaseAi.ExitInteractWithProp(); break;
                case AiMode.ScriptedSequence: mBaseAi.ExitScriptedSequence(); break;
                case AiMode.Stunned: mBaseAi.ExitStunned(); break;
                case AiMode.ScratchingAntlers: mBaseAi.Moose?.ExitScratchingAntlers(); break;
                case AiMode.PatrolPointsOfInterest: mBaseAi.ExitPatrolPointsOfInterest(); break;
                case AiMode.HideAndSeek: mBaseAi.Timberwolf?.ExitHideAndSeek(); break;
                case AiMode.JoinPack: mBaseAi.Timberwolf?.ExitJoinPack(); break;
                case AiMode.PassingAttack: break;
                case AiMode.Howl: mBaseAi.BaseWolf?.ExitHowl(); break;
            }
        }

        #endregion


        #region HoldGround

        protected bool MaybeHoldGround()
        {
            if (!MaybeHoldGroundCustom(out bool shouldHoldGroundCustom))
            {
                return shouldHoldGroundCustom;
            }
            if (mBaseAi.m_AiType != AiType.Predator)
            {
                //LogDebug($"Not predator, cannot hold ground");
                return false;
            }
            if (!mBaseAi.CanHoldGround())
            {
                //LogDebug($"BaseAi.CanHoldGround false, cannot hold ground");
                return false;
            }
            if (((1U << (int)CurrentMode) & (uint)AiModeFlags.EarlyOutMaybeHoldGround) != 0U)
            {
                //LogDebug($"Current mode is {CurrentMode} which precludes holding ground, cannot hold ground");
                return false;
            }
            else if (CurrentMode == AiMode.Attack && mBaseAi.m_IgnoreFlaresAndFireWhenAttacking)
            {
                //LogDebug($"Attacking and ignoring stimulus, cannot hold ground");
                return false;
            }

            if (Time.time - mBaseAi.m_LastTimeWasHoldingGround <= mBaseAi.m_HoldGroundCooldownSeconds)
            {
                return false;
            }

            bool holdingGround = !MaybeHoldGroundForTorchCustom(m_HoldGroundDistanceFromTorch, out bool shouldHoldGroundForTorch)
                ? shouldHoldGroundForTorch
                : mBaseAi.MaybeHoldGroundForTorch(m_HoldGroundDistanceFromTorch);

            holdingGround = holdingGround || (!MaybeHoldGroundForTorchOnGroundCustom(m_HoldGroundDistanceFromTorchOnGround, out bool shouldHoldTorchOnGround)
                ? shouldHoldTorchOnGround
                : mBaseAi.MaybeHoldGroundForTorchOnGround(m_HoldGroundDistanceFromTorchOnGround));

            holdingGround = holdingGround || (!MaybeHoldGroundForFireCustom(m_HoldGroundDistanceFromFire, out bool shouldHoldFire)
                ? shouldHoldFire
                : mBaseAi.MaybeHoldGroundForFire(m_HoldGroundDistanceFromFire));

            holdingGround = holdingGround || (!MaybeHoldGroundForRedFlareCustom(m_HoldGroundDistanceFromFlare, out bool shouldHoldRedFlare)
                ? shouldHoldRedFlare
                : mBaseAi.MaybeHoldGroundForRedFlare(m_HoldGroundDistanceFromFlare));

            holdingGround = holdingGround || (!MaybeHoldGroundForRedFlareOnGroundCustom(m_HoldGroundDistanceFromFlareOnGround, out bool shouldHoldRedFlareOnGround)
                ? shouldHoldRedFlareOnGround
                : mBaseAi.MaybeHoldGroundForRedFlareOnGround(m_HoldGroundDistanceFromFlareOnGround));

            holdingGround = holdingGround || (!MaybeHoldGroundForBlueFlareCustom(m_HoldGroundDistanceFromBlueFlare, out bool shouldHoldBlueFlare)
                ? shouldHoldBlueFlare
                : mBaseAi.MaybeHoldGroundForBlueFlare(m_HoldGroundDistanceFromBlueFlare));

            holdingGround = holdingGround || (!MaybeHoldGroundForBlueFlareOnGroundCustom(m_HoldGroundDistanceFromBlueFlareOnGround, out bool shouldHoldBlueFlareOnGround)
                ? shouldHoldBlueFlareOnGround
                : mBaseAi.MaybeHoldGroundForBlueFlareOnGround(m_HoldGroundDistanceFromBlueFlareOnGround));

            holdingGround = holdingGround || (!MaybeHoldGroundForSpearCustom(m_HoldGroundDistanceFromSpear, out bool shouldHoldSpear)
                ? shouldHoldSpear
                : mBaseAi.MaybeHoldGroundForSpear(m_HoldGroundDistanceFromSpear));

            holdingGround = holdingGround || (!MaybeHoldGroundAuroraFieldCustom(out bool shouldHoldAuroraField)
                ? shouldHoldAuroraField
                : mBaseAi.MaybeHoldGroundAuroraField());

            holdingGround = holdingGround || (!MaybeHoldGroundDueToSafeHavenCustom(out bool shouldHoldSafeHaven)
                ? shouldHoldSafeHaven
                : mBaseAi.MaybeHoldGroundDueToSafeHaven());

            holdingGround = holdingGround || (!MaybeHoldGroundDueToStruggleCustom(out bool shouldHoldStruggle)
                ? shouldHoldStruggle
                : mBaseAi.MaybeHoldGroundDueToStruggle());

            if (holdingGround)
            {
                LogVerbose($"Holding ground!");
                SetAiMode(AiMode.HoldGround);
            }
            return holdingGround;
        }

        #endregion


        #region Imposter Settings

        protected void MaybeImposter()
        {
            mBaseAi.m_Imposter = TestIsImposter();
            UpdateImposterState();
        }


        protected bool TestIsImposter()
        {
            if (!TestIsImposterCustom(out bool isImposter))
            {
                return isImposter;
            }
            return Utils.DistanceToMainCamera(mBaseAi.m_CachedTransform.position) > (Utils.PositionIsOnscreen(mBaseAi.m_CachedTransform.position) ? mBaseAi.m_ImposterDistanceOnScreen : mBaseAi.m_ImposterDistanceOffScreen);
        }


        protected void UpdateImposterState()
        {
            if (!UpdateImposterStateCustom())
            {
                return;
            }
            mBaseAi.m_CharacterController.enabled = !mBaseAi.IsImposter();
        }


        #endregion


        #region Bleeding & Wound Management

        //This one is broken after stepping back some of my half-baked Process() rewrites, so until I get them working vanilla-like this one isn't going to work.
        //If you need to stop bleeding for now, you can override ProcessBleedingCustom
        public bool CanBleedOut()
        {
            return !CanBleedOutCustom(out bool canBleedOut) ? canBleedOut : mBaseAi.CanBleedOut();
        }


        protected virtual void ProcessWounds(float deltaTime)
        {
            if (!ProcessWoundsCustom(deltaTime))
            {
                return;
            }
            if (!mBaseAi.m_Wounded)
            {
                return;
            }
            mBaseAi.m_ElapsedWoundedMinutes += (1440.0f / (mTimeOfDay.m_WeatherSystem.m_DayLength * mTimeOfDay.m_WeatherSystem.m_DayLengthScale)) * deltaTime;
        }


        protected void ProcessBleeding(float deltaTime)
        {
            if (!ProcessBleedingCustom(deltaTime))
            {
                return;
            }
            if (!mBaseAi.m_BleedingOut)
            {
                return;
            }

            mBaseAi.m_ElapsedBleedingOutMinutes += (1440.0f / (mTimeOfDay.m_WeatherSystem.m_DayLength * mTimeOfDay.m_WeatherSystem.m_DayLengthScale)) * deltaTime;
            if (CurrentMode == AiMode.Struggle)
            {
                if (mBaseAi.m_DeathAfterBleeingOutMinutes - mBaseAi.m_ElapsedBleedingOutMinutes < mBaseAi.m_StruggleBleedOutCapTimeMinutes)
                {
                    mBaseAi.m_ElapsedBleedingOutMinutes = mBaseAi.m_DeathAfterBleeingOutMinutes - mBaseAi.m_StruggleBleedOutCapTimeMinutes;
                    return;
                }
            }
            if (mBaseAi.m_ElapsedBleedingOutMinutes >= mBaseAi.m_DeathAfterBleeingOutMinutes)
            {
                mBaseAi.m_ElapsedBleedingOutMinutes = mBaseAi.m_DeathAfterBleeingOutMinutes;
                if (!mBaseAi.Bear?.CanDieFromBleedingOut() ?? false)
                {
                    return;
                }
                mBaseAi.SetDamageImpactParameter(mBaseAi.m_LastDamageSide, mBaseAi.m_LastDamageBodyPart, SetupDamageParamsOptions.None);
                SetAiMode(AiMode.Dead);
            }
        }

        #endregion


        #region Damage Application

        public void ApplyDamage(float damage, float bleedoutMinutes, DamageSource damageSource)
        {
            if (CurrentMode == AiMode.Dead || mBaseAi.m_Invulnerable)
            {
                return;
            }

            if (mBaseAi.m_AiType == AiType.Predator && mBaseAi.m_DamageSource == DamageSource.Player)
            {
                damage *= GameManager.m_AuroraManager.AuroraIsActive() ? GameManager.m_AuroraManager.m_DamageToPredatorsScale : 1.0f;
            }

            mBaseAi.m_CurrentHP -= damage;
            mBaseAi.m_Wounded = true;
            mBaseAi.m_ElapsedWoundedMinutes = 0.0f;

            if ((((mBaseAi.m_CurrentMode == AiMode.Flee) && (mBaseAi.m_FleeReason == AiFleeReason.FleeTriggerVolume)) && (mBaseAi.m_CurrentHP <= 0.0)) && (mBaseAi.CanBleedOut()))
            {
                mBaseAi.m_DeathAfterBleeingOutMinutes = mBaseAi.m_BleedOutTimeMinutesForFleeFromTriggerVolume;
                mBaseAi.m_CurrentHP = 1.0f;
                mBaseAi.m_BleedingOut = true;
                mBaseAi.m_ElapsedBleedingOutMinutes = 0.0f;
                return;
            }
            if (mBaseAi.m_CurrentHP <= 0.0001f)
            {
                SetAiMode(AiMode.Dead);
                if (damageSource != DamageSource.Player)
                {
                    return;
                }
                GameManager.m_AchievementManager.m_HasKilledSomething = true;
            }
            if (PackManager.InPack(mBaseAi.m_PackAnimal) && CurrentMode != AiMode.Struggle)
            {
                GameManager.m_PackManager.ModifyGroupMoraleOnDamage(mBaseAi.m_PackAnimal);
            }
            if (bleedoutMinutes > 0.0f && mBaseAi.CanBleedOut())
            {
                if (!mBaseAi.m_BleedingOut)
                {
                    mBaseAi.m_BleedingOut = true;
                    mBaseAi.m_ElapsedBleedingOutMinutes = 0.0f;
                    mBaseAi.m_DeathAfterBleeingOutMinutes = bleedoutMinutes;
                }
                else if (mBaseAi.m_DeathAfterBleeingOutMinutes - mBaseAi.m_ElapsedBleedingOutMinutes > bleedoutMinutes)
                {
                    mBaseAi.m_DeathAfterBleeingOutMinutes = bleedoutMinutes;
                }

            }
        }

        #endregion


        #region Targetting

        //These previously worked and were relevant when I had the BaseAI methods using them fully patched.
        //Now that I have pulled back those changes this isn't connected, and I'm unwilling to patch it just to make it work right now
        //because it runs on a similar timing to the update loop (if not more frequently)

        //note to self: This already has an early out, we can call it during update loop processing safely
        protected void ScanForNewTarget()
        {
            if (!ScanForNewTargetCustom())
            {
                return;
            }
            if (mBaseAi.m_DisableScanForTargets != false)
            {
                return;
            }
            if (mBaseAi.m_TimeForNextTargetScan >= Time.time)
            {
                return;
            }
            //LogDebug($"Scanning for new target...");
            mBaseAi.m_TimeForNextTargetScan = Time.time + UnityEngine.Random.Range(0.1f, 0.5f); //todo: yoink out the hard coded values
            Vector3 eyePosition = mBaseAi.GetEyePos();
            float distanceToNearestTarget = float.MaxValue;
            AiTarget nearestTarget = null;
            for (int i = BaseAiManager.m_BaseAis.Count - 1, iMin = 0; i >= iMin; i--)
            {
                if (BaseAiManager.m_BaseAis[i] == null)
                {
                    BaseAiManager.m_BaseAis.RemoveAt(i);
                }
                else
                {
                    float distanceToThisTarget = ComputeDistanceForTarget(eyePosition, BaseAiManager.m_BaseAis[i].m_AiTarget);
                    if (distanceToThisTarget < distanceToNearestTarget)
                    {
                        distanceToNearestTarget = distanceToThisTarget;
                        nearestTarget = BaseAiManager.m_BaseAis[i].m_AiTarget;
                    }
                }
            }
            //
            //npc handling code, ignoring
            //
            AiTarget playerTarget = GameManager.GetPlayerObject().GetComponent<AiTarget>();
            bool targettedPlayer = false;
            if (playerTarget != null)
            {
                float distanceToPlayer = ComputeDistanceForTarget(eyePosition, playerTarget);
                if (distanceToPlayer < distanceToNearestTarget)
                {
                    distanceToNearestTarget = distanceToPlayer;
                    nearestTarget = playerTarget;
                    targettedPlayer = true;
                }
            }

            //nearestTarget should pretty much never be null, so this is an error in my opinion
            // Edit: after better understanding "ComputeDistanceForTarget" and how it precludes a lot of targets internally, this check makes much more sense and is no longer an error.
            if (nearestTarget == null)
            {
                //LogDebug($"Potential error, found NO possible additional candidates during scan for new targets. This implies that neither the player nor any other ai scripts exist in scene.");
                return;
            }
            //LogDebug($"Closest target is {nearestTarget} at {nearestTarget.transform.position} which is {distanceToNearestTarget} away");
            AiTarget previousTarget = mBaseAi.m_CurrentTarget;
            mBaseAi.m_CurrentTarget = nearestTarget;

            if (mBaseAi.m_CurrentMode == AiMode.PatrolPointsOfInterest)
            {
                if (mBaseAi.m_CurrentTarget.IsPlayer())
                {
                    if (!mBaseAi.CanPlayerBeReached(mBaseAi.m_CurrentTarget.transform.position, MoveAgent.PathRequirement.FullPath) || !CanSeeTarget(false))
                    {
                        //LogDebug($"Nearest target is player in AiMode.patrolpointsofinterest and PLayer can't be reached, aborting...");
                        mBaseAi.m_CurrentTarget = null;
                        return;
                    }
                }
            }

            if (previousTarget == null || nearestTarget == previousTarget)
            {
                //LogDebug($"previousTarget is null or nearestTarget is same as previousTarget");
                return;
            }

            bool targetDetected = false;
            if (mBaseAi.m_PackAnimal == null || mBaseAi.m_PackAnimal.m_GroupLeader == null)
            {
                if (nearestTarget.IsPlayer() || nearestTarget.IsNpcSurvivor())
                {
                    targetDetected = GameManager.m_PackManager.MaybeFormGroup(mBaseAi.m_PackAnimal);
                }
            }
            GameManager.m_PackManager.MaybeAlertMembers(mBaseAi.m_PackAnimal);
            if (!targetDetected)
            {
                //LogDebug($"Target detected, running ChangeModeWhenTargetDetected");
                ChangeModeWhenTargetDetected(); 
            }

            if (targettedPlayer && mBaseAi.BaseWolf != null && CurrentMode == AiMode.Stalking)
            {
                StatsManager.IncrementValue(Il2CppTLD.Stats.StatID.WolfCloseEncounters, 1.0f);
            }
            return;
        }


        protected bool CanSeeTarget(bool skipDistCheck)
        {
            if (!CanSeeTargetCustom(out bool canSeeTarget))
            {
                //LogDebug($"Custom override, cannot see");
                return canSeeTarget;
            }
            if (Vector3.Angle(mBaseAi.transform.forward, CurrentTarget.transform.position - mBaseAi.transform.position) >= mBaseAi.m_DetectionFOV / 2f)
            {
                //LogDebug($"Target out of field of view, cannot see");
                return false;
            }
            if (!skipDistCheck && ComputeDistanceForTarget(mBaseAi.GetEyePos(), CurrentTarget) == float.PositiveInfinity)
            {
                //LogDebug($"Target distance too great, cannot see");
                return false;
            }
            return true;
        }


        protected float ComputeDistanceForTarget(Vector3 eyePos, AiTarget potentialTarget)
        {
            if (TargetCanBeIgnored(potentialTarget))
            {
                //LogDebug($"Target can be ignored, infinite distance");
                return float.PositiveInfinity;
            }
            Vector3 targetEyePos = potentialTarget.GetEyePos();

            float detectionFOV = 0.0f;
            float detectionRange = mBaseAi.m_DetectionRange;

            if (potentialTarget.IsPlayer())
            {
                PlayerMovement playerMovement = GameManager.m_PlayerMovement;
                AuroraManager auroraManager = GameManager.m_AuroraManager;
                if (GameManager.m_PlayerManager.PlayerIsCrouched())
                {
                    playerMovement.m_LastCrouchedTime = Time.time;
                    detectionFOV = playerMovement.m_DetectionRangeScaleWhenCrouched;
                }
                else
                { 
                    detectionFOV = Time.time;
                    if (detectionFOV - playerMovement.m_LastCrouchedTime < playerMovement.m_CrouchToStandPerceptionDelaySeconds)
                    {
                        detectionFOV = playerMovement.m_DetectionRangeScaleWhenCrouched;
                    }
                    else
                    {
                        detectionFOV = 1.0f;
                    }
                }
                float auroraScalar = auroraManager.AuroraIsActive() ? auroraManager.m_WildlifeDetectionRangeScale : 1.0f;
                float bigCatKillerScalar = 0.0f;//FeatsManager.m_Feat_MasterHunter.IsUnlockedAndEnabled() ? FeatsManager.m_Feat_MasterHunter.m_AiSightRangeScale : 1.0f;
                Feat_MasterHunter bigCatKillerFeat = FeatsManager.m_Feat_MasterHunter;
                if (bigCatKillerFeat.IsUnlockedAndEnabled())
                {
                    //LogDebug($"Master hunter feat enabled! AiSightRangeScale is {bigCatKillerFeat.m_AiSightRangeScale} and SightScale is {bigCatKillerFeat.m_SightScale}");
                    bigCatKillerScalar = bigCatKillerFeat.m_AiSightRangeScale;
                }
                else
                {
                    bigCatKillerScalar = 1.0f;
                }
                detectionRange = auroraScalar * bigCatKillerScalar * detectionFOV * detectionRange;
                
                //Todo: This section is suspect, double check it with a clearer brain some time
                if (GameManager.m_TimeOfDay.IsTimeLapseActive())
                {
                    if (CurrentTarget != potentialTarget)
                    {
                        //LogDebug($"Current Target {CurrentTarget} != potential target {potentialTarget}, infinite distance");
                        return float.PositiveInfinity;
                    }
                }
            }

            if (!AiUtils.PositionVisible(eyePos, mBaseAi.transform.forward, targetEyePos, detectionRange, detectionFOV, 0.0f, 0x100c1b41)) 
            {
                //LogDebug($"target position not visible, infinite distance");
                return float.PositiveInfinity;
            }
            float dist = Vector3.Distance(BaseAi.transform.position, potentialTarget.transform.position);
            //LogDebug($"Distance from ai {mBaseAi.gameObject.name} to target {potentialTarget}: {dist}");
            return dist;
        }


        protected bool TargetCanBeIgnored(AiTarget target)
        {
            if (!TargetCanBeIgnoredCustom(target, out bool canBeIgnored))
            {
                //LogDebug("TargetCanBeIgnoredCustom");
                return canBeIgnored;
            }
            if (target == null)
            {
                //LogDebug("Null Target, ignoring");
                return true;
            }
            /* pretty sure this one precludes the player? Might have interpreted it wrong.
            if (target.m_BaseAi == null)
            {
                LogDebug("Target is not baseAi, ignoring");
                return true;
            }
            */
            if (target.m_BaseAi == this)
            {
                //LogDebug("Target is self, ignoring");
                return true;
            }
            /* Not sure we need this, i dont really care if BaseAI is active or not.
            if (!mBaseAi.enabled)
            {
                return true;
            }
            */
            bool isPlayer = target.IsPlayer();
            if (isPlayer && GameManager.m_PlayerManager.PlayerIsInvisibleToAi())
            {
                //LogDebug("Target is invisible player, ignoring");
                return true;
            }
            if (isPlayer && CurrentMode == AiMode.Feeding)
            {
                //LogDebug("Target is player and am feeding, ignoring");
                return true;
            }
            if (target.IsDead())
            {
                //LogDebug("Target is dead, ignoring (should we make this overridable for zombie wolves...?)");
                return true;
            }
            // NPC survivor code that I'm not going to bother adding
            if (target.IsMoose())
            {
                //LogDebug($"Target is moosing, returning ignore value of {MooseCanBeIgnored()}");
                return MooseCanBeIgnored();
            }
            if (PackManager.InPack(mBaseAi.m_PackAnimal) && !PackManager.IsValidPackTarget(target))
            {
                //LogDebug($"In pack and target is not valid pack target, ignoring");
                return true;
            }
            if (!target.IsHostileTowards(mBaseAi))
            {
                //LogDebug($"Target is not hostile toward me, ignoring");
                return true;
            }
            if (isPlayer)
            {
                if (InterfaceManager.IsMainMenuEnabled())
                {
                    //LogDebug($"Target is player while in main menu, ignoring");
                    return true;
                }
                if (GameManager.m_PlayerStruggle.m_Active)
                {
                    //LogDebug($"Target is player in active struggle, ignoring");
                    return true;
                }
                //LogDebug($"Target is player, NOT ignoring");
                return false;
            }

            if (mBaseAi.WillOnlyTargetPlayer())
            {
                //LogDebug($"Target is not player and will target player, ignoring");
                return true;
            }

            if (mBaseAi.m_WildlifeMode != WildlifeMode.Aurora)
            {
                //LogDebug($"Target is not player and non-aurora wildlife, NOT ignoring");
                return false;
            }

            if (AuroraManager.m_AuroraFieldsSceneManager.GetFieldContaining(target.transform.position) != null)
            {
                //LogDebug($"Target is not player and aurora wildlife and target is not in aurora field, NOT ignoring");
                return false;
            }

            //LogWarning($"CustomBaseAi.TargetCanBeIgnored reached end of method with no cases returning a valid condition. Falling through to 'can ignore target' condition");
            return true;
        }


        protected void ChangeModeWhenTargetDetected()
        {
            if (!ChangeModeWhenTargetDetectedCustom())
            {
                //LogDebug($"ChangeModeWhenTargetDetectedCustom");
                return;
            }

            if (mBaseAi.m_AiType == AiType.Human)
            {
                //LogDebug($"Astrid and mackenzie aren't ai... yet...");
                return;
            }

            //I can't even find this method in the decompile. 90% sure from the logic shown here it's just animation related, so probably points head at player, etc...
            mBaseAi.DoOnDetection();

            //todo: move to moose-specific ai script
            if (mBaseAi.Moose != null && mBaseAi.m_CurrentTarget.IsPlayer())
            {
                //LogDebug($"Moose specific hold ground catch");
                SetAiMode(AiMode.HoldGround);
                return;
            }

            //Stripped out some hunted pt. 2 ai logic here. It just bloats survival mode.

            if (CurrentMode == AiMode.Feeding)
            {
                if (!BaseAi.ShouldAlwaysFleeFromCurrentTarget())
                {
                    //LogDebug($"Feeding and should always flee from current target, fleeing");
                    SetAiMode(AiMode.Flee);
                }
                return;
            }

            if (mBaseAi.Timberwolf?.CanEnterHideAndSeek() ?? false)
            {
                //LogDebug($"Target found, timberwolf entering hide and seek");
                SetAiMode(AiMode.HideAndSeek);
            }

            float fleeChance = mBaseAi.m_FleeChanceWhenTargetDetected;
            if (!mBaseAi.m_CurrentTarget.IsHostileTowards(mBaseAi) || mBaseAi.m_CurrentTarget.IsAmbient())
            {
                //LogDebug($"Target found, target is not hostile or ai is ambient, no flee chance");
                fleeChance = 0.0f;
            }

            //Move to Moose AI
            /*
            if (mBaseAi.Moose != null)
            {
                if (!mBaseAi.m_CurrentTarget.IsMoose())
                {
                    if (!mBaseAi.m_CurrentTarget.IsBear())
                    {
                        if (mBaseAi.m_CurrentTarget.IsBaseWolf())
                        {
                            goto LAB_18053d9be;
                        }
                    }
                    if (!mBaseAi.m_CurrentTarget.IsAmbient())
                    {
                        goto LAB_18053d9c1;
                    }
                }
                LAB_18053d9be:
                fleeChance = 0.0f;
            }
            LAB_18053d9c1:
            */

            if (mBaseAi.m_AiType == AiType.Predator)
            {
                if (mBaseAi.m_CurrentTarget.IsVulnerable() || (CurrentMode != AiMode.Wander))
                {
                    //LogDebug($"Target found, am predator and target is vulnerable OR currentmode {CurrentMode} is not wander, zero flee chance");
                    fleeChance = 0.0f;
                }
            }

            //Move to bear AI
            /*
            if (mBaseAi.Bear == null && mBaseAi.m_BleedingOut)
            {
                fleeChance = 75.0f;
            }
            */

            if (mBaseAi.m_CurrentTarget.IsPlayer() && mBaseAi.BaseWolf != null)
            {
                fleeChance += GameManager.m_PlayerManager.m_IncreaseWolfFleePercentagePoints; 
                //LogDebug($"Playertarget found, am wolf, flee chance increased to {fleeChance}");
            }

            if (mBaseAi.m_AiType == AiType.Predator)
            {
                AuroraManager auroraManager = GameManager.m_AuroraManager;
                fleeChance *= auroraManager.AuroraIsActive() ? auroraManager.m_PredatorFleeChanceScale : 1.0f;
                //LogDebug($"Playertarget found, am aurora wolf: {auroraManager.AuroraIsActive()}, flee chance multiplied to {fleeChance}");
            }

            if (CurrentMode == AiMode.Wander && mBaseAi.m_WanderingAroundPos)
            {
                //LogDebug($"CurrentMode is wander and wandering around pos, flee chance is zero");
                fleeChance = 0.0f;
            }

            if (PackManager.InPack(mBaseAi.m_PackAnimal) && !GameManager.m_PackManager.ShouldAnimalFlee(mBaseAi.m_PackAnimal))
            {
                //LogDebug($"In pack and packmanager says no flee, flee chance is zero");
                fleeChance = 0.0f;
            }

            //move to cougar AI
            if (mBaseAi.m_CurrentTarget.IsPlayer() && mBaseAi.Cougar != null)
            {
                //LogDebug($"Cougar is not scared of you, flee chance is zero");
                fleeChance = 0.0f;
            }

            if (mBaseAi.ShouldAlwaysFleeFromCurrentTarget())
            {
                //LogDebug($"Should always flee from current target, flee chance is 100%");
                fleeChance = 100.0f;
            }

            if (Utils.RollChance(fleeChance))
            {
                //LogDebug($"Random roll with fleeChance {fleeChance} triggered fleeing");
                SetAiMode(AiMode.Flee);
                return;
            }

            //More challenge bear code stripped away here

            if (mBaseAi.CanEnterStalking())
            {
                SetAiMode(AiMode.Stalking);
            }

            if (mBaseAi.BaseWolf != null)
            {
                mBaseAi.m_Curious = true;
            }
        }


        protected bool MooseCanBeIgnored()
        {
            if (MooseCanBeIgnoredCustom(out bool mooseCanBeIgnored))
            {
                return mooseCanBeIgnored;
            }
            return mBaseAi.m_AiType != AiType.Ambient && mBaseAi.m_AiSubType != AiSubType.Wolf;
        }

        #endregion


        #region Animation

        //By far my least favorite method in BaseAi
        //Seriously, fuck this method
        //If i had a dollar for every time i traced a bug back to it I'd be rich
        //If you're reading this and working on this mod, do better than me... please...
        protected void SetAnimationParameters()
        {
            float someFloat = mBaseAi.m_AiGoalSpeed;
            if (mBaseAi.m_Wounded) //Original code seemed to limit this to just wolves, which is weird...
            {
                someFloat = (someFloat - mBaseAi.m_WalkSpeed) * Mathf.Clamp01(1 - mBaseAi.GetWoundedAnimParameter()) + mBaseAi.m_WalkSpeed;
            }
            mBaseAi.m_MoveAgent.SetMoveSpeed(someFloat);
            mBaseAi.SetAnimStateForMoveAgent(IsMoveState(CurrentMode) ? MoveState.Moving : MoveState.Idle, (int)GetAiAnimationState(CurrentMode));
            if (mBaseAi.BaseWolf != null)
            {
                if (!Utils.Approximately(mBaseAi.m_AnimParameter_Wounded_LastSent, mBaseAi.GetWoundedAnimParameter(), 0.0001f))
                {
                    mBaseAi.AnimSetFloat(mBaseAi.m_AnimParameter_Wounded, mBaseAi.GetWoundedAnimParameter());
                    mBaseAi.m_AnimParameter_Wounded_LastSent = mBaseAi.GetWoundedAnimParameter();
                }
            }
            mBaseAi.SetTargetHeadingParameter();
            if (mBaseAi.m_CanPlayPitchRoll)
            {
                Vector3 forward = mBaseAi.m_CachedTransform.forward;
                forward.y = 0f;
                forward.Normalize();
                float pitchValue = Vector3.Dot(forward, mBaseAi.m_CachedTransform.forward);
                if (mBaseAi.m_CachedTransform.forward.y < 0f)
                {
                    pitchValue = -pitchValue;
                }
                mBaseAi.AnimSetFloat(mBaseAi.m_AnimParameter_Pitch, pitchValue);
                Vector3 right = mBaseAi.m_CachedTransform.right;
                right.y = 0f;
                right.Normalize(); ;
                float rollValue = Vector3.Dot(right, mBaseAi.m_CachedTransform.right);
                if (mBaseAi.m_CachedTransform.right.y < 0f)
                {
                    rollValue = -rollValue;
                }
                BaseAi.AnimSetFloat(mBaseAi.m_AnimParameter_Roll, rollValue);
            }
            if (mBaseAi.m_CanPlayTurn != false)
            {
                if (Time.deltaTime > 0.0001f)
                {
                    Vector3 currentForward = mBaseAi.m_CachedTransform.forward;
                    mBaseAi.m_PreviousForward = new Vector3(currentForward.x, 0f, currentForward.z).normalized;
                    mBaseAi.m_turnSpeed = Utils.GetAngleDegrees(new Vector3(currentForward.x, 0f, currentForward.z).normalized, mBaseAi.m_PreviousForward) / Time.deltaTime;
                }
                BaseAi.AnimSetFloat(mBaseAi.m_AnimParameter_TurnSpeed, mBaseAi.m_turnSpeed);
                SetTurnAngleParameters(mBaseAi.m_Animator, mBaseAi.m_TotalTurnAngle, mBaseAi.m_turnSpeed, mBaseAi.m_TurnHeading, mBaseAi.m_CachedTransform.forward, mBaseAi.m_AnimParameter_TurnAngle);
            }
            if (mBaseAi.Moose == null)
            {
                mBaseAi.Moose?.m_Animator.SetBoolID(mBaseAi.m_AnimParameter_IsInjured, mBaseAi.m_Wounded);
            }
            return;
        }


        protected AiAnimationState GetAiAnimationState(AiMode mode)
        {
            if (!GetAiAnimationStateCustom(mode, out AiAnimationState overrideState))
            {
                return overrideState;
            }
            switch (mode)
            {
                case AiMode.None:
                case AiMode.Rooted:
                case AiMode.Struggle:
                case AiMode.WanderPaused:
                case AiMode.Stunned:
                case AiMode.ScratchingAntlers:
                case AiMode.JoinPack:
                case AiMode.Howl:
                case AiMode.Disabled:
                case AiMode.Dead:
                    return AiAnimationState.Paused;
                case AiMode.Attack:
                case AiMode.PassingAttack:
                    return AiAnimationState.Attack;
                case AiMode.Feeding:
                case AiMode.InvestigateFood:
                    return AiAnimationState.Feeding;
                case AiMode.Flee:
                    return AiAnimationState.Flee;
                case AiMode.Wander:
                case AiMode.FollowWaypoints:
                case AiMode.PatrolPointsOfInterest:
                    return AiAnimationState.Wander;
                case AiMode.HoldGround:
                    return AiAnimationState.HoldGround;
                case AiMode.Idle:
                    return AiAnimationState.Idle;
                case AiMode.Investigate:
                    return AiAnimationState.Investigate;
                case AiMode.InvestigateSmell:
                    return AiAnimationState.InvestigateSmell;
                case AiMode.Sleep:
                    return AiAnimationState.Sleep;
                case AiMode.Stalking:
                case AiMode.HideAndSeek:
                    return AiAnimationState.Stalking;
                case AiMode.GoToPoint:
                    return AiAnimationState.GoToPoint;
                case AiMode.InteractWithProp:
                    return AiAnimationState.InteractWithProp;
                case AiMode.ScriptedSequence:
                    return AiAnimationState.ScriptedSequence;
            }
            return AiAnimationState.Invalid;
        }


        protected bool IsMoveState(AiMode mode)
        {
            return !IsMoveStateCustom(mode, out bool isMoveState) ? isMoveState : ((AiModeFlags)(1UL << (int)mode)).AnyOf(AiModeFlags.MovementAllowed);
        }

        #endregion

        #endregion


        #region Helpers/Internal Accessors

        protected AiTarget CurrentTarget { get { return mBaseAi.m_CurrentTarget; } set { mBaseAi.m_CurrentTarget = value; } }
        protected AiMode CurrentMode { get { return mBaseAi.m_CurrentMode; } set { mBaseAi.m_CurrentMode = value; } }
        protected AiMode PreviousMode { get { return mBaseAi.m_PreviousMode; } set { mBaseAi.m_PreviousMode = value; } }
        protected string Name { get { return mBaseAi.gameObject?.name ?? "NULL"; } }

        protected void SetDefaultAiMode()
        {
            SetAiMode(mBaseAi.m_DefaultMode);
        }

        #endregion


        #region Virtuals


        #region Properties

        protected virtual float m_HoldGroundDistanceFromSpear { get { return mBaseAi.m_HoldGroundDistanceFromSpear; } }
        protected virtual float m_HoldGroundOuterDistanceFromSpear { get { return mBaseAi.m_HoldGroundOuterDistanceFromSpear; } }
        protected virtual float m_HoldGroundDistanceFromBlueFlare { get { return mBaseAi.m_HoldGroundDistanceFromBlueFlare; } }
        protected virtual float m_HoldGroundOuterDistanceFromBlueFlare { get { return mBaseAi.m_HoldGroundOuterDistanceFromBlueFlare; } }
        protected virtual float m_HoldGroundDistanceFromBlueFlareOnGround { get { return mBaseAi.m_HoldGroundDistanceFromBlueFlareOnGround; } }
        protected virtual float m_HoldGroundOuterDistanceFromBlueFlareOnGround { get { return mBaseAi.m_HoldGroundOuterDistanceFromBlueFlareOnGround; } }
        protected virtual float m_HoldGroundDistanceFromFire { get { return mBaseAi.m_HoldGroundDistanceFromFire; } }
        protected virtual float m_HoldGroundOuterDistanceFromFire { get { return mBaseAi.m_HoldGroundOuterDistanceFromFire; } }
        protected virtual float m_HoldGroundDistanceFromFlare { get { return mBaseAi.m_HoldGroundDistanceFromFlare; } }
        protected virtual float m_HoldGroundOuterDistanceFromFlare { get { return mBaseAi.m_HoldGroundOuterDistanceFromFlare; } }
        protected virtual float m_HoldGroundDistanceFromFlareOnGround { get { return mBaseAi.m_HoldGroundDistanceFromFlareOnGround; } }
        protected virtual float m_HoldGroundOuterDistanceFromFlareOnGround { get { return mBaseAi.m_HoldGroundOuterDistanceFromFlareOnGround; } }
        protected virtual float m_HoldGroundDistanceFromTorch { get { return mBaseAi.m_HoldGroundDistanceFromTorch; } }
        protected virtual float m_HoldGroundOuterDistanceFromTorch { get { return mBaseAi.m_HoldGroundOuterDistanceFromTorch; } }
        protected virtual float m_HoldGroundDistanceFromTorchOnGround { get { return mBaseAi.m_HoldGroundDistanceFromTorchOnGround; } }
        protected virtual float m_HoldGroundOuterDistanceFromTorchOnGround { get { return mBaseAi.m_HoldGroundOuterDistanceFromTorchOnGround; } }
        protected virtual float m_MinDistanceToKeepWithSafeHaven { get { return mBaseAi.m_MinDistanceToKeepWithSafeHaven; } }
        protected virtual float m_ExtraMarginForStopInField { get { return mBaseAi.m_ExtraMarginForStopInField; } }
        protected virtual float m_MinDistanceToHoldFromInnerRadius { get { return mBaseAi.m_MinDistanceToHoldFromInnerRadius; } }
        protected virtual float m_MaxWaypointDistance { get { return 1.0f; } }
        protected virtual float m_MinWaypointDistance { get { return 0.0f; } }

        #endregion


        #region Setup

        /// <summary>
        /// Intercept or inject logic into parent first frame setup. 
        /// Vanilla logic applies difficulty modifiers and sticks character to ground if not dead.
        /// </summary>
        /// <returns>Return false halt parent first frame logic. Return true to allow parent first frame logic to execute.</returns>
        protected virtual bool FirstFrameCustom() => true;

        #endregion


        #region HoldGround


        //Override any of these and return false to decide whether or not the ai should hold ground.

        protected virtual bool MaybeHoldGroundCustom(out bool shouldHoldGround)
        {
            shouldHoldGround = false;
            return true;
        }


        protected virtual bool MaybeHoldGroundForTorchCustom(float radius, out bool shouldHoldGround)
        {
            shouldHoldGround = false;
            return true;
        }


        protected virtual bool MaybeHoldGroundForTorchOnGroundCustom(float radius, out bool shouldHoldGround)
        {
            shouldHoldGround = false;
            return true;
        }


        protected virtual bool MaybeHoldGroundForFireCustom(float radius, out bool shouldHoldGround)
        {
            shouldHoldGround = false;
            return true;
        }


        protected virtual bool MaybeHoldGroundForRedFlareCustom(float radius, out bool shouldHoldGround)
        {
            shouldHoldGround = false;
            return true;
        }


        protected virtual bool MaybeHoldGroundForRedFlareOnGroundCustom(float radius, out bool shouldHoldGround)
        {
            shouldHoldGround = false;
            return true;
        }


        protected virtual bool MaybeHoldGroundForBlueFlareCustom(float radius, out bool shouldHoldGround)
        {
            shouldHoldGround = false;
            return true;
        }


        protected virtual bool MaybeHoldGroundForBlueFlareOnGroundCustom(float radius, out bool shouldHoldGround)
        {
            shouldHoldGround = false;
            return true;
        }


        protected virtual bool MaybeHoldGroundForSpearCustom(float radius, out bool shouldHoldGround)
        {
            shouldHoldGround = false;
            return true;
        }


        protected virtual bool MaybeHoldGroundAuroraFieldCustom(out bool shouldHoldGround)
        {
            shouldHoldGround = false;
            return true;
        }


        protected virtual bool MaybeHoldGroundDueToSafeHavenCustom(out bool shouldHoldGround)
        {
            shouldHoldGround = false;
            return true;
        }


        protected virtual bool MaybeHoldGroundDueToStruggleCustom(out bool shouldHoldGround)
        {
            shouldHoldGround = false;
            return true;
        }

        #endregion


        #region Update & State Processing

        /// <summary>
        /// Intercept or inject logic into parent update loop. 
        /// Vanilla logic runs the entire process, so you will need to route your own state machine if you halt this method.
        /// </summary>
        /// <returns>Return false halt prevent parent updating. Return true to allow parent updating.</returns>
        protected virtual bool UpdateCustom() => true;


        /// <summary>
        /// Intercept changing of AI modes at the very beginning. Useful if you want to preclude certain behaviors altogether, such as
        /// a wolf that won't attack by turning any "attack", "stalk", "HoldGround", etc state into flee
        /// </summary>
        /// <param name="mode">Incoming AiMode</param>
        /// <param name="newMode">New AiMode to inject</param>
        /// <returns>Return false to skip parent preprocess checks and force set new mode. Return true to allow parent to preprocess newMode, whatever it may have changed to. </returns>
        protected virtual bool PreprocesSetAiModeCustom(AiMode mode, out AiMode newMode)
        {
            newMode = mode;
            return true;
        }


        /// <summary>
        /// Allows intercepting of preprocessing logic.
        /// Vanilla logic increments time in mode, updates wounds/bleeding and tries to trigger some preemptive behaviors like dodging and retargetting.
        /// </summary>
        /// <returns>Return false to halt parent state preprocessing. Return true to allow parent state preprocessing.</returns>
        protected virtual bool PreProcessCustom() => true;


        /// <summary>
        /// Allows intercepting of current mode processing logic. 
        /// Necessary if you are using any non-vanilla state enum values.
        /// Useful if you want to override any base behaviors like attacking or wandering which would otherwise interrupt your intent.
        /// </summary>
        /// <returns>Return false if you are engaging any custom behaviors or overriding any parent behaviors. Return true to allow parent to process current state.</returns>
        protected virtual bool ProcessCustom() => true;


        /// <summary>
        /// Allows intercepting of postprocessing logic.
        /// Vanilla logic primarily handles animation speed updating.
        /// Really not much to see here but provides an easy access point for injecting logic between the end of one state processing frame and the beginning of another.
        /// </summary>
        /// <returns>Return false to halt parent state postprocessing. Return true to allow parent state postprocessing.</returns>
        protected virtual bool PostProcessCustom() => true;


        /// <summary>
        /// Allows handling of on-enter events for custom states, as well as overriding on-enter events for base ai states.
        /// </summary>
        /// <param name="mode">State being entered</param>
        /// <returns>Return false to prevent parent on-enter event from firing. Return true to allow parent on-enter events to fire.</returns>
        protected virtual bool EnterAiModeCustom(AiMode mode) => true;


        /// <summary>
        /// Allows handling of on-exit events for custom states, as well as overriding on-exit events for base ai states.
        /// </summary>
        /// <param name="mode">State being entered</param>
        /// <returns>Return false to prevent parent on-enter event from firing. Return true to allow parent on-enter events to fire.</returns>
        protected virtual bool ExitAiModeCustom(AiMode mode) => true;


        /// <summary>
        /// Allows injection of custom target scanning logic.
        /// Vanilla logic looks for the nearest ai target or player, whichever is closer, and tries to target.
        /// If new target does not match old target, ChangeModeWhenTargetDetected is run. This can be overridden in a separate method.
        /// Only override this method if you want to adjust how new targets are acquired.
        /// </summary>
        /// <returns>Return false to prevent parent retargetting logic from firing. Return true to allow parent re-targetting logic to fire.</returns>

        protected virtual bool ScanForNewTargetCustom() => true;


        /// <summary>
        /// Allows injection of custom logic for handling behavior upon acquiring a new target.
        /// 
        /// </summary>
        /// <returns></returns>
        protected virtual bool ChangeModeWhenTargetDetectedCustom() => false;


        #endregion


        #region Imposter Settings

        /// <summary>
        /// Allows intercepting of parent application of imposter status to an AI. 
        /// Classic logic sets m_Imposter to true and disables character controller entirely (as far as I can tell? further testing is needed here).
        /// </summary>
        /// <returns>Return false to halt parent imposter logic. Return true to allow parent to handle imposter state change.</returns>
        protected virtual bool UpdateImposterStateCustom() => true;


        /// <summary>
        /// Allows intercepting of parent imposter status calculation.
        /// Classic logic tests a number of things like distance to camera, whether ai is on screen, etc.
        /// A never-imposter AI can be created by returning true with isImposter set to false. 
        /// </summary>
        /// <returns>Return false to halt parent imposter calculation. Return true to allow parent to handle imposter calculations.</returns>
        protected virtual bool TestIsImposterCustom(out bool isImposter)
        {
            isImposter = false;
            return true;
        }

        #endregion


        #region Animation

        /// <summary>
        /// Allows intercepting of parent AiAnimationState mapping from input AiMode.
        /// Allows for functional routing of custom ai modes to existing (or new?) AiAnimationState values.
        /// </summary>
        /// <param name="mode">Incoming mode. Usually CurrentMode but leaving the parameter open for calculation purposes</param>
        /// <param name="overrideState">Return your own state here. Required for custom states, optionally can override base game states as well.</param>
        /// <returns>Return false to override base mapping with your own overrideState. Return true to allow parent to handle animation state mapping.</returns>
        protected virtual bool GetAiAnimationStateCustom(AiMode mode, out AiAnimationState overrideState)
        {
            overrideState = AiAnimationState.Invalid;
            return true;
        }


        /// <summary>
        /// Allows intercepting of parent move state flag mapping from input AiMode, and for functional routing of custom ai modes to move state flag.
        /// Setting this to false will preclude any AI movement, be careful!
        /// </summary>
        /// <param name="mode">Incoming mode. Usually CurrentMode but leaving the parameter open for calculation purposes</param>
        /// <param name="overrideState">Return your own preference here</param>
        /// <returns>Return false to override base mapping with your own move state. Return true to allow parent to handle move state determination.</returns>
        protected virtual bool IsMoveStateCustom(AiMode mode, out bool isMoveState)
        {
            isMoveState = false;
            return true;
        }

        #endregion


        #region Damage & Wound/Bleeding

        /// <summary>
        /// Allows intercepting of parent logic for wound processing. 
        /// Vanilla logic increments BaseAi.m_ElapsedWoundedMinutes.
        /// </summary>
        /// <param name="deltaTime">frame time</param>
        /// <returns>return false to halt parent wound processing. Return true to allow parent wound processing.</returns>
        protected bool ProcessWoundsCustom(float deltaTime) => true;


        /// <summary>
        /// Allows intercepting of parent logic for bleeding out. 
        /// Vanilla logic increments BaseAi.m_ElapsedWoundedMinutes.
        /// </summary>
        /// <param name="deltaTime">frame time</param>
        /// <returns>return false to halt parent bleeding processing. Return true to allow parent bleeding processing.</returns>
        protected bool ProcessBleedingCustom(float deltaTime) => true;


        /// <summary>
        /// Allows intercepting of base game logic for bleed out qualification. 
        /// Vanilla logic is surprisingly obfuscated, but we all pretty much know that Moose (and i think cougar?) can't bleed out in vanilla.
        /// </summary>
        /// <param name="canBleedOut">Return your own value here</param>
        /// <returns>Return false to override parent CanBleedOut logic with your own. Return true to defer to parent calculations.</returns>
        protected virtual bool CanBleedOutCustom(out bool canBleedOut)
        {
            canBleedOut = false;
            return true;
        }

        #endregion


        #region Targetting

        protected virtual bool TargetCanBeIgnoredCustom(AiTarget target, out bool canBeIgnored)
        {
            canBeIgnored = false;
            return true;
        }


        protected virtual bool CanSeeTargetCustom(out bool canSeeTarget)
        {
            canSeeTarget = false;
            return true;
        }


        protected virtual bool MooseCanBeIgnoredCustom(out bool mooseCanBeIgnored)
        {
            mooseCanBeIgnored = false;
            return true;
        }

        #endregion



        #endregion


        #region ***DEBUG***

        protected void LogTrace(string message) { Utility.LogTrace($"[{this}]: {message}"); }
        protected void LogDebug(string message) { Utility.LogDebug($"[{this}]: {message}"); ; }
        protected void LogVerbose(string message) { Utility.LogVerbose($"[{this}]: {message}"); ; }
        protected void LogWarning(string message) { Utility.LogWarning($"[{this}]:  {message}"); ; }
        protected void LogError(string message, FlaggedLoggingLevel additionalFlags = 0U) { Utility.LogError($"[{this}]:  {message}", additionalFlags); }
        protected void LogCriticalError(string message) { LogError($"[{this}]:  {message}", FlaggedLoggingLevel.Critical); }
        protected void LogException(string message) { LogError($"[{this}]:  {message}", FlaggedLoggingLevel.Exception); }


#if DEV_BUILD
        protected AiMode mCachedMode = AiMode.None;
        protected bool mReadout = false;
        public Transform mMarkerTransform;
        public Renderer mMarkerRenderer;
#endif

        private void OnAugmentDebug()
        {
#if DEV_BUILD_STATELABEL

            GameObject marker = EAFManager.Instance.CreateMarker(mBaseAi.transform.position, Color.clear, $"Debug Ai Marker for {mBaseAi.name}", 100, 0.5f);
            mMarkerTransform = marker.transform;
            mMarkerTransform.SetParent(mBaseAi.transform);
            mMarkerRenderer = marker.GetComponent<Renderer>();
            SetMarkerColor();
#endif
        }


        private void OnUpdateDebug()
        {
#if DEV_BUILD_STATELABEL
            SetMarkerColor();
#endif
        }

#if DEV_BUILD_STATELABEL

        public void SetMarkerColor()
        {
            if (mMarkerRenderer != null && mMarkerRenderer.material != null)
            {
#if DEV_BUILD_STATELABEL
                mMarkerRenderer.material.color = GetMarkerColorByState();
#endif
            }
        }


        public Color GetMarkerColorByState()
        {
            if (mCachedMode != CurrentMode)
            {
                mCachedMode = CurrentMode;
                mMarkerRenderer.gameObject.active = true;
            }
            switch (CurrentMode)
            {
                case AiMode.Wander:
                case AiMode.PatrolPointsOfInterest:
                case AiMode.FollowWaypoints:
                case AiMode.GoToPoint:
                    return Color.grey;
                case AiMode.Attack:
                case AiMode.PassingAttack:
                case AiMode.Struggle:
                    return Color.red;
                case AiMode.Feeding:
                case AiMode.Dead:
                case AiMode.Idle:
                    return Color.blue;
                case AiMode.Flee:
                case AiMode.WanderPaused:
                    return Color.green;
                case AiMode.Investigate:
                case AiMode.InvestigateFood:
                case AiMode.InvestigateSmell:
                case AiMode.Stalking:
                case AiMode.HideAndSeek:
                    return new Color(255, 0, 255);
                case AiMode.ScratchingAntlers:
                case AiMode.ScriptedSequence:
                case AiMode.InteractWithProp:
                case AiMode.Sleep:
                    return new Color(255, 255, 0);
                case AiMode.Rooted:
                case AiMode.None:
                case AiMode.Disabled:
                    return Color.black;
                default:
                    if (CurrentMode > AiMode.Disabled)
                    {
                        return Color.white;
                    }
                    else
                    {
                        mMarkerRenderer.gameObject.active = false;
                        return Color.clear;
                    }
            }
        }
#endif


        #endregion
    }
}