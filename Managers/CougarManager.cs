global using VanillaCougarManager = Il2CppTLD.AI.CougarManager;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Il2CppTLD.AI;

namespace ExpandedAiFramework
{
    public class CougarManager : BaseSubManager, ICougarManager
    {
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


        public override void UpdateFromManager() 
        {
            if (!UpdateCustom())
            {
                return;
            }
            VanillaCougarManager vanillaCougarManager = VanillaCougarManager;
            if (EarlyExitUpdate(vanillaCougarManager))
            {
                return;
            }
            if (ValidUpdateCondition())
            {
                UpdateInternal(vanillaCougarManager);
            }
            else
            {
                CheckStaticCounts();
            }
        }

        protected void UpdateInternal(VanillaCougarManager vanillaCougarManager)
        {
            bool shouldReport = mDebugTicker + 10000000 <= DateTime.Now.Ticks ; // 10,000 ticks per ms, 1,000ms per second = 10,000,000
            if (shouldReport)
            {
                mDebugTicker = DateTime.Now.Ticks;
            }
            MaybeDebugReset();
            var territory = vanillaCougarManager.MaybeGetCurrentTerritory();
            if (territory == null)
            {
                if (shouldReport)
                {
                    LogDebug($"No territory found");
                }
                return;
            }
            switch(territory.m_CougarState) 
            {
                case VanillaCougarManager.CougarState.Start:
                    LogDebug($"On Start");
                    vanillaCougarManager.OnStart(territory);
                    return;
                case VanillaCougarManager.CougarState.WaitingForArrival:
                    if (shouldReport)
                    {
                        LogDebug($"Waiting for Arrival with DaysPlayedNotPaused: {vanillaCougarManager.GetDaysPlayedNotPaused()}");
                    }
                    vanillaCougarManager.UpdateWaitingForArrival(territory, vanillaCougarManager.GetDaysPlayedNotPaused());
                    return;
                case VanillaCougarManager.CougarState.HasArrivedAfterTransition:
                    LogDebug($"Setting Cougar Arrived");
                    vanillaCougarManager.SetCougarArrived(territory);
                    return;
                case VanillaCougarManager.CougarState.HasArrived:                    
                    if (shouldReport)
                    {
                        LogDebug($"Has Arrived");
                    }
                    vanillaCougarManager.UpdateHasArrived(vanillaCougarManager.GetDaysPlayedNotPaused());
                    return;                    
                default:
                    return;
            }
        }        

        protected bool ValidUpdateCondition()
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

        protected bool EarlyExitUpdate(VanillaCougarManager vanillaManager)
        {
            if (vanillaManager == null) return true;
            if (!vanillaManager.IsEnabled) return true;
            if (GameManager.m_IsPaused) return true;
            if (GameManager.s_IsGameplaySuspended) return true;
            if (GameManager.s_ActiveIsMainMenu) return true;
            if (SaveGameSystem.IsRestoreInProgress()) return true;
            if (GameManager.s_IsGameplaySuspended) return true;
            PlayerStruggle playerStruggle = GameManager.GetPlayerStruggleComponent();
            if (playerStruggle == null || playerStruggle.InStruggle()) return true;
            return false;
        }

        protected void MaybeDebugReset()
        {
            if (VanillaCougarManager.s_DebugWaitingForComponentRegistration)
            {
                LogDebug($"Debug resetting cougar static counts");
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
                LogDebug($"Static counts incorrect, re-recording.");
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
