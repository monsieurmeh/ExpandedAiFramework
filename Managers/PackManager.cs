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
        protected bool mStartCalled = false;
        protected bool mIsMenuScene = true; 
        protected VanillaPackManager mVanillaManager;
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
        public PackManager(EAFManager manager) : base(manager) { }
        
        protected long mDebugTicker = 0;
        protected PackSettings mPackSettings;

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
            bool shouldReport = mDebugTicker + 10000000L <= DateTime.Now.Ticks ; // 10,000 ticks per ms, 1,000ms per second = 10,000,000
            if (shouldReport)
            {
                mDebugTicker = DateTime.Now.Ticks;
            }
            if (!ShouldUpdate()) return;
            VanillaPackManager.MaybeEnableAnimalsOnLoad();
			VanillaPackManager.ResetGroupEventFlags();
			VanillaPackManager.MaybeCleanupDeadPackAnimals(mPackSettings);
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
            if (!UpdateCustom()) return false;
            if (VanillaPackManager == null) return false;
            if (!VanillaPackManager.m_SystemEnabled) return false;
            if (GameManager.m_IsPaused) return false;
            if (GameManager.s_IsGameplaySuspended) return false;
            if (GameManager.ControlsLocked()) return false;
            return true;
        }

        protected virtual bool UpdateCustom() => true;
    }
}
