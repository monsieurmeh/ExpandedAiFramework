using Il2Cpp;
using Il2CppRewired.Utils;
using Il2CppVoice;
using MelonLoader.TinyJSON;
using MelonLoader.Utils;
using ModData;
using UnityEngine;

namespace ExpandedAiFramework
{
    public sealed class DataManager : BaseSubManager
    {
        #region General        

        private HidingSpotManager mHidingSpotManager;
        private WanderPathManager mWanderPathManager;
        private SpawnRegionModDataProxyManager mSpawnRegionModDataProxyManager;
        private SpawnModDataProxyManager[] mSpawnModDataProxyManagers = new SpawnModDataProxyManager[(int)WildlifeMode.Aurora + 1];
        private Dictionary<Type, IMapDataManager> mMapDataManagers = new Dictionary<Type, IMapDataManager>();

        private string mLastScene = string.Empty;

#if DEV_BUILD
        private ModDataManager mModData = new ModDataManager(ModName, true);
#else
        private ModDataManager mModData = new ModDataManager(ModName, false);
#endif
       

        public ModDataManager ModData { get { return mModData; } }
        public DataManager(EAFManager manager, ISubManager[] subManagers) : base(manager, subManagers) { }

        #endregion


        #region Primary API

        public string LastValidGameplaySceneName { get { return mManager.CurrentScene; } }


        #region MapData

        public bool ScheduleMapDataRequest<T>(IRequest request) where T : IMapData, new()
        {
            if (!mMapDataManagers.TryGetValue(typeof(T), out IMapDataManager manager))
            {
                return false;
            }
            manager.ScheduleRequest(request);
            return true;
        }

        #endregion


        #region ModDataProxy

        public void ScheduleSpawnRegionModDataProxyRequest(IRequest request) => mSpawnRegionModDataProxyManager.ScheduleRequest(request);


        public bool ScheduleSpawnModDataProxyRequest(IRequest request, WildlifeMode wildlifeMode)
        {
            if (wildlifeMode < WildlifeMode.Normal || wildlifeMode > WildlifeMode.Aurora)
            {
                LogError($"Invalid WildlifeMode: {wildlifeMode}");
                return false;
            }
            mSpawnModDataProxyManagers[(int)wildlifeMode].ScheduleRequest(request);
            return true;
        }


        public bool SchedulePreQueueRequest(CustomSpawnRegion region, WildlifeMode mode, bool closeEnoughForPrespawning)
        {
            return ScheduleSpawnModDataProxyRequest(new PreQueueRequest(region, mode, closeEnoughForPrespawning), mode);
        }
        

        public bool ScheduleRegisterSpawnModDataProxyRequest(SpawnModDataProxy proxy, Action<SpawnModDataProxy, RequestResult> callback)
        {
            if (proxy.WildlifeMode < WildlifeMode.Normal || proxy.WildlifeMode > WildlifeMode.Aurora)
            {
                LogError($"Invalid WildlifeMode: {proxy.WildlifeMode}");
                return false;
            }
            SpawnModDataProxyManager proxyManager = mSpawnModDataProxyManagers[(int)proxy.WildlifeMode];
            proxyManager.ScheduleRequest(new RegisterDataRequest<SpawnModDataProxy>(proxy, proxyManager.DataLocation, callback, false));
            return true;

        }



        public bool ScheduleRegisterSpawnRegionModDataProxyRequest(SpawnRegionModDataProxy proxy, Action<SpawnRegionModDataProxy, RequestResult> callback, bool callbackIsThreadSafe)
        {
            mSpawnRegionModDataProxyManager.ScheduleRequest(new RegisterDataRequest<SpawnRegionModDataProxy>(proxy, mSpawnRegionModDataProxyManager.DataLocation, callback, callbackIsThreadSafe));
            return true;
        }


        public bool CanForceSpawn(WildlifeMode wildlifeMode)
        {
            if ((int)wildlifeMode >= mSpawnModDataProxyManagers.Length)
            {
                LogError($"Invalid wildlife mode: {wildlifeMode}!");
                return false;
            }
            return mSpawnModDataProxyManagers[(int)wildlifeMode].CanForceSpawn();
        }


        public void IncrementForceSpawnCount(WildlifeMode wildlifeMode)
        {
            if ((int)wildlifeMode >= mSpawnModDataProxyManagers.Length)
            {
                LogError($"Invalid wildlife mode: {wildlifeMode}!");
                return;
            }
            mSpawnModDataProxyManagers[(int)wildlifeMode].IncrementForceSpawnCount();
        }
     
        #endregion


        #endregion


        #region Execution flow

        public override void Initialize(EAFManager manager, ISubManager[] subManagers)
        {
            base.Initialize(manager, subManagers);
            mWanderPathManager = new WanderPathManager(this, mManager.DispatchManager);
            mHidingSpotManager = new HidingSpotManager(this, mManager.DispatchManager);
            mMapDataManagers.Add(typeof(WanderPath), mWanderPathManager);
            mMapDataManagers.Add(typeof(HidingSpot), mHidingSpotManager);
            mSpawnModDataProxyManagers[(int)WildlifeMode.Normal] = new SpawnModDataProxyManager(this, mManager.DispatchManager, "NormalSpawnModDataProxies");
            mSpawnModDataProxyManagers[(int)WildlifeMode.Aurora] = new SpawnModDataProxyManager(this, mManager.DispatchManager, "AuroraSpawnModDataProxies");
            mSpawnRegionModDataProxyManager = new SpawnRegionModDataProxyManager(this, mManager.DispatchManager, "SpawnRegionModDataProxies");
            StartSubManagers();
            LoadMapData();
        }

        private void StartSubManagers()
        { 
            foreach(ISubDataManager mapDataManager in mMapDataManagers.Values)
            {
                mapDataManager.StartWorker();
            }
            foreach (SpawnModDataProxyManager manager in mSpawnModDataProxyManagers)
            {
                manager.StartWorker();
            }
            mSpawnRegionModDataProxyManager.StartWorker();
        }


        private void StopSubManagers()
        {
            foreach (ISubDataManager mapDataManager in mMapDataManagers.Values)
            {
                mapDataManager.StopWorker();
            }
            foreach (SpawnModDataProxyManager manager in mSpawnModDataProxyManagers)
            {
                manager.StopWorker();
            }
            mSpawnRegionModDataProxyManager.StopWorker();
        }


        public override void Shutdown()
        {
            ClearWorkers();
            ClearMapData();
            ClearDataCache();
            StopSubManagers();
            base.Shutdown();
        }


        public override void OnQuitToMainMenu()
        {
            ClearWorkers();
            ClearDataCache();
        }

        
        private void ClearWorkers()
        {
            LogTrace($"Clearing Workers");
            for (int i = 0, iMax = mSpawnModDataProxyManagers.Length; i < iMax; i++)
            {
                if (mSpawnModDataProxyManagers[i] == null)
                {
                    continue;
                }
                mSpawnModDataProxyManagers[i].ClearRequests();
            }
            foreach (IMapDataManager mapDataManager in mMapDataManagers.Values)
            {
                if (mapDataManager == null)
                {
                    continue;
                }
                mapDataManager.ClearRequests();
            }
            mSpawnRegionModDataProxyManager.ClearRequests();
        }


        private void ClearDataCache()
        {
            LogTrace($"Clearing DataCache");
            mLastScene = string.Empty;
            for (int i = 0, iMax = mSpawnModDataProxyManagers.Length; i < iMax; i++)
            {
                if (mSpawnModDataProxyManagers[i] == null)
                {
                    continue;
                }
                mSpawnModDataProxyManagers[i].ScheduleClear();
            }
            mSpawnRegionModDataProxyManager.ScheduleClear();
        }


        public override void OnLoadScene(string sceneName)
        {
            base.OnLoadScene(sceneName);
            
            if (mLastScene == sceneName)
            {
                return;
            }

            mLastScene = sceneName;

            RefreshModDataProxies();
            RefreshAvailableMapData();
        }


        public override void OnLoadGame()
        {
            base.OnLoadGame();
            LoadModDataProxies();
        }


        public override void OnSaveGame()
        {
            base.OnSaveGame();
            SaveModDataProxies();
        }


        #region ModDataProxies


        private void RefreshModDataProxies()
        {
            for (int i = 0, iMax = mSpawnModDataProxyManagers.Length; i < iMax; i++)
            {
                if (mSpawnModDataProxyManagers[i] == null)
                {
                    continue;
                }
                mSpawnModDataProxyManagers[i].ScheduleRefresh(mLastScene);
            }
            mSpawnRegionModDataProxyManager.ScheduleRefresh(mLastScene);
        }

        
        private void LoadModDataProxies()
        {
            for (int i = 0, iMax = mSpawnModDataProxyManagers.Length; i < iMax; i++)
            {
                if (mSpawnModDataProxyManagers[i] == null)
                {
                    continue;
                }
                mSpawnModDataProxyManagers[i].ScheduleLoad();
            }
            mSpawnRegionModDataProxyManager.ScheduleLoad();
        }


        private void SaveModDataProxies()
        {
            for (int i = 0, iMax = mSpawnModDataProxyManagers.Length; i < iMax; i++)
            {
                if (mSpawnModDataProxyManagers[i] == null)
                {
                    continue;
                }
                mSpawnModDataProxyManagers[i].ScheduleSave();
            }
            mSpawnRegionModDataProxyManager.ScheduleSave();
        }

        #endregion


        #region MapData

        private void RefreshAvailableMapData()
        {
            LogVerbose($"Loading EAF map data for scene {mLastScene}");
            foreach (ISubDataManager mapDataManager in mMapDataManagers.Values)
            {
                mapDataManager.ScheduleRefresh(mLastScene);
            }
        }


        public void SaveMapData()
        {
            LogVerbose($"Saving MapData");
            foreach (ISubDataManager mapDataManager in mMapDataManagers.Values)
            {
                mapDataManager.ScheduleSave();
            }
        }


        public void LoadMapData()
        {
            LogVerbose($"Loading MapData");
            foreach (ISubDataManager mapDataManager in mMapDataManagers.Values)
            {
                mapDataManager.ScheduleLoad();
            }
        }


        public void ClearMapData()
        {
            LogVerbose($"Clearing MapData");
            foreach (ISubDataManager mapDataManager in mMapDataManagers.Values)
            {
                mapDataManager.ScheduleClear();
            }
        }

        #endregion

        #endregion
    }
}
