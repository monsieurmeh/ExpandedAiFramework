global using VanillaCougarManager = Il2CppTLD.AI.CougarManager;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Il2CppAK.Wwise;
using Il2CppTLD.AI;
using UnityEngine;

namespace ExpandedAiFramework
{
    public class CougarManager : BaseSubManager, ICougarManager
    {   
        protected List<CougarIntroCinematic> mIntroCinematics = new List<CougarIntroCinematic>();
        protected List<CougarTerritoryZoneTrigger> mTriggers = new List<CougarTerritoryZoneTrigger>();
        protected List<CougarIntroScene> mIntroScenes = new List<CougarIntroScene>();
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

        private void ClearLists()
        {
            mTriggers.Clear();
            mIntroCinematics.Clear();
            mIntroScenes.Clear();
        }

        public override void OnQuitToMainMenu()
        {
            base.OnQuitToMainMenu();
            ClearLists();
            mStartCalled = false;
        }

        public override void OnLoadScene(string sceneName)
        {
            base.OnLoadScene(sceneName);
            ClearLists();
        }

        public override void OnInitializedScene(string sceneName)
        {
            base.OnInitializedScene(sceneName);
            foreach (CougarTerritoryZoneTrigger trigger in GameObject.FindObjectsOfType<CougarTerritoryZoneTrigger>())
            {
                if (!mTriggers.Contains(trigger))
                {
                    LogDebug($"Found territory zone trigger: {trigger.name}", LogCategoryFlags.CougarManager);
                    mTriggers.Add(trigger);
                }
            }
            foreach (CougarIntroCinematic cinematic in GameObject.FindObjectsOfType<CougarIntroCinematic>())
            {
                if (!mIntroCinematics.Contains(cinematic))
                {
                    LogDebug($"Found intro cinematic: {cinematic.name}", LogCategoryFlags.CougarManager);
                    mIntroCinematics.Add(cinematic);
                }
            }
            foreach (CougarIntroScene scene in GameObject.FindObjectsOfType<CougarIntroScene>())
            {
                if (!mIntroScenes.Contains(scene))
                {
                    LogDebug($"Found intro scene: {scene.name}", LogCategoryFlags.CougarManager);
                    mIntroScenes.Add(scene);
                }
            }
        }

        public void OverrideStart()
        {
            if (mStartCalled) 
            {
                LogDebug($"Start already called, aborting", LogCategoryFlags.CougarManager);
                return;
            }
            LogDebug($"OverrideStart", LogCategoryFlags.CougarManager);
            if (ShouldAbortStart()) 
            {
                LogDebug($"Aborting...", LogCategoryFlags.CougarManager);
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
            LogDebug($"Started", LogCategoryFlags.CougarManager);
            
        }

        private bool ShouldAbortStart()
        {            
            if (!VanillaCougarManager.GetCougarSettings(true)) 
            {
                Error($"Could not get cougar settings");
                return true;
            }
            if (!VanillaCougarManager.s_EnableInNewGame)
            {
                Log($"Cougar disabled", LogCategoryFlags.CougarManager);
                return true;
            }
            if (VanillaCougarManager == null) 
            {
                Log("Null vanilla cougar manager");
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
                CheckStaticCounts(shouldReport);
            }
        }

        protected void UpdateInternal(VanillaCougarManager vanillaCougarManager, bool shouldReport)
        {
            MaybeDebugReset();
            MaybePlayStalkingAudioInside(vanillaCougarManager);
            var territory = vanillaCougarManager.MaybeGetCurrentTerritory();
            if (territory == null)
            {
                if (shouldReport)
                {
                    LogDebug($"No territory found", LogCategoryFlags.CougarManager);
                }
                return;
            }
            switch(territory.m_CougarState) 
            {
                case VanillaCougarManager.CougarState.Start:
                    LogDebug($"On Start", LogCategoryFlags.CougarManager);
                    vanillaCougarManager.OnStart(territory);
                    return;
                case VanillaCougarManager.CougarState.WaitingForArrival:
                    if (shouldReport)
                    {
                        LogDebug($"Waiting for Arrival with DaysPlayedNotPaused: {vanillaCougarManager.GetDaysPlayedNotPaused()}", LogCategoryFlags.CougarManager);
                    }
                    vanillaCougarManager.UpdateWaitingForArrival(territory, vanillaCougarManager.GetDaysPlayedNotPaused());
                    return;
                case VanillaCougarManager.CougarState.HasArrivedAfterTransition:
                    LogDebug($"Setting Cougar Arrived", LogCategoryFlags.CougarManager);
                    vanillaCougarManager.SetCougarArrived(territory);
                    return;
                case VanillaCougarManager.CougarState.HasArrived:                    
                    if (shouldReport)
                    {
                        LogDebug($"Has Arrived", LogCategoryFlags.CougarManager);
                    }
                    vanillaCougarManager.UpdateHasArrived(vanillaCougarManager.GetDaysPlayedNotPaused());
                    return;                    
                default:
                    if (shouldReport)
                    {
                        LogDebug($"No state to run", LogCategoryFlags.CougarManager);
                    }
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
                    LogDebug($"Null VanillaCougarManager!", LogCategoryFlags.CougarManager); 
                }
                return true;
            }
            if (!vanillaManager.IsEnabled)
            { 
                if (shouldReport) 
                {
                    LogDebug($"Vanilla Manager disabled!!", LogCategoryFlags.CougarManager); 
                }
                return true;
            }
            if (GameManager.m_IsPaused) 
            { 
                if (shouldReport) 
                {
                    LogDebug($"Game paused!", LogCategoryFlags.CougarManager); 
                }
                return true;
            }
            if (GameManager.s_IsGameplaySuspended)
            { 
                if (shouldReport) 
                {
                    LogDebug($"Gameplay suspended!", LogCategoryFlags.CougarManager); 
                }
                return true;
            }
            if (GameManager.s_ActiveIsMainMenu) 
            { 
                if (shouldReport) 
                {
                    LogDebug($"Main menu active!", LogCategoryFlags.CougarManager); 
                }
                return true;
            }
            if (SaveGameSystem.IsRestoreInProgress()) 
            { 
                if (shouldReport) 
                {
                    LogDebug($"Restore in progress!", LogCategoryFlags.CougarManager); 
                }
                return true;
            }
            PlayerStruggle playerStruggle = GameManager.GetPlayerStruggleComponent();
            if (playerStruggle == null || playerStruggle.InStruggle()) 
            { 
                if (shouldReport) 
                {
                    LogDebug($"Player struggle!", LogCategoryFlags.CougarManager); 
                }
                return true;
            }
            return false;
        }

        protected void MaybeDebugReset()
        {
            if (VanillaCougarManager.s_DebugWaitingForComponentRegistration)
            {
                Log($"Debug resetting cougar static counts", LogCategoryFlags.CougarManager);
                VanillaCougarManager.s_SceneTerritoryZonesCount = 0;
                VanillaCougarManager.s_CougarIntroCinematicsCount = 0;
                VanillaCougarManager.s_CougarIntroScenesCount = 0;
                VanillaCougarManager.s_DebugWaitingForComponentRegistration = false;
            }
        }

        protected void CheckStaticCounts(bool shouldReport) 
        {
            if (!AreStaticCountsCorrect(shouldReport))
            {
                LogDebug($"Static counts incorrect, ensuring triggers/scenes/cinematics are contained in static lists then re-recording counts", LogCategoryFlags.CougarManager);
                SetStaticCountsCorrect();
            }
        }

        protected bool AreStaticCountsCorrect(bool shouldReport)
        {
            if (shouldReport)
            {
                Log($"Checking static counts: \nTerritories: {mTriggers.Count} actual vs {VanillaCougarManager.s_SceneTerritoryZonesCount} expected\nCinematics: {mIntroCinematics.Count} actual vs {VanillaCougarManager.s_CougarIntroCinematicsCount} expected\nScenes: {mIntroScenes.Count} actual vs {VanillaCougarManager.s_CougarIntroScenesCount} expected", LogCategoryFlags.CougarManager);
            }
            return mTriggers.Count == VanillaCougarManager.s_SceneTerritoryZonesCount 
                && mIntroCinematics.Count == VanillaCougarManager.s_CougarIntroCinematicsCount 
                && mIntroScenes.Count == VanillaCougarManager.s_CougarIntroScenesCount;
        }
        protected void SetStaticCountsCorrect()
        {
            Log($"Seting static counts: \nTerritories: {mTriggers.Count}\nCinematics: {mIntroCinematics.Count}\nScenes: {mIntroScenes.Count}", LogCategoryFlags.CougarManager);

            VanillaCougarManager.s_SceneTerritoryZones.Clear();
            VanillaCougarManager.s_SceneTerritoryZonesCount = mTriggers.Count;
            foreach (CougarTerritoryZoneTrigger trigger in mTriggers)
            {
                VanillaCougarManager.s_SceneTerritoryZones.Add(trigger);
            }

            VanillaCougarManager.s_CougarIntroCinematics.Clear();
            VanillaCougarManager.s_CougarIntroCinematicsCount = mIntroCinematics.Count;
            foreach (CougarIntroCinematic cinematic in mIntroCinematics)
            {
                VanillaCougarManager.s_CougarIntroCinematics.Add(cinematic);
            }

            VanillaCougarManager.s_CougarIntroScenes.Clear();
            VanillaCougarManager.s_CougarIntroScenesCount = mIntroScenes.Count;
            foreach (CougarIntroScene scene in mIntroScenes)
            {
                VanillaCougarManager.s_CougarIntroScenes.Add(scene);
            }
        }

        protected virtual bool UpdateCustom() => true;
    }
}
