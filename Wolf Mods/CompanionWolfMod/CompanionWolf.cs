using Il2CppRewired;
using UnityEngine;


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


        private const AiModeFlags UntamedCompanionWolfRouteToFleeModes = AiModeFlags.Attack
                                                                        | AiModeFlags.Stalking
                                                                        | AiModeFlags.PassingAttack
                                                                        | AiModeFlags.Struggle
                                                                        | AiModeFlags.HoldGround;

        private const AiModeFlags UntamedCompanionWolfOverrideModes = AiModeFlags.InvestigateFood
                                                                    | AiModeFlags.InvestigateSmell
                                                                    | AiModeFlags.HoldGround
                                                                    | CompanionWolfAiModes;

        private const AiModeFlags TamedCompanionWolfOverrideModes = AiModeFlags.Attack
                                                                |   AiModeFlags.Wander
                                                                |   AiModeFlags.Feeding
                                                                |   AiModeFlags.HideAndSeek
                                                                | CompanionWolfAiModes;


        internal static CompanionWolfSettings Settings;

        protected CompanionWolfManager mSubManager;

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
            ProcessTimePassing(timePassed);
            LogDebug($"Initialized CompanionWolf with {mBaseAi.m_MaxHP}/{mBaseAi.m_MaxHP} condition and scale {mBaseAi.transform.localScale}; Tamed: {mSubManager.Data.Tamed}; Time passed since last load: {timePassed} seconds or {timePassed * Utility.SecondsToHours} hrs; CurrentTime: {currentTime} hrs");
        }


        public override void Despawn(float despawnTime)
        {
            mSubManager.Data.LastDespawnTime = despawnTime;
        }


        protected override bool PreprocesSetAiModeCustom(AiMode mode, out AiMode newMode)
        {
            if (mSubManager.Data.Tamed && mode.ToFlag().AnyOf(UntamedCompanionWolfRouteToFleeModes))
            {
                LogVerbose($"Untamed companions dont like to {mode}!");
                newMode = AiMode.Flee;
                return false;
            }
            if (!mSubManager.Data.Tamed && mode.ToFlag().AnyOf(UntamedCompanionWolfOverrideModes))
            {
                LogVerbose($"Untamed companion with mode {mode}, ignoring typical pre-processing in favor of CompanionWolf.");
                newMode = mode;
                return false;
            }
            if (mSubManager.Data.Tamed && mode.ToFlag().AnyOf(TamedCompanionWolfOverrideModes))
            {
                LogVerbose($"Tamed companion with mode {mode}, ignoring typical pre-processing in favor of CompanionWolf.");
                newMode = mode;
                return false;
            }
            newMode = mode;
            return true;
        }

        //runs in seconds!
        protected void ProcessTimePassing(float deltaTime)
        {
            if (!mSubManager.Data.Indoors && mSubManager.Data.Tamed)
            {
                mSubManager.Data.CurrentCalories -= deltaTime * Settings.CaloriesBurnedPerDay * Utility.SecondsToDays;
                if (mSubManager.Data.CurrentCalories < 0.0f)
                {
                    mSubManager.Data.CurrentCalories = 0.0f;
                    mSubManager.Data.CurrentCondition -= deltaTime * Settings.StarvingConditionDecayPerHour * Utility.SecondsToHours;
                    mBaseAi.m_CurrentHP = mSubManager.Data.CurrentCondition;
                }
            }
            if (mSubManager.Data.Indoors || !mSubManager.Data.Tamed)
            {
                mSubManager.Data.CurrentAffection -= deltaTime * (mSubManager.Data.Tamed ? Settings.TamedAffectionDecayRate : Settings.UntamedAffectionDecayRate) * Utility.SecondsToHours;
            }
        }


        protected override bool PostProcessCustom()
        {
            ProcessTimePassing(Time.deltaTime);
            if (!mSubManager.Data.Tamed && mSubManager.Data.UntamedTimeoutTime <= Utility.GetCurrentTimelinePoint())
            {
                //turn into "disconnect" method
                LogDebug("Disappear! go away! until next time :-(");
                mSubManager.Data.Disconnect();
                Despawn(Utility.GetCurrentTimelinePoint());
                return false;
            }
            return true;
        }


        protected override bool EnterAiModeCustom(AiMode mode)
        {
            if (!mSubManager.Data.Tamed)
            {
                switch (mode)
                {
                    case AiMode.InvestigateFood:
                    case AiMode.InvestigateSmell:
                    case AiMode.HoldGround:
                    case (AiMode)CompanionWolfAiMode.BigCarry:
                    case (AiMode)CompanionWolfAiMode.Follow:
                    case (AiMode)CompanionWolfAiMode.Fetch:
                    case (AiMode)CompanionWolfAiMode.LAST_STATE_DONT_ADD_MORE_THAN_THIS:
                        return false;
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
                    case AiMode.Attack:
                    case AiMode.Wander:
                    case AiMode.Feeding:
                    case AiMode.HideAndSeek:
                    case AiMode.InvestigateFood:
                    case (AiMode)CompanionWolfAiMode.BigCarry:
                    case (AiMode)CompanionWolfAiMode.Follow:
                    case (AiMode)CompanionWolfAiMode.Fetch:
                    case (AiMode)CompanionWolfAiMode.LAST_STATE_DONT_ADD_MORE_THAN_THIS:
                        return false;
                }
            }
            else
            {
                switch (CurrentMode)
                {
                    case AiMode.InvestigateFood:
                    case (AiMode)CompanionWolfAiMode.BigCarry:
                    case (AiMode)CompanionWolfAiMode.Follow:
                    case (AiMode)CompanionWolfAiMode.Fetch:
                    case (AiMode)CompanionWolfAiMode.LAST_STATE_DONT_ADD_MORE_THAN_THIS:
                        return false;
                }
            }
            return true;
        }


        protected override bool ExitAiModeCustom(AiMode mode)
        {
            if (!mSubManager.Data.Tamed)
            {
                switch (mode)
                {
                    case (AiMode)CompanionWolfAiMode.BigCarry:
                    case (AiMode)CompanionWolfAiMode.Follow:
                    case (AiMode)CompanionWolfAiMode.Fetch:
                    case (AiMode)CompanionWolfAiMode.LAST_STATE_DONT_ADD_MORE_THAN_THIS:
                        return false;
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
                        return false;
                }
            }
            return true;
        }
    }
}
