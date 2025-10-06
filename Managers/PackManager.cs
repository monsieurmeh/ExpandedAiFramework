global using VanillaPackManager = Il2Cpp.PackManager;
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
        protected bool mIsMenuScene = true; //start out as true
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
        }

        protected virtual bool UpdateCustom() => true;
    }
}
