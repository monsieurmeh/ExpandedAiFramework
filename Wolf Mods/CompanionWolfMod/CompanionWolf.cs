using Il2Cpp;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.Runtime;
using Il2CppRewired.Utils;
using Il2CppSuperSplines;
using UnityEngine;
using UnityEngine.UI;

namespace ExpandedAiFramework.CompanionWolfMod
{
    [RegisterTypeInIl2Cpp]
    public class CompanionWolf : BaseWolf
    {
        // A lot of things here are referencing the submanager for data; we are intentionally not storing blackboard/long term data on this script,
        // as it disappears during scene load and I never really know if i might lose it at any time.
        // Anything that doesnt need to persist between scenes like short term behavioral timers can live here.

        //have to be careful, we'll start looping back at 32 so we only get one more state. We'll need to hijack vanilla processes after that
        private enum CompanionWolfAiMode : int
        {
            Follow = (int)AiMode.Disabled + 1,
            Fetch,
            BigCarry,
            LAST_STATE_DONT_ADD_MORE_THAN_THIS,
            COUNT
        }

        private const AiModeFlags CompanionWolfAiModes = ((AiModeFlags)(1U << (int)CompanionWolfAiMode.Follow))
                                                        | ((AiModeFlags)(1U << (int)CompanionWolfAiMode.Fetch))
                                                        | ((AiModeFlags)(1U << (int)CompanionWolfAiMode.BigCarry))
                                                        | ((AiModeFlags)(1U << (int)CompanionWolfAiMode.LAST_STATE_DONT_ADD_MORE_THAN_THIS));


        private const AiModeFlags UntamedCompanionWolfRouteToHoldGroundModes = AiModeFlags.Attack
                                                                        | AiModeFlags.Stalking
                                                                        | AiModeFlags.PassingAttack
                                                                        | AiModeFlags.Struggle;


        private const AiModeFlags UntamedCompanionWolfOverrideModes = AiModeFlags.InvestigateFood
                                                                    | AiModeFlags.InvestigateSmell
                                                                    | AiModeFlags.HoldGround
                                                                    | CompanionWolfAiModes;


        private const AiModeFlags TamedCompanionWolfOverrideModes = AiModeFlags.Attack
                                                                |   AiModeFlags.Stalking
                                                                |   AiModeFlags.Wander
                                                                |   AiModeFlags.Feeding
                                                                |   AiModeFlags.InvestigateFood
                                                                |   AiModeFlags.HideAndSeek
                                                                |   AiModeFlags.HoldGround
                                                                |   AiModeFlags.Idle
                                                                | CompanionWolfAiModes;


        private const AiModeFlags TamedCompanionFeedingStateLockModes = AiModeFlags.Feeding | AiModeFlags.InvestigateFood;

        private const float MinPlayerDistanceFromDecoy = 2.0f;
        private const float MaxWolfDistanceFromDecoy = 50.0f;
        private const float MinPlayerDistanceFromWolf = 5.0f;
        private const float FollowDistForRunSpeed = 50.0f;
        private const float FollowCheckInterval = 1.0f;
        private const float FollowDist = 5.0f;
        private const float MaxScale = 2.0f;
        private const float GrowthPerDay = 0.1f;
        private const float CheckTamedStateDebugFrequency = 5.0f;

        internal static CompanionWolfSettings Settings;

        protected CompanionWolfManager mSubManager;
        protected GearItem mCurrentFoodTargetGearItem;
        protected Text mStatusText;

        protected float mCheckTamedStateDebugResetTime = 0.0f;
        protected float mCheckForDecoyMeatTime = 0.0f;
        protected float mCheckForFollowTime = 0.0f;
        protected bool mFollowing = false;

        public CompanionWolfData PersistentData { get { return mSubManager.Data; } }


        public CompanionWolf(IntPtr ptr) : base(ptr) { }

        public override void Initialize(BaseAi ai, TimeOfDay timeOfDay, SpawnRegion spawnRegion)//, EAFManager manager)
        {
            base.Initialize(ai, timeOfDay, spawnRegion);//, manager);
            if (!mManager.SubManagers.TryGetValue(GetType(), out ISubManager subManager))
            {
                LogError("Could not fetch submanager for CompanionWolf!");
                return;
            }
            CompanionWolfManager companionWolfManager = subManager as CompanionWolfManager;
            if (companionWolfManager == null)
            {
                LogError("Fetched submanager for CompanionWolf is NOT a CompanionWolfManager, type mismatch!");
                return;
            }
            mSubManager = companionWolfManager;
            LogDebug($"Initializing companion! Data: Connected = {mSubManager.Data.Connected}");
            mSubManager.Data.Initialize(GameManager.m_ActiveScene, ai, spawnRegion);
            mBaseAi.m_MaxHP = mSubManager.Data.MaxCondition;
            mBaseAi.m_CurrentHP = mSubManager.Data.CurrentCondition;
            mBaseAi.transform.localScale = new Vector3(mSubManager.Data.Scale, mSubManager.Data.Scale, mSubManager.Data.Scale);
            float currentTime = Utility.GetCurrentTimelinePoint();
            float timePassed = (currentTime - mSubManager.Data.LastDespawnTime) * Utility.HoursToSeconds;
            mSubManager.Data.LastDespawnTime = currentTime;
            mSubManager.Instance = this;
            if (mSubManager.Data.Tamed)
            {
                mBaseAi.m_DefaultMode = (AiMode)CompanionWolfAiMode.Follow;
                mBaseAi.m_CurrentMode = (AiMode)CompanionWolfAiMode.Follow;
                SetupInfoWindow();
            }
            else
            {
                mBaseAi.m_DefaultMode = AiMode.Wander;
                mBaseAi.m_CurrentMode = AiMode.Wander;
            }
            UpdateStats(timePassed);
            LogDebug($"Initialized CompanionWolf with {mBaseAi.m_MaxHP}/{mBaseAi.m_MaxHP} condition and scale {mBaseAi.transform.localScale}; Tamed: {mSubManager.Data.Tamed}; Time passed since last load: {timePassed} seconds or {timePassed * Utility.SecondsToHours} hrs; CurrentTime: {currentTime} hrs; Listed timeout time: {mSubManager.Data.UntamedTimeoutTime} which is {mSubManager.Data.UntamedTimeoutTime - currentTime} hrs away");
        }

        public override bool ShouldAddToBaseAiManager() => false;


        private void SetupInfoWindow()
        {
#if DEV_BUILD
            try
            {
                GameObject canvasGO = new GameObject("WorldCanvas");
                canvasGO.transform.SetParent(transform);
                Canvas canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = Camera.main;

                CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.dynamicPixelsPerUnit = 5;

                canvasGO.AddComponent<GraphicRaycaster>();

                RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(50, 25);

                // Create Panel
                GameObject panelGO = new GameObject("Panel");
                panelGO.transform.SetParent(canvasGO.transform);
                RectTransform panelRect = panelGO.AddComponent<RectTransform>();
                panelRect.sizeDelta = new Vector2(50, 20);
                Image panelImage = panelGO.AddComponent<Image>();
                panelImage.color = new Color(0, 0, 0, 0.6f); // semi-transparent black

                // Create Text
                GameObject textGO = new GameObject("Text");
                textGO.transform.SetParent(panelGO.transform);
                RectTransform textRect = textGO.AddComponent<RectTransform>();
                textRect.sizeDelta = new Vector2(46, 18);
                textRect.localPosition = Vector3.zero;

                mStatusText = textGO.AddComponent<Text>();
                mStatusText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                mStatusText.alignment = TextAnchor.MiddleLeft;
                mStatusText.color = Color.white;
                mStatusText.fontSize = 6;
                mStatusText.horizontalOverflow = HorizontalWrapMode.Overflow;
                mStatusText.verticalOverflow = VerticalWrapMode.Overflow;
                UpdateStatusText();

                // Initial position
                canvasGO.transform.localPosition = new Vector3(0f, 5.0f, 0f);
                canvasGO.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            }
            catch (Exception e)
            {
                LogError($"Error while trying out some text stuff: {e}");
            }
#endif
        }


        public void UpdateStatusText()
        {
#if DEV_BUILD
            if (!mSubManager.Data.Tamed)
            {
                return; //no window to update
            }
            string targetName = "null";
            if (CurrentTarget != null)
            {
                targetName = CurrentTarget.gameObject.name;
            }

            mStatusText.text = $"Name:      Floofy\n" +
                               $"Condition: {(int)mSubManager.Data.CurrentCondition}/{(int)Settings.MaximumCondition}\n" +
                               $"Hunger:    {(int)mSubManager.Data.CurrentCalories}/{(int)Settings.MaximumCalorieIntake}\n" +
                               $"Affection: {(int)mSubManager.Data.CurrentAffection}\n" +
                               $"State:     {CurrentMode}\n";
                               //$"AnimationState:    {(AiAnimationState)mBaseAi.m_MoveAgent.m_CurrentAnimState}\n" +
                               //$"MoveState:         {(MoveState)mBaseAi.m_MoveAgent.m_MoveState}\n" +
                               //$"CurrentTarget:     {targetName}";
            mStatusText.transform.parent.parent.forward = GameManager.GetPlayerObject().transform.forward;
#endif
        }


        public override void Despawn(float despawnTime)
        {
            mSubManager.Data.LastDespawnTime = despawnTime;
            base.Despawn(despawnTime);
        }


        protected override bool PreprocesSetAiModeCustom(AiMode mode, out AiMode newMode)
        {
            if (!mSubManager.Data.Tamed && mode.ToFlag().AnyOf(UntamedCompanionWolfRouteToHoldGroundModes))
            {
                LogDebug($"Untamed companions dont like to {mode}, routing to HoldGround!");
                newMode = AiMode.HoldGround;
                return false;
            }
            if (!mSubManager.Data.Tamed && mode.ToFlag().AnyOf(UntamedCompanionWolfOverrideModes))
            {
                LogDebug($"Untamed companion with mode {mode}, ignoring typical pre-processing in favor of CompanionWolf.");
                newMode = mode;
                return false;
            }
            if (mSubManager.Data.Tamed && mode.ToFlag().AnyOf(TamedCompanionWolfOverrideModes))
            {
                if (mode == AiMode.HoldGround)
                {
                    LogDebug($"Temporary catch to prevent tamed wolf from holding ground against friendly things like fire. Eventually we'll program in proper behavior. For right now i just want him to sit with me by the fire <3");
                    newMode = AiMode.None;
                    return false;
                }
                if (CurrentMode == AiMode.Wander)
                {
                    LogDebug($"Temporary catch to route tamed wander to follow. good boy!");
                    newMode = (AiMode)CompanionWolfAiMode.Follow;
                    return false;
                }
                if (CurrentTarget != null && CurrentTarget.IsPlayer() && mode.ToFlag().AnyOf(AiModeFlags.Stalking | AiModeFlags.Attack | AiModeFlags.Struggle))
                {
                    //Recipe #19 for "Friendly Floofy"
                    LogDebug($"Preventing attempted savaging and devouring of mackenzie/astrid by floofy. good boy!");
                    newMode = AiMode.None;
                    return false;
                }
                // If feeding or investigating food and the request is not one of those two and we still have a food target, ignore request
                if (CurrentMode.ToFlag().AnyOf(TamedCompanionFeedingStateLockModes) && TamedCompanionFeedingStateLockModes.NoneOf(mode.ToFlag()) && mCurrentFoodTargetGearItem != null)
                {
                    LogDebug($"Preventing attempt to interrupt hungry floofy. Bad developers!");
                    newMode = AiMode.None;
                    return false;
                }
                LogDebug($"Tamed companion with mode {mode}, ignoring typical pre-processing in favor of CompanionWolf.");
                newMode = mode;
                return false;
            }
            newMode = mode;
            return true;
        }


        //run using actual time passed in seconds
        protected void UpdateStats(float deltaTime)
        {
            //LogDebug($"Calories: {mSubManager.Data.CurrentCalories} will be reduced by {deltaTime * Settings.CaloriesBurnedPerDay * Utility.SecondsToDays} to {mSubManager.Data.CurrentCalories - deltaTime * Settings.CaloriesBurnedPerDay * Utility.SecondsToDays}\nAffection: {mSubManager.Data.CurrentAffection} will be reduced by {deltaTime * Settings.AffectionDecayDelayHours * Utility.SecondsToHours} to {mSubManager.Data.CurrentAffection - deltaTime * Settings.AffectionDecayDelayHours * Utility.SecondsToHours}");
            mSubManager.Data.CurrentCalories -= deltaTime * Settings.CaloriesBurnedPerDay * Utility.SecondsToDays;
            mSubManager.Data.CurrentAffection -= deltaTime * (mSubManager.Data.Tamed ? Settings.TamedAffectionDecayRate : Settings.UntamedAffectionDecayRate) * Utility.SecondsToHours;

            if (mSubManager.Data.CurrentCalories < 0.0f)
            {
                mSubManager.Data.CurrentCalories = 0.0f;
                if (mSubManager.Data.Tamed) //untamed wolves don't deteriorate from calorie loss
                {
                    mSubManager.Data.CurrentCondition -= deltaTime * Settings.StarvingConditionDecayPerHour * Utility.SecondsToHours;
                    mBaseAi.m_CurrentHP = mSubManager.Data.CurrentCondition;
                }
            }

            if (mSubManager.Data.CurrentAffection < 0.0f)
            {
                mSubManager.Data.CurrentAffection = 0.0f;
                if (mSubManager.Data.Tamed) //Tamed wolves will run away at this point :(
                {
                    LogDebug("Tamd wolf affection reached zero and it ran away! How could you let this happen... :( :( :(");
                    mSubManager.Data.Disconnect();
                    mManager.TryRemoveCustomAi(mBaseAi);
                    GameObject.Destroy(mBaseAi.transform.parent.gameObject);
                    return;
                }
            }

            if (mSubManager.Data.Scale <= MaxScale)
            {
                mSubManager.Data.Scale += deltaTime * GrowthPerDay * Utility.SecondsToDays;
                mBaseAi.transform.localScale = new Vector3(mSubManager.Data.Scale, mSubManager.Data.Scale, mSubManager.Data.Scale);
            }
        }


        protected override bool PostProcessCustom()
        {
            UpdateStats(RealTimeToGameTime(Time.deltaTime));
            if (!mSubManager.Data.Tamed && mSubManager.Data.UntamedTimeoutTime <= Utility.GetCurrentTimelinePoint())
            {
                //turn into "disconnect" method
                LogDebug("Disappear! go away! until next time :-(");
                mSubManager.Data.Disconnect();
                mManager.TryRemoveCustomAi(mBaseAi);
                return false;
            }
            if (mSubManager.Data.Tamed && Time.time - mCheckTamedStateDebugResetTime < CheckTamedStateDebugFrequency)
            {
                mCheckTamedStateDebugResetTime = Time.time;
                LogDebug("Debug tame state reset check");
                if (CurrentMode.ToFlag().AnyOf(UntamedCompanionWolfRouteToHoldGroundModes) && CurrentTarget.IsPlayer())
                {
                    SetAiMode((AiMode)CompanionWolfAiMode.Follow);
                }
            }
            return true;
        }


        protected override bool EnterAiModeCustom(AiMode mode)
        {
            mCheckForDecoyMeatTime = Time.time;
            if (mode == AiMode.Dead)
            {
                LogDebug("Entered AiMode.Dead and DIED! How could you let this happen... :( :( :(");
                mSubManager.Data.Disconnect();
                mManager.TryRemoveCustomAi(mBaseAi);
                return false;
            }
            if (!mSubManager.Data.Tamed)
            {
                return true;
            }
            else
            {
                switch (mode)
                {
                    case (AiMode)CompanionWolfAiMode.Follow:
                        if (mBaseAi.m_PathTargetTransform == null || mBaseAi.m_PathTargetTransform.IsNullOrDestroyed())
                        {
                            mBaseAi.m_PathTargetTransform = new GameObject();
                        }
                        mFollowing = false;
                        return false;
                }
            }
            return true;
        }


        protected override bool ProcessCustom()
        {
            if (!mSubManager.Data.Tamed)
            {
                switch (CurrentMode)
                {
                    //Maybe switch to preprocess?
                    case AiMode.HoldGround: return ProcessHoldGroundCustom();
                    case AiMode.Wander: return ProcessWanderCustom();
                    case AiMode.Feeding: return ProcessFeedingCustom();
                    case AiMode.InvestigateFood: return ProcessInvestigateFoodCustom();
                }
            }
            else
            {
                switch (CurrentMode)
                {
                    case (AiMode)CompanionWolfAiMode.Follow: return ProcessFollow();
                    case AiMode.Idle: return ProcessIdleCustom();
                    case AiMode.InvestigateFood: return ProcessInvestigateFoodCustom();
                    case AiMode.Feeding: return ProcessFeedingCustom();
                }
            }
            return true;
        }


        protected override bool ExitAiModeCustom(AiMode mode)
        {
            /*
            if (!mSubManager.Data.Tamed)
            {
                switch (mode)
                {
                    case (AiMode)CompanionWolfAiMode.BigCarry:
                    case (AiMode)CompanionWolfAiMode.Follow:
                    case (AiMode)CompanionWolfAiMode.Fetch:
                    case (AiMode)CompanionWolfAiMode.LAST_STATE_DONT_ADD_MORE_THAN_THIS:
                        return true;
                }
            }
            else
            {
                switch (mode)
                {
                    case (AiMode)CompanionWolfAiMode.BigCarry:
                    case (AiMode)CompanionWolfAiMode.Follow:
                    case (AiMode)CompanionWolfAiMode.Fetch:
                    case (AiMode)CompanionWolfAiMode.LAST_STATE_DONT_ADD_MORE_THAN_THIS:
                        return true;
                }
            }
            */
            return true;
        }



        private bool ProcessHoldGroundCustom()
        {
            AiUtils.TurnTowardsTarget(mBaseAi);
            ScanForNewTarget();
            if (CheckForDecoyMeat())
            {
                //LogVerbose($"Valid decoy found, aborting hold ground!");
                return false;
            }
            if (!mBaseAi.m_CurrentTarget.IsPlayer())
            {
                //LogVerbose($"Target not player, deferring...");
                return true;
            }
            if (mBaseAi.m_TimeInModeSeconds >= 10.0f)
            {
                //LogVerbose($"Player took too long to drop food clos to an untamed companion wolf, running away!");
                SetAiMode(AiMode.Flee);
                return false;
            }
            if (Vector3.Distance(mBaseAi.transform.position, mBaseAi.m_CurrentTarget.transform.position) <= MinPlayerDistanceFromWolf)
            {
                //LogVerbose($"Player got too close to an untamed companion wolf, running away!"); 
                SetAiMode(AiMode.Flee);
                return false;
            }
            //LogVerbose($"Player target causing hold ground state!");
            return false;
        }


        private bool ProcessWanderCustom()
        {
            bool hasNewWanderPos = false;
            Vector3 wanderPos = Vector3.zero;
            ScanForNewTarget();
            MaybeImposter(); 
            if (mBaseAi.IsImposter())
            {
                mBaseAi.m_AiGoalSpeed = 0.0f;
                return false;
            }
            mBaseAi.m_AiGoalSpeed = mBaseAi.m_WalkSpeed;
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
                }
                if (!hasNewWanderPos)
                {
                    mBaseAi.m_CurrentWanderPos = mBaseAi.transform.position;
                    hasNewWanderPos = AiUtils.GetClosestNavmeshPos(out wanderPos, mBaseAi.m_CachedTransform.position, mBaseAi.m_CachedTransform.position);
                    if (!hasNewWanderPos)
                    {
                        mBaseAi.MoveAgentStop();
                        SetDefaultAiMode();
                        return false;
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
                        return false;
                    }
                    hasNewWanderPos = mBaseAi.StartPath(mBaseAi.m_WanderTurnTargets[0], mBaseAi.m_WalkSpeed);
                }

                if (!hasNewWanderPos)
                {
                    SetDefaultAiMode();
                    return false;
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
                return false;
            }

            mBaseAi.MaybeHoldGroundAuroraField();
            mBaseAi.MaybeEnterWanderPause();

            UniStormWeatherSystem uniStormWeatherSystem = mTimeOfDay.m_WeatherSystem;
            mBaseAi.m_ElapsedWanderHours += (24.0f / (uniStormWeatherSystem.m_DayLengthScale * uniStormWeatherSystem.m_DayLength)) * Time.deltaTime;
            return false;
        }


        protected bool CheckForDecoyMeat()
        {
            if (Time.time - mCheckForDecoyMeatTime < 1.0f)
            {
                return false;
            }
            LogDebug("Decoy meat check");
            mCheckForDecoyMeatTime = Time.time;
            Il2CppSystem.Collections.Generic.List<GearItem> droppedDecoys = GearManager.m_DroppedDecoys;
            for (int i = 0, iMax = droppedDecoys.Count; i < iMax; i++)
            { 
                if (!mSubManager.Data.Tamed && Vector3.Distance(CurrentTarget.transform.position, droppedDecoys[i].transform.position) <= MinPlayerDistanceFromDecoy)
                {
                    LogDebug($"Player is too close to decoy!");
                    continue;
                }
                /* They can smell this shit, if its close enough its close enough...
                if (Vector3.Angle(mBaseAi.transform.forward, CurrentTarget.transform.position - mBaseAi.transform.position) >= mBaseAi.m_DetectionFOV / 2f)
                {
                    LogVerbose($"Decoy out of field of view, cannot see");
                    return false;
                }
                */
                if (Vector3.Distance(mBaseAi.transform.position, droppedDecoys[i].transform.position) >= MaxWolfDistanceFromDecoy)
                {
                    LogDebug($"Decoy is too far away!");
                    continue;
                }
                mBaseAi.m_InvestigateFoodObject = droppedDecoys[i].gameObject;
                mCurrentFoodTargetGearItem = droppedDecoys[i].GetComponent<GearItem>();
                if (mCurrentFoodTargetGearItem == null)
                {
                    LogDebug($"No gear item on target!");
                    continue;
                }

                LogDebug($"Food found, investigating!");
                SetAiMode(AiMode.InvestigateFood);
                return true;
            }
            mBaseAi.m_InvestigateFoodObject = null;
            mCurrentFoodTargetGearItem = null;
            return false;
        }

        
        private bool ProcessInvestigateFoodCustom()
        {
            //LogDebug($"Investigating Food like a GOOD BOY");
            if (!mSubManager.Data.Tamed && Vector3.Distance(mBaseAi.transform.position, GameManager.GetPlayerObject().transform.position) <= MinPlayerDistanceFromDecoy)
            {
                LogDebug($"Player too close to food, swapping to hold ground!");
                SetAiMode(AiMode.HoldGround);
                return false;
            }
            if (mBaseAi.CloseEnoughToEatObject(mBaseAi.m_InvestigateFoodObject))// || Vector3.Distance(mBaseAi.m_InvestigateFoodObject.transform.position, mBaseAi.transform.position) <= 2.0f)
            {
                LogDebug($"Close enough to each, transitioning to eating!");
                mBaseAi.MoveAgentStop();
                SetAiMode(AiMode.Feeding);
            }
            else
            {
                if (!mBaseAi.m_MoveAgent.m_DestinationReached)
                {
                    if (!mBaseAi.m_MoveAgent.HasPath())
                    {
                        LogDebug($"Hasn't reached dest and no path, this could be a bug. Resetting to default mode");
                        SetDefaultAiMode();
                        return false;
                    }
                    if (CurrentTarget == null)
                    {
                        ScanForNewTarget();
                    }
                    if (!mSubManager.Data.Tamed && mBaseAi.m_CurrentTarget.Distance(mBaseAi.transform.position) <= mBaseAi.m_InvestigateFoodAvoidTargetDistance)
                    {
                        LogDebug($"Target got too close, fleeing!");
                        mBaseAi.ClearTarget();
                        SetAiMode(AiMode.Flee);
                    }
                }
            }
            
            return false;
        }


        protected bool ProcessFeedingCustom()
        {
            /*
            if (mBaseAi.MaybeWaitForStopAgent() || mBaseAi.MaybeSyncToFeeding())
            {
                LogVerbose($"Waiting for animation sync");
                return false;
            }
            */
            if (!mBaseAi.m_DidStopAudio)
            {
                LogDebug($"Audio queue");
                mBaseAi.m_FeedingAudioID = GameAudioManager.Play3DSound(mBaseAi.m_FeedingAudio, mBaseAi.gameObject);
            }
            if (!mSubManager.Data.Tamed && CurrentTarget != null && CurrentTarget.IsPlayer() && CurrentTarget.Distance(mBaseAi.transform.position) <= MinPlayerDistanceFromWolf)
            {
                LogDebug($"Too close to untamed feeding wolf, running away!");
                SetAiMode(AiMode.Flee);
                return false;
            }
            if (CurrentMode == AiMode.Feeding)
            {
                float deltaTime = RealTimeToGameTime(Time.deltaTime); //We'll need to adjust settings to reflect that the eating rate is per in game hour, not real time second
                mCurrentFoodTargetGearItem.m_FoodItem.m_CaloriesRemaining -= Settings.CaloriesConsumedPerGameHour * Utility.SecondsToHours * deltaTime;
                mSubManager.Data.CurrentCalories += Settings.CaloriesConsumedPerGameHour * Utility.SecondsToHours * deltaTime;
                if (!mSubManager.Data.Tamed || mSubManager.Data.CurrentAffection <= Settings.MaximumAffectionFromFeeding)
                {
                    mSubManager.Data.CurrentAffection += Settings.CaloriesConsumedPerGameHour * Settings.AffectionPerCalorie * Utility.SecondsToHours * deltaTime;
                }
                if (!mSubManager.Data.Tamed && mSubManager.Data.CurrentAffection >= Settings.AffectionRequirement && GameManager.m_TimeOfDay.m_DaysSurvivedLastFrame >= Settings.AffectionDaysRequirement)
                {
                    LogDebug($"You tamed it! YOU DID IT! WOO!!");
                    mSubManager.Data.Tamed = true;
                    mBaseAi.m_DefaultMode = (AiMode)CompanionWolfAiMode.Follow;
                    SetAiMode((AiMode)CompanionWolfAiMode.Follow);
                    SetupInfoWindow();
                }
                bool finishedEating = false;
                if (mSubManager.Data.CurrentCalories >= Settings.MaximumCalorieIntake)
                {
                    LogDebug($"Full!");
                    mSubManager.Data.CurrentCalories = Settings.MaximumCalorieIntake;
                    finishedEating = true;
                }
                if (mCurrentFoodTargetGearItem.m_FoodItem.m_CaloriesRemaining <= 0)
                {
                    LogDebug($"Done!");
                    GearManager.DestroyGearObject(mCurrentFoodTargetGearItem);
                    finishedEating = true;
                }
                if (finishedEating && !CheckForDecoyMeat())
                {
                    LogDebug($"Done eating and no food found, wandering away");
                    SetDefaultAiMode();
                }
            }
            return false;
        }


        protected bool ProcessFollow()
        {
            if (CheckForDecoyMeat())
            {
                //LogVerbose($"Valid decoy found, aborting hold ground!");
                return false;
            }
            if (Time.time - mCheckForFollowTime < FollowCheckInterval)
            {
                return false;
            }
            ScanForNewTarget();
            if (CurrentTarget == null)
            {
                CurrentTarget = GameManager.m_PlayerManager.m_AiTarget;
            }
            mCheckForFollowTime = Time.time;
            Vector3 playerPosition = GameManager.m_PlayerManager.m_LastPlayerPosition;
            Vector3 currentPosition = new Vector3(mBaseAi.transform.position.x, playerPosition.y, mBaseAi.transform.position.z); //prevent vertical following of player. oops! lmao
            float currentDistance = Vector3.Distance(currentPosition, GameManager.m_PlayerManager.m_LastPlayerPosition);
            if (currentDistance >= FollowDist)
            {
                Vector3 followDirection = (currentPosition - playerPosition).normalized;
                Vector3 followPosition = playerPosition + followDirection * (FollowDist * 0.80f); // Agent needs to get a LITTLE closer otherwise it moves a bunch while mackenzie shifts around
                if (!mBaseAi.CanPlayerBeReached(followPosition))
                {
                    LogVerbose("Can't reach player, warping...");
                    mBaseAi.m_MoveAgent.transform.position = followPosition;
                    mBaseAi.m_MoveAgent.Warp(followPosition, 1.0f, true, -1);
                }
                else
                {
                    Mathf.Clamp(currentDistance, FollowDist, FollowDistForRunSpeed);
                    mBaseAi.StartPath(followPosition, Mathf.Lerp(mBaseAi.m_WalkSpeed, FollowDistForRunSpeed, (currentDistance - FollowDist) / (FollowDistForRunSpeed - FollowDist)) * mSubManager.Data.Scale);
                    mFollowing = true;
                }
            }
            else
            {
                mBaseAi.MoveAgentStop();
                SetAiMode(AiMode.Idle);
            }
            return false;
        }


        protected bool ProcessIdleCustom()
        {
            if (Time.time - mCheckForFollowTime < FollowCheckInterval)
            {
                return false;
            }
            ScanForNewTarget();
            if (CurrentTarget == null)
            {
                CurrentTarget = GameManager.m_PlayerManager.m_AiTarget;
            }
            mCheckForFollowTime = Time.time;
            if (Vector3.Distance(mBaseAi.transform.position, GameManager.m_PlayerManager.m_LastPlayerPosition) > FollowDist)
            {
                mBaseAi.MoveAgentStop();
                SetAiMode((AiMode)CompanionWolfAiMode.Follow);
            }
            return false;
        }


        protected override bool GetAiAnimationStateCustom(AiMode mode, out AiAnimationState overrideState)
        {
            switch (mode)
            {
                case AiMode.Idle:
                    overrideState = AiAnimationState.Paused;
                    return false;
                case (AiMode)CompanionWolfAiMode.Follow:
                    overrideState = AiAnimationState.Wander;
                    return false;
                default:
                    overrideState = AiAnimationState.Invalid;
                    return true;
            }
        }


        protected override bool IsMoveStateCustom(AiMode mode, out bool isMoveState)
        {
            switch (mode)
            {
                case AiMode.Idle:
                    isMoveState = false;
                    return false;
                case (AiMode)CompanionWolfAiMode.Follow:
                    isMoveState = true;
                    return false;
                default:
                    isMoveState = false;
                    return true;
            }
        }


        protected override bool ChangeModeWhenTargetDetectedCustom()
        {
            LogDebug($"ChangeModeWhenTargetDetectedCustom");
            if (mSubManager.Data.Tamed)
            {
                if (CurrentTarget.IsPlayer())
                {
                    LogDebug($"Tamed wolf sees player");
                    //right now we dont care
                    return false;
                }
            }
            else
            {
                //use some flags here for clarity, other modes might pop up too like eating, etc
                if (CurrentTarget.IsPlayer() && CurrentMode == AiMode.Wander)
                {
                    LogDebug($"Untamed wolf sees player in wander mode, hold ground!");
                    SetAiMode(AiMode.HoldGround);
                    return false;
                }
            }
            if (CurrentTarget.IsBear() || CurrentTarget.IsCougar() || CurrentTarget.IsMoose())
            {
                SetAiMode(AiMode.Flee);
                return false;
            }
            LogDebug($"no catch, defer...");
            return true;
        }
    }
}




