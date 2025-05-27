using Il2Cpp;
using MelonLoader.TinyJSON;
using MelonLoader.Utils;
using ModData;
using System.Security.AccessControl;
using UnityEngine;
using UnityEngine.AI;

namespace ExpandedAiFramework
{
    public sealed class DataManager : BaseSubManager
    {
        #region General

        private Dictionary<string, List<HidingSpot>> mHidingSpots = new Dictionary<string, List<HidingSpot>>();
        private Dictionary<string, List<WanderPath>> mWanderPaths = new Dictionary<string, List<WanderPath>>(); 
        private Dictionary<Guid, SpawnModDataProxy> mActiveSpawnModDataProxies = new Dictionary<Guid, SpawnModDataProxy>(); //this originally lived inside the spawn region wrapper, but i kept needing to access it here so I stole it. Now it needs to go back some day!
        private Dictionary<Guid, List<Guid>> mQueuedSpawnModDataProxiesByParentGuid = new Dictionary<Guid, List<Guid>>();
        private Dictionary<Guid, SpawnRegionModDataProxy> mUnmatchedSpawnRegionModDataProxies = new Dictionary<Guid, SpawnRegionModDataProxy>(); // holds unmatched proxies during init for easy matching
        private Dictionary<string, List<SpawnRegionModDataProxy>> mSpawnRegionModDataProxyCache = new Dictionary<string, List<SpawnRegionModDataProxy>>();
        private Dictionary<string, List<SpawnModDataProxy>> mSpawnModDataProxyCache = new Dictionary<string, List<SpawnModDataProxy>>();
        private List<HidingSpot> mAvailableHidingSpots = new List<HidingSpot>();
        private List<WanderPath> mAvailableWanderPaths = new List<WanderPath>();
        private bool mMapDataInitialized = false;
        private bool mProxyDataLoaded = false;
        private bool mProxyDataUncached = false;
        private string mLastSceneName = string.Empty;
#if DEV_BUILD
        private ModDataManager mModData = new ModDataManager(ModName, true);
#else
        private ModDataManager mModData = new ModDataManager(ModName, false);
#endif

        public ModDataManager ModData { get { return mModData; } }
        public Dictionary<string, List<HidingSpot>> HidingSpots { get { return mHidingSpots; } }
        public Dictionary<string, List<WanderPath>> WanderPaths { get { return mWanderPaths; } }
        public List<HidingSpot> AvailableHidingSpots { get { return mAvailableHidingSpots; } }
        public List<WanderPath> AvailableWanderPaths { get { return mAvailableWanderPaths; } }


        public DataManager(EAFManager manager, ISubManager[] subManagers) : base(manager, subManagers) { }

        public override void Initialize(EAFManager manager, ISubManager[] subManagers)
        {
            base.Initialize(manager, subManagers);
            LoadMapData();
        }

        #endregion


        #region Execution flow

        public override void Shutdown()
        {
            ClearMapData();
            ClearDataCache();
            base.Shutdown();
        }


        public override void OnQuitToMainMenu()
        {
            LogTrace($"DataCache clearing!");
            ClearDataCache();
        }


        private void ClearDataCache()
        {
            mSpawnModDataProxyCache.Clear();
            mSpawnRegionModDataProxyCache.Clear();
            mActiveSpawnModDataProxies.Clear();
            mQueuedSpawnModDataProxiesByParentGuid.Clear();
            mUnmatchedSpawnRegionModDataProxies.Clear();
        }

        public override void OnLoadScene(string sceneName)
        {
            base.OnLoadScene(sceneName);
            CacheSpawnRegionModDataProxies();
            CacheSpawnModDataProxies(); 
            mMapDataInitialized = false;
            mProxyDataUncached = false;
        }


        public override void OnLoadGame()
        {
            base.OnLoadGame();
            mProxyDataLoaded = false;
        }


        public override void OnSaveGame()
        {
            base.OnSaveGame();
            CacheSpawnRegionModDataProxies();
            CacheSpawnModDataProxies();
            SaveSpawnRegionModDataProxies();
            SaveSpawnModDataProxies();
        }


        public override void OnInitializedScene(string sceneName)
        {
            mLastSceneName = mManager.CurrentScene;
            if (!mMapDataInitialized)
            {
                mMapDataInitialized = true;
                RefreshAvailableMapData(mManager.CurrentScene);
            }
            if (!mProxyDataLoaded && GameManager.m_ActiveScene != null && Utility.IsValidGameplayScene(GameManager.m_ActiveScene, out string parsedSceneName))
            {
                mProxyDataLoaded = true;
                LogTrace($"Loading SpawnRegionModProxyData and SpawnModProxyData library for current save...");
                LoadSpawnRegionModDataProxies();
                LoadSpawnModDataProxies();
            }
            if (!mProxyDataUncached)
            {
                mProxyDataUncached = true;
                UncacheSpawnRegionModDataProxies(mLastSceneName);
                UncacheSpawnModDataProxies(mLastSceneName);
            }
        }

        #endregion


        #region Map Data

        public void RefreshAvailableMapData(string sceneName)
        {
            LogVerbose($"Loading EAF map data for scene {sceneName}");
            mAvailableHidingSpots.Clear();
            mAvailableWanderPaths.Clear();
            if (HidingSpots.TryGetValue(sceneName, out List<HidingSpot> hidingSpots))
            {
                mAvailableHidingSpots.AddRange(hidingSpots);
            }
            for (int i = 0, iMax = mAvailableHidingSpots.Count; i < iMax; i++)
            {
                LogVerbose($"Available Hiding spot {i}: {mAvailableHidingSpots[i]}");
            }
            if (WanderPaths.TryGetValue(sceneName, out List<WanderPath> wanderPaths))
            {
                mAvailableWanderPaths.AddRange(wanderPaths);
            }
            for (int i = 0, iMax = mAvailableWanderPaths.Count; i < iMax; i++)
            {
                LogVerbose($"Available Wander Path {i}: {mAvailableWanderPaths[i]}");
            }
        }


        public void SaveMapData()
        {
            List<HidingSpot> allSpots = new List<HidingSpot>();
            foreach (string key in mHidingSpots.Keys)
            {
                allSpots.AddRange(mHidingSpots[key]);
            }
            File.WriteAllText(Path.Combine(MelonEnvironment.ModsDirectory, "ExpandedAiFramework.HidingSpots.json"), JSON.Dump(allSpots, EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints), System.Text.Encoding.UTF8);
            List<WanderPath> allPaths = new List<WanderPath>();
            foreach (string key in mWanderPaths.Keys)
            {
                allPaths.AddRange(mWanderPaths[key]);
            }
            File.WriteAllText(Path.Combine(MelonEnvironment.ModsDirectory, "ExpandedAiFramework.WanderPaths.json"), JSON.Dump(allPaths, EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints), System.Text.Encoding.UTF8);
        }


        public void LoadMapData()
        {
            mHidingSpots.Clear();
            bool canAdd;
            try
            {
                string hidingSpots = File.ReadAllText(Path.Combine(MelonEnvironment.ModsDirectory, "ExpandedAiFramework.HidingSpots.json"), System.Text.Encoding.UTF8);
                if (hidingSpots != null)
                {
                    Variant hidingSpotsVariant = JSON.Load(hidingSpots);
                    foreach (var spotJSON in hidingSpotsVariant as ProxyArray)
                    {
                        canAdd = true;
                        HidingSpot newSpot = new HidingSpot();
                        JSON.Populate(spotJSON, newSpot);
                        if (!mHidingSpots.TryGetValue(newSpot.Scene, out List<HidingSpot> sceneSpots))
                        {
                            sceneSpots = new List<HidingSpot>();
                            mHidingSpots.Add(newSpot.Scene, sceneSpots);
                        }
                        for (int i = 0, iMax = sceneSpots.Count; i < iMax; i++)
                        {
                            if (sceneSpots[i] == newSpot)
                            {
                                LogWarning($"Can't add hiding spot {newSpot.Name} at {newSpot.Position} because another hiding spot with the same name is already defined!");
                                canAdd = false;
                            }
                        }
                        if (canAdd)
                        {
                            LogVerbose($"Found {newSpot}, adding...");
                            sceneSpots.Add(newSpot);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogError($"Error during DataManager.LoadMapData.HidingSpots: {e}");
            }

            mWanderPaths.Clear(); 
            try
            {
                string wanderPaths = File.ReadAllText(Path.Combine(MelonEnvironment.ModsDirectory, "ExpandedAiFramework.WanderPaths.json"), System.Text.Encoding.UTF8);
                if (wanderPaths != null)
                {
                    Variant wanderPathsVariant = JSON.Load(wanderPaths);
                    foreach (var pathJSON in wanderPathsVariant as ProxyArray)
                    {
                        canAdd = true;
                        WanderPath newPath = new WanderPath();
                        JSON.Populate(pathJSON, newPath);
                        if (!mWanderPaths.TryGetValue(newPath.Scene, out List<WanderPath> scenePaths))
                        {
                            scenePaths = new List<WanderPath>();
                            mWanderPaths.Add(newPath.Scene, scenePaths);
                        }
                        for (int i = 0, iMax = scenePaths.Count; i < iMax; i++)
                        {
                            if (scenePaths[i] == newPath)
                            {
                                LogWarning($"Can't add hiding spot {newPath} because another hiding spot with the same name is already defined!");
                                canAdd = false;
                            }
                        }
                        if (canAdd)
                        {
                            scenePaths.Add(newPath);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogError($"Error during DataManager.LoadMapData.HidingSpots: {e}");
            }
        }


        public void ClearMapData()
        {
            mHidingSpots.Clear();
            mWanderPaths.Clear();
        }


        public HidingSpot GetNearestHidingSpot(CustomBaseAi ai, int extraNearestCandidatesToMaybePickFrom = 0, bool requireAbleToPathfind = false)
        {
            Vector3 spawnPosition = ai.BaseAi.transform.position;
            int pickIndex = -1;
            if (mAvailableHidingSpots.Count > 1)
            {
                mAvailableHidingSpots.Sort((a, b) => SquaredDistance(spawnPosition, a.Position).CompareTo(SquaredDistance(spawnPosition, b.Position)));
                pickIndex = UnityEngine.Random.Range(0, Math.Min(mAvailableHidingSpots.Count - 1, extraNearestCandidatesToMaybePickFrom));
            }
            else if (mAvailableHidingSpots.Count == 1)
            {
                pickIndex = 0;
            }

            HidingSpot toReturn = null;
            if (pickIndex >= 0)
            {
                for (int i = 0, iMax = mAvailableHidingSpots.Count; i < iMax; i++)
                {
                    if (!requireAbleToPathfind || ai.BaseAi.CanPathfindToPosition(mAvailableHidingSpots[i].Position))
                    {
                        if (i == iMax || pickIndex <= 0)
                        {
                            toReturn = mAvailableHidingSpots[i];
                            break;
                        }
                        pickIndex--;
                    }
                }
                if (toReturn != null)
                {
                    LogVerbose($"{ai} picked {toReturn}.");
                    mAvailableHidingSpots.Remove(toReturn);
#if DEV_BUILD_LOCATIONMARKERS
                    mDebugShownHidingSpots.Add(CreateMarker(toReturn.Position, Color.yellow, $"Hiding spot for ai at {ai.BaseAi.transform.position}", 100));
#endif
                }
            }
            if (toReturn == null)
            {
                LogWarning($"Could not resolve a valid hiding spot for ai at {ai.BaseAi.transform.position}, expect auto generated spot..");
                while (toReturn == null)
                {
                    if (AiUtils.GetRandomPointOnNavmesh(out Vector3 validPos, ai.BaseAi.transform.position, 250.0f, 5.0f, NavMesh.AllAreas, false, 0.2f) && ai.BaseAi.CanPathfindToPosition(validPos, MoveAgent.PathRequirement.FullPath))
                    {
                        toReturn = new HidingSpot($"AutoGenerated for ai at {validPos}", validPos, Quaternion.LookRotation(new Vector3(UnityEngine.Random.Range(0f, 360f), 0f, 0f)), GameManager.m_ActiveScene);
#if DEV_BUILD_LOCATIONMARKERS
                        mDebugShownHidingSpots.Add(CreateMarker(validPos, Color.yellow, toReturn.Name, 100.0f));
#endif
                    }
                }
            }
            return toReturn;
        }


        public WanderPath GetNearestWanderPath(CustomBaseAi ai, int extraNearestCandidatesToMaybePickFrom = 0, bool requireAbleToPathfind = false)
        {
            Vector3 spawnPosition = ai.BaseAi.transform.position;
            int pickIndex = -1;
            if (mAvailableWanderPaths.Count > 1)
            {
                mAvailableWanderPaths.Sort((a, b) => SquaredDistance(spawnPosition, a.PathPoints[0]).CompareTo(SquaredDistance(spawnPosition, b.PathPoints[0])));
                pickIndex = UnityEngine.Random.Range(0, Math.Min(mAvailableWanderPaths.Count - 1, extraNearestCandidatesToMaybePickFrom));
            }
            else if (mAvailableWanderPaths.Count == 1)
            {
                pickIndex = 0;
            }
            WanderPath toReturn = null;
            if (pickIndex >= 0)
            {
                for (int i = 0, iMax = mAvailableWanderPaths.Count; i < iMax; i++)
                {
                    if (!requireAbleToPathfind || ai.BaseAi.CanPathfindToPosition(mAvailableWanderPaths[i].PathPoints[0]))
                    {
                        if (i == iMax || pickIndex <= 0)
                        {
                            toReturn = mAvailableWanderPaths[i];
                            break;
                        }
                        pickIndex--;
                    }
                }
                if (toReturn != null)
                {
                    mAvailableWanderPaths.Remove(toReturn);
                    LogVerbose($"{ai} picked {toReturn}.");
#if DEV_BUILD_LOCATIONMARKERS
                    for (int i = 0, iMax = toReturn.PathPoints.Length; i < iMax; i++)
                    {
                        mDebugShownWanderPaths.Add(CreateMarker(toReturn.PathPoints[i], Color.blue, $"WanderPath.PathPoint[{i}] for ai at {ai.BaseAi.transform.position}", 100));
                        if (i > 0)
                        {
                            mDebugShownWanderPaths.Add(ConnectMarkers(toReturn.PathPoints[i], toReturn.PathPoints[i - 1], Color.blue, $"WanderPath.PathPointConnector[{i - 1} -> {i}] for ai at {ai.BaseAi.transform.position}", 100));
                        }
                    }
#endif
                }
            }
            if (toReturn == null)
            {
                LogWarning($"Could not resolve a valid wander path for ai at {ai.BaseAi.transform.position}, expect auto-generated path...");
                int newNumWaypoints = UnityEngine.Random.Range(4, 8);
                Vector3[] pathPoints = new Vector3[newNumWaypoints];
                int failCount = 0;
                for (int i = 0, iMax = newNumWaypoints; i < iMax;)
                {
                    if (AiUtils.GetRandomPointOnNavmesh(out Vector3 validPos, ai.BaseAi.transform.position, Mathf.Max(50.0f - failCount, 10.0f), Mathf.Max(500.0f - failCount, 10.0f), -1, false, 0.2f) && ai.BaseAi.CanPathfindToPosition(validPos, MoveAgent.PathRequirement.FullPath))
                    {
                        pathPoints[i] = validPos;
#if DEV_BUILD_LOCATIONMARKERS
                        mDebugShownWanderPaths.Add(CreateMarker(validPos, Color.blue, $"AutoGenerated WanderPath Marker {i} for qi at {ai.BaseAi.transform.position}", 100));
                        if (i > 0)
                        {
                            mDebugShownWanderPaths.Add(ConnectMarkers(validPos, pathPoints[i - 1], Color.blue, $"AutoGenerated WanderPath Connector {i - 1} -> {i} for ai at {ai.BaseAi.transform.position}", 100));
                        }
#endif
                        i++;
                    }
                    else
                    {
                        failCount++;
                    }
                }
                toReturn = new WanderPath($"AutoGenerated WanderPath for ai at {ai.BaseAi.transform.position} with starting point of {pathPoints[0]}", pathPoints, GameManager.m_ActiveScene);
            }
            return toReturn;
        }

        #endregion


        #region SpawmRegionModDataProxy Management

        public void LoadSpawnRegionModDataProxies()
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
                LogTrace($"DataManager: Deserialized spawn region mod data proxy with guid {newProxy.Guid} in scene {newProxy.Scene}!");
                proxies.Add(newProxy);
            }
        }


        public void SaveSpawnRegionModDataProxies()
        {
            LogTrace($"Saving spawn region mod data proxies to suffix SpawnRegionModDataProxies!");
            List<SpawnRegionModDataProxy> masterProxyList = new List<SpawnRegionModDataProxy>();
            foreach (List<SpawnRegionModDataProxy> subProxyList in mSpawnRegionModDataProxyCache.Values)
            {
                masterProxyList.AddRange(subProxyList);
            }
            string json = JSON.Dump(masterProxyList, EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints);
            if (json == null || json == string.Empty)
            {
                return;
            }
            mModData.Save(json, $"SpawnRegionModDataProxies");
            LogTrace($"Saved!");
        }


        public List<SpawnRegionModDataProxy> GetCachedSpawnRegionModDataProxies(string sceneName)
        {
            if (!mSpawnRegionModDataProxyCache.TryGetValue(sceneName, out List<SpawnRegionModDataProxy> cachedProxies))
            {
                cachedProxies = new List<SpawnRegionModDataProxy>();
                mSpawnRegionModDataProxyCache.Add(sceneName, cachedProxies);
            }
            return cachedProxies;
        }


        private void CacheSpawnRegionModDataProxies()
        {
            if (mLastSceneName == string.Empty)
            {
                return;
            }
            LogTrace($"Caching spawn region mod data proxies to scene {mLastSceneName}!");
            List<SpawnRegionModDataProxy> cachedProxies = GetCachedSpawnRegionModDataProxies(mLastSceneName);
            cachedProxies.Clear();
            foreach (CustomBaseSpawnRegion customSpawnRegion in mManager.SpawnRegionManager.CustomSpawnRegions.Values)
            {
                if (customSpawnRegion == null)
                {
                    LogTrace($"Null CustomAi found??");
                    continue;
                }
                if (customSpawnRegion.ModDataProxy == null ||
                    customSpawnRegion.ModDataProxy.Guid == Guid.Empty)
                {
                    continue;
                }
                LogTrace($"Caching spawn region mod data proxies with guid {customSpawnRegion.ModDataProxy.Guid}");
                cachedProxies.Add(customSpawnRegion.ModDataProxy);
            }
        }


        public void UncacheSpawnRegionModDataProxies(string sceneName)
        {
            mUnmatchedSpawnRegionModDataProxies.Clear();
            LogTrace($"Uncaching spawn region mod data proxies from cache for scene {sceneName}!");
            List<SpawnRegionModDataProxy> cachedProxies = GetCachedSpawnRegionModDataProxies(sceneName);
            for (int i = 0, iMax = cachedProxies.Count; i < iMax; i++)
            {
                if (!mUnmatchedSpawnRegionModDataProxies.TryAdd(cachedProxies[i].Guid, cachedProxies[i]))
                {
                    LogTrace($"Even though we checked at cache load, we have a spawn region mod data proxy guid collision at {cachedProxies[i].Guid} during de-caching!");
                }
                LogTrace($"Uncached spawn region mod data proxy with guid {cachedProxies[i].Guid}!");
            }
            LogTrace($"Uncached {mUnmatchedSpawnRegionModDataProxies.Count} region data proxies");
        }


        public bool TryGetUnmatchedSpawnRegionModDataProxy(Guid guid, SpawnRegion spawnRegion, out SpawnRegionModDataProxy matchedProxy)
        {
            if (!mUnmatchedSpawnRegionModDataProxies.TryGetValue(guid, out matchedProxy))
            {
                LogTrace($"Couldnt find unmached spawn region mod data proxy with guid {guid}");
                return false;
            }
            LogTrace($"Matched against spawn region with hashcode {spawnRegion.GetHashCode()} and guid {guid}, uncaching and wrapping");
            mUnmatchedSpawnRegionModDataProxies.Remove(guid);
            return true;
        }

        #endregion


        #region SpawnModDataProxy Management

        //if we start getting too many more of these, im going to turn it into a damn generic library! lol
        public void LoadSpawnModDataProxies()
        {
            mSpawnModDataProxyCache.Clear();
            LogTrace($"Trying to load spawn  mod data proxies from suffix SpawnModDataProxies!");
            string proxiesString = mModData.Load($"SpawnModDataProxies");
            if (proxiesString != null)
            {
                Variant proxiesVariant = JSON.Load(proxiesString);
                foreach (var pathJSON in proxiesVariant as ProxyArray)
                {
                    SpawnModDataProxy newProxy = new SpawnModDataProxy();
                    JSON.Populate(pathJSON, newProxy);
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
                    LogTrace($"DataManager: Deserialized spawn region mod data proxy with guid {newProxy.Guid} in scene {newProxy.Scene}!");
                    proxies.Add(newProxy);
                }
            }
        }


        public void SaveSpawnModDataProxies()
        {
            LogTrace($"Saving spawn mod data proxies to suffix SpawnModDataProxies!");
            List<SpawnModDataProxy> masterProxyList = new List<SpawnModDataProxy>();
            foreach (List<SpawnModDataProxy> subProxyList in mSpawnModDataProxyCache.Values)
            {
                masterProxyList.AddRange(subProxyList);
            }
            string json = JSON.Dump(masterProxyList, EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints);
            if (json == null || json == string.Empty)
            {
                return;
            }
            mModData.Save(json, $"SpawnModDataProxies");
            LogTrace($"Saved!");
        }


        private void CacheSpawnModDataProxies()
        {
            if (mLastSceneName == string.Empty)
            {
                return;
            }
            LogTrace($"Caching spawn mod data proxies to scene {mLastSceneName}!");
            List<SpawnModDataProxy> cachedProxies = GetCachedSpawnModDataProxies(mLastSceneName);
            cachedProxies.Clear();
            foreach (CustomBaseAi customAi in mManager.AiManager.CustomAis.Values)
            {
                if (customAi == null)
                {
                    LogTrace($"Null CustomAi found??");
                    continue;
                }
                if (customAi.ModDataProxy == null ||
                    customAi.ModDataProxy.Guid == Guid.Empty ||
                    customAi.ModDataProxy.ParentGuid == Guid.Empty)
                {
                    continue;
                }
                LogTrace($"Caching spawn region mod data proxies with guid {customAi.ModDataProxy.Guid}");
                cachedProxies.Add(customAi.ModDataProxy);
            }
        }


        public void UncacheSpawnModDataProxies(string sceneName)
        {
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
        }


        public List<SpawnModDataProxy> GetCachedSpawnModDataProxies(string sceneName)
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


        public bool TryRegisterActiveSpawnModDataProxy(SpawnModDataProxy newProxy) => mActiveSpawnModDataProxies.TryAdd(newProxy.Guid, newProxy);

        #endregion
    }
}
