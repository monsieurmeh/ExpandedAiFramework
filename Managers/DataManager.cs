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

        private Dictionary<Guid, SpawnModDataProxy> mActiveSpawnModDataProxies = new Dictionary<Guid, SpawnModDataProxy>(); 
        private Dictionary<Guid, SpawnRegionModDataProxy> mActiveSpawnRegionModDataProxies = new Dictionary<Guid, SpawnRegionModDataProxy>();
        private Dictionary<Guid, List<Guid>> mQueuedSpawnModDataProxiesByParentGuid = new Dictionary<Guid, List<Guid>>();
        private Dictionary<Guid, SpawnRegionModDataProxy> mUnmatchedSpawnRegionModDataProxies = new Dictionary<Guid, SpawnRegionModDataProxy>();
        private Dictionary<string, List<SpawnRegionModDataProxy>> mSpawnRegionModDataProxyCache = new Dictionary<string, List<SpawnRegionModDataProxy>>();
        private Dictionary<string, List<SpawnModDataProxy>> mSpawnModDataProxyCache = new Dictionary<string, List<SpawnModDataProxy>>();
        private Dictionary<Type, MapDataManagerBase> mMapDataManagers = new Dictionary<Type, MapDataManagerBase>();

        private bool mMapDataInitialized = false;
        private bool mProxyDataLoaded = false;
        private bool mSpawnRegionModDataProxiesUncached = false;
        private bool mSpawnModDataProxiesUncached = false;
        private string mLastSceneName = string.Empty;
#if DEV_BUILD
        private ModDataManager mModData = new ModDataManager(ModName, true);
#else
        private ModDataManager mModData = new ModDataManager(ModName, false);
#endif
       

        public ModDataManager ModData { get { return mModData; } }
        public DataManager(EAFManager manager, ISubManager[] subManagers) : base(manager, subManagers) { }

        #endregion


        #region Primary API
        public HidingSpotManager HidingSpotManager { get { return mHidingSpotManager; } }
        public Dictionary<Guid, HidingSpot> AvailableHidingSpots { get { return mHidingSpotManager.AvailableData; } }
        public Dictionary<string, List<HidingSpot>> HidingSpots { get { return mHidingSpotManager.Data; } }
        public WanderPathManager WanderPathManager { get { return mWanderPathManager; } }
        public Dictionary<Guid, WanderPath> AvailableWanderPaths { get { return mWanderPathManager.AvailableData; } }
        public Dictionary<string, List<WanderPath>> WanderPaths { get { return mWanderPathManager.Data; } }
        public bool TryRegisterActiveSpawnModDataProxy(SpawnModDataProxy newProxy) => mActiveSpawnModDataProxies.TryAdd(newProxy.Guid, newProxy);
        public bool TryRegisterActiveSpawnRegionModDataProxy(SpawnRegionModDataProxy newProxy) => mActiveSpawnRegionModDataProxies.TryAdd(newProxy.Guid, newProxy);
        public void GetNearestHidingSpotAsync(Vector3 position, Action<HidingSpot> callback, int extraNearestCandidatesToMaybePickFrom = 0) => mHidingSpotManager.GetNearestMapDataAsync(position, callback, extraNearestCandidatesToMaybePickFrom);
        public void GetNearestWanderPathAsync(Vector3 position, WanderPathTypes type, Action<WanderPath> callback, int extraNearestCandidatesToMaybePickFrom = 0) => mWanderPathManager.GetNearestMapDataAsync(position, callback, extraNearestCandidatesToMaybePickFrom, [type]);
        public bool TryGetMapDataManager<T>(out MapDataManager<T> mapDataManager) where T : MapData, new()
        {
            mapDataManager = null;
            if (!mMapDataManagers.TryGetValue(typeof(T), out MapDataManagerBase baseMapDataManager))
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
        #endregion


        #region Execution flow

        public override void Initialize(EAFManager manager, ISubManager[] subManagers)
        {
            base.Initialize(manager, subManagers);
            mWanderPathManager = new WanderPathManager(this);
            mHidingSpotManager = new HidingSpotManager(this);
            mMapDataManagers.Add(typeof(WanderPath), mWanderPathManager);
            mMapDataManagers.Add(typeof(HidingSpot), mHidingSpotManager);
            StartSubManagers();
            LoadMapData();
        }

        private void StartSubManagers()
        { 
            foreach(MapDataManagerBase mapDataManager in mMapDataManagers.Values)
            {
                mapDataManager.StartWorker();
            }
        }


        private void StopSubManagers()
        {
            foreach (MapDataManagerBase mapDataManager in mMapDataManagers.Values)
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
            LogTrace($"Clearing DataCache");
            mSpawnModDataProxyCache.Clear();
            mSpawnRegionModDataProxyCache.Clear();
            mActiveSpawnModDataProxies.Clear();
            mQueuedSpawnModDataProxiesByParentGuid.Clear();
            mActiveSpawnRegionModDataProxies.Clear(); 
            mUnmatchedSpawnRegionModDataProxies.Clear();
            mSpawnModDataProxiesUncached = false;
            mProxyDataLoaded = false;
            mSpawnRegionModDataProxiesUncached = false;
        }

        public override void OnLoadScene(string sceneName)
        {
            base.OnLoadScene(sceneName);
            CacheProxies(false); 
            mMapDataInitialized = false;
        }


        public override void OnLoadGame()
        {
            base.OnLoadGame(); 
            ClearDataCache();
        }


        public override void OnSaveGame()
        {
            base.OnSaveGame();
            CacheProxies(true); //probably move this to
            SaveProxies();
        }


        public override void OnInitializedScene(string sceneName)
        {
            mLastSceneName = mManager.CurrentScene;
            RefreshAvailableMapData(mLastSceneName);
        }


        public void SaveProxies()
        {
            SaveSpawnRegionModDataProxies();
            SaveSpawnModDataProxies();
        }


        public void LoadProxies()
        {
            if (!mProxyDataLoaded && GameManager.m_ActiveScene != null && Utility.IsValidGameplayScene(GameManager.m_ActiveScene, out string parsedSceneName))
            {
                mProxyDataLoaded = true;
                LogTrace($"Loading SpawnRegionModProxyData and SpawnModProxyData library for current save...");
                LoadSpawnRegionModDataProxies();
                LoadSpawnModDataProxies();
            }
        }


        public void CacheProxies(bool keepDataUncached)
        {
            CacheSpawnRegionModDataProxies(keepDataUncached);
            CacheSpawnModDataProxies(keepDataUncached);
        }

        
        public void UncacheProxies()
        {
            UncacheSpawnRegionModDataProxies(mLastSceneName);
            UncacheSpawnModDataProxies(mLastSceneName);
        }


        public void RefreshAvailableMapData(string sceneName)
        {
            if (!mMapDataInitialized)
            {

            }

            LogVerbose($"[{nameof(DataManager)}.{nameof(RefreshAvailableMapData)}] Loading EAF map data for scene {sceneName}");
            foreach (MapDataManagerBase mapDataManager in mMapDataManagers.Values)
            {
                mapDataManager.RefreshData(sceneName);
            }
        }


        public void SaveMapData()
        {
            LogVerbose($"[{nameof(DataManager)}.{nameof(SaveMapData)}] Saving");
            foreach (MapDataManagerBase mapDataManager in mMapDataManagers.Values)
            {
                mapDataManager.Save();
            }
        }


        public void LoadMapData()
        {
            LogVerbose($"[{nameof(DataManager)}.{nameof(LoadMapData)}] Loading");
            foreach (MapDataManagerBase mapDataManager in mMapDataManagers.Values)
            {
                mapDataManager.Load();
            }
        }


        public void ClearMapData()
        {
            LogVerbose($"[{nameof(DataManager)}.{nameof(ClearMapData)}] Clearing");
            foreach (MapDataManagerBase mapDataManager in mMapDataManagers.Values)
            {
                mapDataManager.Clear();
            }
        }

        #endregion


        #region SpawmRegionModDataProxy Management

        private void LoadSpawnRegionModDataProxies()
        {
            mSpawnRegionModDataProxyCache.Clear();
            LogTrace($"Trying to load spawn region mod data proxies from suffix SpawnRegionModDataProxies!");
            string proxiesString = mModData.Load($"SpawnRegionModDataProxies");
            if (proxiesString == null)
            {
                LogTrace($"Null proxy string, aborting!");
                return;
            }
            Variant proxiesVariant = JSON.Load(proxiesString);
            foreach (var pathJSON in proxiesVariant as ProxyArray)
            {
                SpawnRegionModDataProxy newProxy = new SpawnRegionModDataProxy();
                JSON.Populate(pathJSON, newProxy);
                List<SpawnRegionModDataProxy> proxies = GetCachedSpawnRegionModDataProxies(newProxy.Scene);
                if (proxies.Contains(newProxy))
                {
                    LogTrace($"Couldn't add new spawn region data proxy to data manager library with guid {newProxy.Guid} in scene {newProxy.Scene} because it already exists!");
                    continue;
                }
                LogTrace($"Deserialized spawn region mod data proxy with guid {newProxy.Guid} in scene {newProxy.Scene}!");
                proxies.Add(newProxy);
            }
        }


        private void SaveSpawnRegionModDataProxies()
        {
            if (!mProxyDataLoaded)
            {
                return;
            }
            LogTrace($"Saving spawn region mod data proxies to suffix SpawnRegionModDataProxies!");
            List<SpawnRegionModDataProxy> masterProxyList = new List<SpawnRegionModDataProxy>();
            foreach (List<SpawnRegionModDataProxy> subProxyList in mSpawnRegionModDataProxyCache.Values)
            {
                foreach (SpawnRegionModDataProxy proxy in subProxyList)
                {
                    LogTrace($"Serializing spawn region mod data proxy: {proxy}");
                }
                masterProxyList.AddRange(subProxyList);
            }
            string json = JSON.Dump(masterProxyList, EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints);
            if (json == null || json == string.Empty)
            {
                return;
            }
            mModData.Save(json, $"SpawnRegionModDataProxies");
            File.WriteAllText(Path.Combine(MelonEnvironment.ModsDirectory, $"SpawnRegionModDataProxies.json"), json);

            LogTrace($"Saved!");
        }


        private List<SpawnRegionModDataProxy> GetCachedSpawnRegionModDataProxies(string sceneName)
        {
            if (!mProxyDataLoaded)
            {
                return null;
            }
            if (!mSpawnRegionModDataProxyCache.TryGetValue(sceneName, out List<SpawnRegionModDataProxy> cachedProxies))
            {
                cachedProxies = new List<SpawnRegionModDataProxy>();
                mSpawnRegionModDataProxyCache.Add(sceneName, cachedProxies);
            }
            return cachedProxies;
        }

        //use "keepDataUncached" to prevent re-fetching of cached data overwriting future changes before next scene load. Best used with save features to capture changes since last load for serialization
        private void CacheSpawnRegionModDataProxies(bool keepDataUncached)
        {
            if (!mProxyDataLoaded)
            {
                return;
            }
            if (!mSpawnRegionModDataProxiesUncached)
            {
                return;
            }
            if (mLastSceneName == string.Empty)
            {
                return;
            }
            LogTrace($"Caching spawn region mod data proxies to scene {mLastSceneName}!");
            List<SpawnRegionModDataProxy> cachedProxies = GetCachedSpawnRegionModDataProxies(mLastSceneName);
            cachedProxies.Clear();
            foreach (SpawnRegionModDataProxy proxy in mActiveSpawnRegionModDataProxies.Values)
            {
                if (proxy.Guid == Guid.Empty)
                {
                    LogTrace($"SpawnRegionModDataProxy with empty guid found, discarding...?");
                    continue;
                }
                LogTrace($"Caching spawn region mod data proxy with guid {proxy.Guid}");
                cachedProxies.Add(proxy);
            }
            if (!keepDataUncached)
            {
                mSpawnRegionModDataProxiesUncached = false;
            }
            
            LogTrace($"Spawn region mod proxy data cached!");
        }


        private void UncacheSpawnRegionModDataProxies(string sceneName)
        {
            if (!mProxyDataLoaded)
            {
                return;
            }
            if (mSpawnRegionModDataProxiesUncached)
            {
                return;
            }
            mActiveSpawnRegionModDataProxies.Clear();
            mUnmatchedSpawnRegionModDataProxies.Clear();
            List<SpawnRegionModDataProxy> cachedProxies = GetCachedSpawnRegionModDataProxies(sceneName);
            LogTrace($"Uncaching spawn region mod data proxies from cache for scene {sceneName}! Before pre-check, we found {cachedProxies.Count} proxies!");
            for (int i = 0, iMax = cachedProxies.Count; i < iMax; i++)
            {
                if (!mUnmatchedSpawnRegionModDataProxies.TryAdd(cachedProxies[i].Guid, cachedProxies[i]))
                {
                    LogTrace($"Even though we checked at cache load, we have a spawn region mod data proxy guid collision at {cachedProxies[i].Guid} during de-caching!");
                }
                LogTrace($"Uncached spawn region mod data proxy with guid {cachedProxies[i].Guid}!");
            }
            LogTrace($"Uncached {mUnmatchedSpawnRegionModDataProxies.Count} region data proxies");
            mSpawnRegionModDataProxiesUncached = true;
        }


        public bool TryGetUnmatchedSpawnRegionModDataProxy(Guid guid, SpawnRegion spawnRegion, out SpawnRegionModDataProxy matchedProxy)
        {
            matchedProxy = null;
            if (!mProxyDataLoaded)
            {
                return false;
            }
            if (!mSpawnRegionModDataProxiesUncached)
            {
                return false;
            }
            if (!mUnmatchedSpawnRegionModDataProxies.TryGetValue(guid, out matchedProxy))
            {
                LogTrace($"Couldnt find unmached spawn region mod data proxy with guid {guid}");
                return false;
            }
            LogTrace($"Matched against spawn region with hashcode {spawnRegion.GetHashCode()} and guid {guid}, uncaching and wrapping");
            if (!mActiveSpawnRegionModDataProxies.TryAdd(guid, matchedProxy))
            {
                LogError($"Guid collision while trying to activate unmatched spawn region mod data proxy with guid {guid}!");
                return false;
            }
            mUnmatchedSpawnRegionModDataProxies.Remove(guid);
            return true;
        }

        #endregion


        #region SpawnModDataProxy Management

        //if we start getting too many more of these, im going to turn it into a damn generic library! lol
        private void LoadSpawnModDataProxies()
        {
            try
            {
                mSpawnModDataProxyCache.Clear();
                LogTrace($"Trying to load spawn mod data proxies from suffix SpawnModDataProxies!");
                string proxiesString = mModData.Load($"SpawnModDataProxies");
                if (proxiesString != null)
                {
                    Variant proxiesVariant = JSON.Load(proxiesString);
                    foreach (var pathJSON in proxiesVariant as ProxyArray)
                    {
                        SpawnModDataProxy newProxy = new SpawnModDataProxy();
                        JSON.Populate(pathJSON, newProxy);
                        if (pathJSON is ProxyObject proxyObject && proxyObject.TryGetValue("CustomData", out Variant item) && item is ProxyArray proxyArray)
                        {
                            newProxy.CustomData = new string[proxyArray.Count];
                            for (int i = 0, iMax = proxyArray.Count; i < iMax; i++)
                            {
                                newProxy.CustomData[i] = proxyArray[i];
                                LogTrace($"Extracted custom data: {newProxy.CustomData[i]}");
                            }
                        }
                        List<SpawnModDataProxy> proxies = GetCachedSpawnModDataProxies(newProxy.Scene);
                        if (proxies.Contains(newProxy))
                        {
                            LogError($"Couldn't add new spawn region data proxy to data manager library with guid {newProxy.Guid} in scene {newProxy.Scene} due to guid collision!");
                            continue;
                        }
                        if (!newProxy.InitializeType())
                        {
                            LogError($"Couldn't add new spawn region data proxy to data manager library with guid {newProxy.Guid} in scene {newProxy.Scene} because of type resolution error!");
                            continue;
                        }
                        LogTrace($"Deserialized spawn region mod data proxy with guid {newProxy.Guid} in scene {newProxy.Scene}!");
                        proxies.Add(newProxy);
                    }
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"{e}");
            }
        }


        private void SaveSpawnModDataProxies()
        {
            if (!mProxyDataLoaded)
            {
                return;
            }
            LogTrace($"Saving spawn mod data proxies to suffix SpawnModDataProxies!");
            List<SpawnModDataProxy> masterProxyList = new List<SpawnModDataProxy>();
            foreach (List<SpawnModDataProxy> subProxyList in mSpawnModDataProxyCache.Values)
            {
                foreach (SpawnModDataProxy proxy in subProxyList)
                {
                    LogTrace($"Serializing spawn mod data proxy: {proxy}");
                }
                masterProxyList.AddRange(subProxyList);
            }
            string json = JSON.Dump(masterProxyList, EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints);
            if (json == null || json == string.Empty)
            {
                return;
            }
            mModData.Save(json, $"SpawnModDataProxies");
            File.WriteAllText(Path.Combine(MelonEnvironment.ModsDirectory, $"SpawnModDataProxies.json"), json);
            LogTrace($"Saved!");
        }


        private void CacheSpawnModDataProxies(bool keepDataUncached)
        {
            if (!mProxyDataLoaded)
            {
                return;
            }
            if (!mSpawnModDataProxiesUncached)
            {
                return;
            }
            if (mLastSceneName == string.Empty)
            {
                return;
            }
            LogTrace($"Caching spawn mod data proxies to scene {mLastSceneName}!");
            List<SpawnModDataProxy> cachedProxies = GetCachedSpawnModDataProxies(mLastSceneName);
            cachedProxies.Clear();
            foreach (SpawnModDataProxy proxy in mActiveSpawnModDataProxies.Values)
            {

                if (proxy.Guid == Guid.Empty || proxy.ParentGuid == Guid.Empty)
                {
                    LogTrace($"Spawn mod data proxy with empty guid or empty parent guid found. discarding...");
                    continue;
                }
                if (proxy.Disconnected)
                {
                    LogTrace($"Disconnected spawn mod data proxy found, discarding..");
                    continue;
                }
                LogTrace($"Caching spawn mod data proxy with guid {proxy.Guid}");
                cachedProxies.Add(proxy);
            }
            if (!keepDataUncached)
            {
                mSpawnModDataProxiesUncached = false;
            }
            LogTrace($"Spawn mod proxy data cached!");
        }


        private void UncacheSpawnModDataProxies(string sceneName)
        {
            if (!mProxyDataLoaded)
            {
                return;
            }
            if (mSpawnModDataProxiesUncached)
            {
                return;
            }
            mActiveSpawnModDataProxies.Clear();
            mQueuedSpawnModDataProxiesByParentGuid.Clear();
            LogTrace($"Uncaching spawn mod data proxies for scene {sceneName}!");
            List<SpawnModDataProxy> cachedProxies = GetCachedSpawnModDataProxies(sceneName);
            for (int i = 0, iMax = cachedProxies.Count; i < iMax; i++)
            {
                if (cachedProxies[i].ParentGuid == Guid.Empty)
                {
                    LogError("Empty parent guid, cannot match with spawn region! ignoring.");
                    continue;
                }
                if (!mActiveSpawnModDataProxies.TryAdd(cachedProxies[i].Guid, cachedProxies[i]))
                {
                    LogTrace($"Even though we checked at cache load, we have a spawn mod data proxy guid collision at {cachedProxies[i].Guid} during de-caching!");
                    continue;
                }
                LogTrace($"Uncached spawn mod data proxy with guid {cachedProxies[i].Guid} which needs pairing against spawn region wrapper with guid {cachedProxies[i].ParentGuid}!");
                List<Guid> proxyGuidsByParentGuid = GetCachedSpawnModDataProxiesByParentGuid(cachedProxies[i].ParentGuid);
                if (proxyGuidsByParentGuid.Contains(cachedProxies[i].Guid))
                {
                    LogTrace($"Even though we checked at cache load, we have a spawn mod data proxy guid collision at {cachedProxies[i].Guid} within parent queued spawn list for parent guid {cachedProxies[i].ParentGuid}!");
                    continue;
                }
                proxyGuidsByParentGuid.Add(cachedProxies[i].Guid);
                LogTrace($"Queued spawn mod data proxy with guid {cachedProxies[i].Guid} in spawn list for parent guid {cachedProxies[i].ParentGuid}");
            }
            LogTrace($"Uncached {mActiveSpawnModDataProxies.Count} data proxies paired against {mQueuedSpawnModDataProxiesByParentGuid.Count} parent spawn regions!");
            mSpawnModDataProxiesUncached = true;
        }


        private List<SpawnModDataProxy> GetCachedSpawnModDataProxies(string sceneName)
        {
            if (!mSpawnModDataProxyCache.TryGetValue(sceneName, out List<SpawnModDataProxy> cachedProxies))
            {
                cachedProxies = new List<SpawnModDataProxy>();
                mSpawnModDataProxyCache.Add(sceneName, cachedProxies);
            }
            return cachedProxies;
        }


        public List<Guid> GetCachedSpawnModDataProxiesByParentGuid(Guid guid)
        {
            if (!mQueuedSpawnModDataProxiesByParentGuid.TryGetValue(guid, out List<Guid> proxyGuidsByParentGuid))
            {
                proxyGuidsByParentGuid = new List<Guid>();
                mQueuedSpawnModDataProxiesByParentGuid.Add(guid, proxyGuidsByParentGuid);
            }
            return proxyGuidsByParentGuid;
        }


        public bool TryGetNextAvailableSpawnModDataProxy(Guid spawnRegionModDataProxyGuid, out SpawnModDataProxy proxy)
        {
            proxy = null;
            if (!mQueuedSpawnModDataProxiesByParentGuid.TryGetValue(spawnRegionModDataProxyGuid, out List<Guid> availableProxies))
            {
                availableProxies = new List<Guid>();
                mQueuedSpawnModDataProxiesByParentGuid.Add(spawnRegionModDataProxyGuid, availableProxies);
            }
            if (availableProxies.Count == 0)
            {
                LogTrace($"No available proxies queued, generate a new one.");
                return false;
            }
            if (!mActiveSpawnModDataProxies.TryGetValue(availableProxies[0], out proxy))
            {
                LogError($"Couldnt match existing matched spawn mod data proxy guid {availableProxies[0]} to intended parent proxy guid {spawnRegionModDataProxyGuid}!");
                return false;
            }
            availableProxies.RemoveAt(0);
            return true;
        }

        #endregion
    }
}
