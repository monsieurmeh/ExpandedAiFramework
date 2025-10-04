global using VanillaCougarManager = Il2CppTLD.AI.CougarManager;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Il2CppTLD.AI;

namespace ExpandedAiFramework
{
    public class CougarManager : BaseSubManager, ICougarManager
    {
        protected bool mStartCalled = false;
        protected bool mIsMenuScene = true; //start out as true
        protected VanillaCougarManager mVanillaManager;
        public VanillaCougarManager VanillaCougarManager
        { 
            get 
            {
                if (mVanillaManager == null)
                {
                    mVanillaManager = GameManager.m_CougarManager;
                }
                return mVanillaManager;
            }
        }
        public CougarManager(EAFManager manager) : base(manager) { }
        public virtual Type SpawnType  => typeof(BaseCougar);
        public virtual bool ShouldInterceptSpawn(CustomSpawnRegion region) => false;
        public virtual void PostProcessNewSpawnModDataProxy(SpawnModDataProxy proxy) { }

        protected long mDebugTicker = 0;

        public override void OnQuitToMainMenu()
        {
            base.OnQuitToMainMenu();
            mStartCalled = false;
        }

        public void OverrideStart()
        {
            if (mStartCalled) 
            {
                LogDebug($"Start already called, aborting");
                return;
            }
            LogDebug($"OverrideStart");
            if (ShouldAbortStart()) 
            {
                LogDebug($"Aborting...");
                if (mVanillaManager != null)
                {
                    mVanillaManager.IsEnabled = false;
                }
                return;
            }
            mStartCalled = true;
            VanillaCougarManager.s_CougarSettingsOverride = false;
            mVanillaManager.m_CurrentThreatLevelByRegion.Clear();
            mVanillaManager.m_CurrentThreatCooldownByRegion.Clear();
            mVanillaManager.IsEnabled = true;
            mVanillaManager.m_Cougar_TerritoryZone_EnterID = 0;
            mVanillaManager.m_Cougar_TerritoryZone_ExitID = 0;
            mVanillaManager.m_Cougar_NearbyOutsideID = 0;
            VanillaCougarManager.SetAudioState(mVanillaManager.m_CougarTerritory_ZoneThreatLevel_ctztl_0);
            LogDebug($"Started");
        }

        private bool ShouldAbortStart()
        {            
            if (!VanillaCougarManager.GetCougarSettings(true)) 
            {
                LogError($"Could not get cougar settings");
                return true;
            }
            if (!VanillaCougarManager.s_EnableInNewGame)
            {
                LogTrace($"Cougar disabled");
                return true;
            }
            if (mVanillaManager == null) 
            {
                LogTrace("Null vanilla cougar manager");
                return true;
            }
            return false;
        }

        public override void UpdateFromManager() 
        {
            bool shouldReport = mDebugTicker + 10000000L <= DateTime.Now.Ticks ; // 10,000 ticks per ms, 1,000ms per second = 10,000,000
            if (shouldReport)
            {
                mDebugTicker = DateTime.Now.Ticks;
            }
            if (!UpdateCustom())
            {
                return;
            }
            VanillaCougarManager vanillaCougarManager = VanillaCougarManager;
            if (EarlyExitUpdate(vanillaCougarManager, shouldReport))
            {
                return;
            }
            if (ValidUpdateCondition(shouldReport))
            {
                UpdateInternal(vanillaCougarManager, shouldReport);
            }
            else
            {
                CheckStaticCounts();
            }
        }

        protected void UpdateInternal(VanillaCougarManager vanillaCougarManager, bool shouldReport)
        {
            MaybeDebugReset();
            var territory = vanillaCougarManager.MaybeGetCurrentTerritory();
            if (territory == null)
            {
                if (shouldReport)
                {
                    LogTrace($"No territory found");
                }
                return;
            }
            switch(territory.m_CougarState) 
            {
                case VanillaCougarManager.CougarState.Start:
                    LogTrace($"On Start");
                    vanillaCougarManager.OnStart(territory);
                    return;
                case VanillaCougarManager.CougarState.WaitingForArrival:
                    if (shouldReport)
                    {
                        LogTrace($"Waiting for Arrival with DaysPlayedNotPaused: {vanillaCougarManager.GetDaysPlayedNotPaused()}");
                    }
                    vanillaCougarManager.UpdateWaitingForArrival(territory, vanillaCougarManager.GetDaysPlayedNotPaused());
                    return;
                case VanillaCougarManager.CougarState.HasArrivedAfterTransition:
                    LogTrace($"Setting Cougar Arrived");
                    vanillaCougarManager.SetCougarArrived(territory);
                    return;
                case VanillaCougarManager.CougarState.HasArrived:                    
                    if (shouldReport)
                    {
                        LogTrace($"Has Arrived");
                    }
                    vanillaCougarManager.UpdateHasArrived(vanillaCougarManager.GetDaysPlayedNotPaused());
                    return;                    
                default:
                    return;
            }
        }        

        protected bool ValidUpdateCondition(bool shouldReport)
        {
            return VanillaCougarManager.s_SceneTerritoryZones.Count != 0 
                && VanillaCougarManager.s_CougarIntroCinematics.Count != 0 
                && VanillaCougarManager.s_CougarIntroScenes.Count != 0;
        }

        protected void MaybePlayStalkingAudioInside(VanillaCougarManager vanillaManager)
        {
            Weather weather = GameManager.GetWeatherComponent();
            if (weather != null && weather.IsIndoorEnvironment())
            {
                vanillaManager.MaybePlayStalkingAudio();
            }
        }

        protected bool EarlyExitUpdate(VanillaCougarManager vanillaManager, bool shouldReport)
        {
            if (vanillaManager == null) 
            { 
                if (shouldReport) 
                {
                    LogTrace($"Null VanillaCougarManager!"); 
                }
                return true;
            }
            if (!vanillaManager.IsEnabled)
            { 
                if (shouldReport) 
                {
                    LogTrace($"Vanilla Manager disabled!!"); 
                }
                return true;
            }
            if (GameManager.m_IsPaused) 
            { 
                if (shouldReport) 
                {
                    LogTrace($"Game paused!"); 
                }
                return true;
            }
            if (GameManager.s_IsGameplaySuspended)
            { 
                if (shouldReport) 
                {
                    LogTrace($"Gameplay suspended!"); 
                }
                return true;
            }
            if (GameManager.s_ActiveIsMainMenu) 
            { 
                if (shouldReport) 
                {
                    LogTrace($"Main menu active!"); 
                }
                return true;
            }
            if (SaveGameSystem.IsRestoreInProgress()) 
            { 
                if (shouldReport) 
                {
                    LogTrace($"Restore in progress!"); 
                }
                return true;
            }
            PlayerStruggle playerStruggle = GameManager.GetPlayerStruggleComponent();
            if (playerStruggle == null || playerStruggle.InStruggle()) 
            { 
                if (shouldReport) 
                {
                    LogTrace($"Player struggle!"); 
                }
                return true;
            }
            return false;
        }

        protected void MaybeDebugReset()
        {
            if (VanillaCougarManager.s_DebugWaitingForComponentRegistration)
            {
                LogTrace($"Debug resetting cougar static counts");
                VanillaCougarManager.s_SceneTerritoryZonesCount = 0;
                VanillaCougarManager.s_CougarIntroCinematicsCount = 0;
                VanillaCougarManager.s_CougarIntroScenesCount = 0;
                VanillaCougarManager.s_DebugWaitingForComponentRegistration = false;
            }
        }

        protected void CheckStaticCounts() 
        {
            if (!AreStaticCountsCorrect())
            {
                LogTrace($"Static counts incorrect, re-recording.");
                SetStaticCountsCorrect();
            }
        }

        protected bool AreStaticCountsCorrect()
        {
            return VanillaCougarManager.s_SceneTerritoryZones.Count == VanillaCougarManager.s_SceneTerritoryZonesCount 
                && VanillaCougarManager.s_CougarIntroCinematics.Count == VanillaCougarManager.s_CougarIntroCinematicsCount 
                && VanillaCougarManager.s_CougarIntroScenes.Count == VanillaCougarManager.s_CougarIntroScenesCount;
        }

        protected void SetStaticCountsCorrect()
        {
            VanillaCougarManager.s_SceneTerritoryZonesCount = VanillaCougarManager.s_SceneTerritoryZones.Count;
            VanillaCougarManager.s_CougarIntroCinematicsCount = VanillaCougarManager.s_CougarIntroCinematics.Count;
            VanillaCougarManager.s_CougarIntroScenesCount = VanillaCougarManager.s_CougarIntroScenes.Count;
        }

        protected virtual bool UpdateCustom() => true;
    }
}
