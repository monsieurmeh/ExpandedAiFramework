global using VanillaPackManager = Il2Cpp.PackManager;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Il2CppAK.Wwise;
using Il2CppTLD.AI;
using UnityEngine;

namespace ExpandedAiFramework
{
    public class PackManager : BaseSubManager, IPackManager
    {
        private const long TickRate = 500000L; // 10,000 ticks per ms, 1,000ms per second = 10,000,000 factor
        private bool mStartCalled = false;
        private long mNextTick = 0;
        private PackSettings mPackSettings;
        private VanillaPackManager mVanillaManager;

        public PackManager(EAFManager manager) : base(manager) 
        { 
            PackManagerSettings settings = new PackManagerSettings(Path.Combine(DataFolderPath, $"{nameof(PackManager)}"));
            settings.AddToModSettings(ModName);
            settings.Reload();
        }

        public VanillaPackManager VanillaPackManager
        { 
            get 
            {
                if (mVanillaManager == null)
                {
                    mVanillaManager = GameManager.m_PackManager;
                }
                return mVanillaManager;
            }
        }
        
        public override void OnQuitToMainMenu()
        {
            base.OnQuitToMainMenu();
            mStartCalled = false;
        }

        public override void OnLoadScene(string sceneName)
        {
            base.OnLoadScene(sceneName);
        }

        public override void OnInitializedScene(string sceneName)
        {
            base.OnInitializedScene(sceneName);
        }

        public void OverrideStart()
        {
            if (mStartCalled) 
            {
                LogDebug($"Start already called, aborting", LogCategoryFlags.PackManager);
                return;
            }
            LogDebug($"OverrideStart", LogCategoryFlags.PackManager);
            Il2CppTLD.UI.PanelReference panelReference = Il2CppTLD.UI.PanelReference.Get<Panel_HUD>();
            if (panelReference.TryGetPanel<Panel_HUD>(out Panel_HUD panel))
            {
                panel.QuietlyResetTimberWolfCombatMusic();
            }
            VanillaPackManager.m_HoursPlayedAtStart = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
            VanillaPackManager.m_SystemEnabled = VanillaPackManager.ArePacksAllowed();
            mPackSettings = VanillaPackManager.GetPackSettings();
        }


        public override void UpdateFromManager() 
        {
            if (!UpdateCustom()) return; // give priority to a custom implementation if ever exists
            if (mNextTick > DateTime.Now.Ticks) return;
            mNextTick = DateTime.Now.Ticks + TickRate;
            if (!ShouldUpdate()) return;
            MaybeEnableAnimalsOnLoad();
			ResetGroupEventFlags();
			MaybeCleanupDeadPackAnimals(mPackSettings);
			VanillaPackManager.MaybeDisbandGroupOnTargetLost(mPackSettings);
			VanillaPackManager.MaybeFleeAndDisbandOnMoraleCheck();
			VanillaPackManager.MaybeFleeAndDisbandOnBearOrMooseCheck(mPackSettings);
			VanillaPackManager.MaybeForceMoveMembers(mPackSettings);
			VanillaPackManager.MaybeMoveToNewHoldGroundPosition(mPackSettings);
			VanillaPackManager.MaybeForceAttackInCombatRestrictedArea(mPackSettings);
			VanillaPackManager.MaybeKeepLonersWithinRadius();
			VanillaPackManager.MaybeFormGroupOnPlayerDetectionRange(mPackSettings);
			VanillaPackManager.MaybeUpdateInteriorAudio(mPackSettings);
        }


        private bool ShouldUpdate()
        {
            if (VanillaPackManager == null) return false;
            if (!VanillaPackManager.m_SystemEnabled) return false;
            if (GameManager.m_IsPaused) return false;
            if (GameManager.s_IsGameplaySuspended) return false;
            if (GameManager.ControlsLocked()) return false;
            return true;
        }

        private void MaybeEnableAnimalsOnLoad() 
        {
            if (!VanillaPackManager.m_EnablePacksOnLoad) return;
            foreach (PackGroup pack in VanillaPackManager.m_PackAnimalGroupByLeader.Values)
            {
                foreach (PackAnimal animal in pack.m_Members)
                {
                    EnablePackAnimal(animal);
                }
            }
            VanillaPackManager.m_EnablePacksOnLoad = false;
        }

        private void EnablePackAnimal(PackAnimal animal)
        {
            if (animal.isActiveAndEnabled) return;
            if (animal.transform.parent == null) return; 
            animal.gameObject.SetActive(value: true);
            if (animal.transform.parent.gameObject.activeSelf) return; 
            animal.transform.parent.gameObject.SetActive(value: true);
        }

        private void ResetGroupEventFlags()
        {
            foreach (PackGroup pack in VanillaPackManager.m_PackAnimalGroupByLeader.Values)
            {
                pack.m_GroupEventProcessed = false;
            }
        }

        private void MaybeCleanupDeadPackAnimals(PackSettings settings)
        {
            foreach (PackAnimal animal in GetPossiblyDeadPackAnimals(settings))
            {
                if (IsPackAnimalDead(animal, settings)) continue;
                VanillaPackManager.UnregisterPackAnimal(animal, true);
            }
        }


        private IEnumerable<PackAnimal> GetPossiblyDeadPackAnimals(PackSettings settings)
        {
            foreach (PackGroup packGroup in VanillaPackManager.m_PackAnimalGroupByLeader.Values)
            {
                foreach (PackAnimal animal in packGroup.m_Members)
                {
                    yield return animal;
                }
            }
        }


        private bool IsPackAnimalDead(PackAnimal animal, PackSettings settings) 
        {
            if (animal.m_BaseAi.GetAiMode() == AiMode.Dead) return true;
            if (animal.m_BaseAi.IsBleedingOut() && animal.m_BaseAi.GetBleedingOutMinutesRemaining() < settings.m_MinBleedOutTimeMinutes) return true;
            if (!animal.isActiveAndEnabled && animal.m_BaseAi.HasUpdated()) return true; // This one may need special EAF attention, m_BaseAi.HasUpdated() is unlikely to work properly...
            return false;
        }


        #region overrides
        protected virtual bool UpdateCustom() => true;

        #endregion
    }
}
