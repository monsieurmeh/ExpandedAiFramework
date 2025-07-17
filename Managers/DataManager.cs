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
        private Dictionary<Type, SubDataManagerBase> mMapDataManagers = new Dictionary<Type, SubDataManagerBase>();

        private string mLastScene = string.Empty;
        private bool mProxyDataLoaded = false;
        private bool mSpawnRegionModDataProxiesUncached = false;
        private bool mSpawnModDataProxiesUncached = false;

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


        #region Map Data

        public bool TryGetMapDataManager<T>(out MapDataManager<T> mapDataManager) where T : MapData, new()
        {
            mapDataManager = null;
            if (!mMapDataManagers.TryGetValue(typeof(T), out SubDataManagerBase baseMapDataManager))
            {
                return false;
            }
            if (baseMapDataManager is not MapDataManager<T> matchedMapDataManager)
            {
                return false;
            }
            mapDataManager = matchedMapDataManager;
            return true;
        }


        #region HidingSpot

        public HidingSpotManager HidingSpotManager { get { return mHidingSpotManager; } }
        public Dictionary<Guid, HidingSpot> AvailableHidingSpots { get { return mHidingSpotManager.AvailableData; } }
        public Dictionary<string, List<HidingSpot>> HidingSpots { get { return mHidingSpotManager.Data; } }
        public void GetNearestHidingSpotAsync(Vector3 position, Action<HidingSpot> callback, int extraNearestCandidatesToMaybePickFrom = 0) => mHidingSpotManager.GetNearestMapDataAsync(position, callback, extraNearestCandidatesToMaybePickFrom);

        #endregion


        #region WanderPath

        public WanderPathManager WanderPathManager { get { return mWanderPathManager; } }
        public Dictionary<Guid, WanderPath> AvailableWanderPaths { get { return mWanderPathManager.AvailableData; } }
        public Dictionary<string, List<WanderPath>> WanderPaths { get { return mWanderPathManager.Data; } }
        public void GetNearestWanderPathAsync(Vector3 position, WanderPathTypes type, Action<WanderPath> callback, int extraNearestCandidatesToMaybePickFrom = 0) => mWanderPathManager.GetNearestMapDataAsync(position, callback, extraNearestCandidatesToMaybePickFrom, [type]);

        #endregion

        #endregion


        #region Proxy Management

        public bool TryGetActiveSpawnRegionModDataProxy(Guid guid, out SpawnRegionModDataProxy proxy, string scene = null) => mSpawnRegionModDataProxyManager.TryGetProxy(guid, out proxy, scene);


        public bool TryGetActiveSpawnModDataProxy(Guid guid, out SpawnModDataProxy proxy, string scene = null)
        {
            for (int i = 0, iMax = mSpawnModDataProxyManagers.Length; i < iMax; i++)
            {
                if (mSpawnModDataProxyManagers[i] == null)
                {
                    continue;
                }
                if (mSpawnModDataProxyManagers[i].TryGetProxy(guid, out proxy, scene))
                {
                    return true;
                }
            }
            proxy = null;
            return false;
        }


        public bool TryGetActiveSpawnModDataProxy(Guid guid, WildlifeMode mode, out SpawnModDataProxy proxy, string scene = null) => mSpawnModDataProxyManagers[(int)mode].TryGetProxy(guid, out proxy, scene);
        

        public List<Guid> GetQueuedSpawnModDataProxiesByParentGuid(Guid guid, WildlifeMode wildlifeMode)
        {
            return mSpawnModDataProxyManagers[(int)wildlifeMode].GetQueuedSpawnModDataProxiesByParentGuid(guid);
        }


        public bool ClaimAvailableSpawnModDataProxy(SpawnModDataProxy proxy)
        {
            proxy.Available = true;
            if ((int)proxy.WildlifeMode >= mSpawnModDataProxyManagers.Length)
            {
                LogError($"Invalid wildlife mode: {proxy.WildlifeMode}!");
                return false;
            }
            return mSpawnModDataProxyManagers[(int)proxy.WildlifeMode].ClaimAvailableSpawnModDataProxy(proxy);
        }


        public bool TryGetNextAvailableSpawnModDataProxy(Guid spawnRegionModDataProxyGuid, WildlifeMode wildlifeMode, bool requireForceSpawn, out SpawnModDataProxy proxy)
        {
            return mSpawnModDataProxyManagers[(int)wildlifeMode].TryGetNextAvailableSpawnModDataProxy(spawnRegionModDataProxyGuid, requireForceSpawn, out proxy);
        }


        public bool TryRegisterSpawnRegionModDataProxy(SpawnRegionModDataProxy proxy) => mSpawnRegionModDataProxyManager.TryRegisterProxy(proxy);


        public bool TryRegisterSpawnModDataProxy(SpawnModDataProxy proxy)
        {
            if ((int)proxy.WildlifeMode >= mSpawnModDataProxyManagers.Length)
            {
                LogError($"Invalid wildlife mode: {proxy.WildlifeMode}!");
                return false;
            }
            return mSpawnModDataProxyManagers[(int)proxy.WildlifeMode].TryRegisterProxy(proxy);
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
            mWanderPathManager = new WanderPathManager(this);
            mHidingSpotManager = new HidingSpotManager(this);
            mMapDataManagers.Add(typeof(WanderPath), mWanderPathManager);
            mMapDataManagers.Add(typeof(HidingSpot), mHidingSpotManager);
            mSpawnModDataProxyManagers[(int)WildlifeMode.Normal] = new SpawnModDataProxyManager(this, "NormalSpawnModDataProxies");
            mSpawnModDataProxyManagers[(int)WildlifeMode.Aurora] = new SpawnModDataProxyManager(this, "AuroraSpawnModDataProxies");
            mSpawnRegionModDataProxyManager = new SpawnRegionModDataProxyManager(this, "SpawnRegionModDataProxies");
            StartSubManagers();
            LoadMapData();
        }

        private void StartSubManagers()
        { 
            foreach(SubDataManagerBase mapDataManager in mMapDataManagers.Values)
            {
                mapDataManager.StartWorker();
            }
        }


        private void StopSubManagers()
        {
            foreach (SubDataManagerBase mapDataManager in mMapDataManagers.Values)
            {
                mapDataManager.StopWorker();
            }
        }


        public override void Shutdown()
        {
            ClearMapData();
            ClearDataCache();
            StopSubManagers();
            base.Shutdown();
        }


        public override void OnQuitToMainMenu()
        {
            ClearDataCache();
        }


        private void ClearDataCache()
        {
            LogAlways($"Clearing DataCache");
            mLastScene = string.Empty;
            for (int i = 0, iMax = mSpawnModDataProxyManagers.Length; i < iMax; i++)
            {
                if (mSpawnModDataProxyManagers[i] == null)
                {
                    continue;
                }
                mSpawnModDataProxyManagers[i].Clear();
            }
            mSpawnRegionModDataProxyManager.Clear();
        }


        public override void OnLoadScene(string sceneName)
        {
            base.OnLoadScene(sceneName);
            
            if (mLastScene == sceneName)
            {
                return;
            }

            mLastScene = sceneName; 

            for (int i = 0, iMax = mSpawnModDataProxyManagers.Length; i < iMax; i++)
            {
                if (mSpawnModDataProxyManagers[i] == null)
                {
                    continue;
                }
                mSpawnModDataProxyManagers[i].Refresh(mLastScene);
            }
            mSpawnRegionModDataProxyManager.Refresh(mLastScene);
            RefreshAvailableMapData(mManager.CurrentScene);
        }


        public override void OnLoadGame()
        {
            base.OnLoadGame();
            for (int i = 0, iMax = mSpawnModDataProxyManagers.Length; i < iMax; i++)
            {
                if (mSpawnModDataProxyManagers[i] == null)
                {
                    continue;
                }
                mSpawnModDataProxyManagers[i].Load();
            }
            mSpawnRegionModDataProxyManager.Load();
        }


        public override void OnSaveGame()
        {
            base.OnSaveGame();
            for (int i = 0, iMax = mSpawnModDataProxyManagers.Length; i < iMax; i++)
            {
                if (mSpawnModDataProxyManagers[i] == null)
                {
                    continue;
                }
                mSpawnModDataProxyManagers[i].Save();
            }
            mSpawnRegionModDataProxyManager.Save();
        }



        public void RefreshAvailableMapData(string sceneName)
        {
            LogVerbose($"Loading EAF map data for scene {sceneName}");
            foreach (SubDataManagerBase mapDataManager in mMapDataManagers.Values)
            {
                mapDataManager.RefreshData(sceneName);
            }
        }


        public void SaveMapData()
        {
            LogVerbose($"Saving");
            foreach (SubDataManagerBase mapDataManager in mMapDataManagers.Values)
            {
                mapDataManager.Save();
            }
        }


        public void LoadMapData()
        {
            LogVerbose($"Loading");
            foreach (SubDataManagerBase mapDataManager in mMapDataManagers.Values)
            {
                mapDataManager.Load();
            }
            WanderPathManager.LoadAdditional("EAF/ExpandedAiFramework.WanderPathsEXTRA.json");
        }


        public void ClearMapData()
        {
            LogVerbose($"Clearing");
            foreach (SubDataManagerBase mapDataManager in mMapDataManagers.Values)
            {
                mapDataManager.Clear();
            }
        }

        #endregion
    }
}
