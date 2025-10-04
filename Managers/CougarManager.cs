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
            MaybeDebugReset();
            var territory = vanillaCougarManager.MaybeGetCurrentTerritory();
            if (territory == null)
            {
                return;
            }
            switch(territory.m_CougarState) 
            {
                case VanillaCougarManager.CougarState.Start:
                    vanillaCougarManager.OnStart(territory);
                    return;
                case VanillaCougarManager.CougarState.WaitingForArrival:
                    vanillaCougarManager.UpdateWaitingForArrival(territory, vanillaCougarManager.GetDaysPlayedNotPaused());
                    return;
                case VanillaCougarManager.CougarState.HasArrivedAfterTransition:
                    vanillaCougarManager.SetCougarArrived(territory);
                    return;
                case VanillaCougarManager.CougarState.HasArrived:
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
            if (!VanillaCougarManager.IsEnabled)
            {
                return true;
            }
            if (GameManager.m_IsPaused)
            {
                return true;
            }
            if (GameManager.s_IsGameplaySuspended)
            {
                return true;
            }
            if (GameManager.s_ActiveIsMainMenu)
            {
                return true;
            }
            if (SaveGameSystem.IsRestoreInProgress())
            {
                return true;
            }
            if (GameManager.s_IsGameplaySuspended)
            {
                return true;
            }
            PlayerStruggle playerStruggle = GameManager.GetPlayerStruggleComponent();
            if (playerStruggle == null || playerStruggle.InStruggle())
            {
                return true;
            }
            return false;
        }

        protected void MaybeDebugReset()
        {
            if (VanillaCougarManager.s_DebugWaitingForComponentRegistration)
            {
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
