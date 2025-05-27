using Il2Cpp;
using MelonLoader.TinyJSON;
using MelonLoader.Utils;
using ModData;
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
        private Dictionary<Guid, ICustomSpawnRegion> mCustomSpawnRegionsByGuid = new Dictionary<Guid, ICustomSpawnRegion>(); // provides easy map to spawn regions from their proxy (do we still need this?)
        private Dictionary<string, List<SpawnRegionModDataProxy>> mSpawnRegionModDataProxies = new Dictionary<string, List<SpawnRegionModDataProxy>>();
        private Dictionary<string, List<SpawnModDataProxy>> mSpawnModDataProxies = new Dictionary<string, List<SpawnModDataProxy>>();
        private List<HidingSpot> mAvailableHidingSpots = new List<HidingSpot>();
        private List<WanderPath> mAvailableWanderPaths = new List<WanderPath>();
        private bool mMapDataInitialized = false;
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
            base.Shutdown();
        }


        public override void OnLoadScene()
        {
            base.OnLoadScene();
            mMapDataInitialized = false;
        }


        public override void OnStartNewGame()
        {
            base.OnStartNewGame();
            LogDebug($"Loading SpawnRegionModProxyData and SpawnModProxyData library for current save...");
            LoadSpawnRegionModDataProxies();
            LoadSpawnModDataProxies();
        }


        public override void OnSaveGame()
        {
            base.OnSaveGame();
            SaveSpawnModDataProxies();
            SaveSpawnRegionModDataProxies();
        }


        public override void OnInitializedScene(string sceneName)
        {
            if (!mMapDataInitialized)
            {
                mMapDataInitialized = true;
                RefreshAvailableMapData(mManager.CurrentScene);
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

            mWanderPaths.Clear();
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


        public void ClearMapData()
        {
            mHidingSpots.Clear();
            mWanderPaths.Clear();
        }


        public HidingSpot GetNearestHidingSpot(ICustomAi ai, int extraNearestCandidatesToMaybePickFrom = 0, bool requireAbleToPathfind = false)
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


        public WanderPath GetNearestWanderPath(ICustomAi ai, int extraNearestCandidatesToMaybePickFrom = 0, bool requireAbleToPathfind = false)
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
            mSpawnRegionModDataProxies.Clear();
            LogDebug($"Trying to load spawn region mod data proxies from suffix SpawnRegionModDataProxies!");
            string proxiesString = mManager.LoadData($"SpawnRegionModDataProxies");
            if (proxiesString != null)
            {
                Variant proxiesVariant = JSON.Load(proxiesString);
                foreach (var pathJSON in proxiesVariant as ProxyArray)
                {
                    SpawnRegionModDataProxy newProxy = new SpawnRegionModDataProxy();
                    JSON.Populate(pathJSON, newProxy);
                    if (!mSpawnRegionModDataProxies.TryGetValue(newProxy.Scene, out List<SpawnRegionModDataProxy> proxies))
                    {
                        proxies = new List<SpawnRegionModDataProxy>();
                        mSpawnRegionModDataProxies.Add(newProxy.Scene, proxies);
                    }
                    if (proxies.Contains(newProxy))
                    {
                        LogDebug($"Couldn't add new spawn region data proxy to data manager library with guid {newProxy.Guid} in scene {newProxy.Scene} because it already exists!");
                        continue;
                    }
                    LogDebug($"DataManager: Deserialized spawn region mod data proxy with guid {newProxy.Guid} in scene {newProxy.Scene}!");
                    proxies.Add(newProxy);
                }
            }
        }


        public void SaveSpawnRegionModDataProxies()
        {
            LogDebug($"Saving spawn region mod data proxies to suffix SpawnRegionModDataProxies!");
            List<SpawnRegionModDataProxy> masterProxyList = new List<SpawnRegionModDataProxy>();
            foreach (List<SpawnRegionModDataProxy> subProxyList in mSpawnRegionModDataProxies.Values)
            {
                masterProxyList.AddRange(subProxyList);
            }
            string json = JSON.Dump(masterProxyList, EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints);
            if (json == null || json == string.Empty)
            {
                return;
            }
            mManager.SaveData(json, $"SpawnRegionModDataProxies");
            LogDebug($"Saved!");
        }


        public List<SpawnRegionModDataProxy> GetCachedSpawnRegionModDataProxies(string sceneName)
        {
            if (!mSpawnRegionModDataProxies.TryGetValue(sceneName, out List<SpawnRegionModDataProxy> cachedProxies))
            {
                cachedProxies = new List<SpawnRegionModDataProxy>();
                mSpawnRegionModDataProxies.Add(sceneName, cachedProxies);
            }
            return cachedProxies;
        }


        public void UncacheSpawnRegionModDataProxies(string sceneName)
        {
            mUnmatchedSpawnRegionModDataProxies.Clear();
            LogDebug($"Decaching spawn region mod data proxies from cache for scene {sceneName}!");
            List<SpawnRegionModDataProxy> cachedProxies = GetCachedSpawnRegionModDataProxies(sceneName);
            for (int i = 0, iMax = cachedProxies.Count; i < iMax; i++)
            {
                if (!mUnmatchedSpawnRegionModDataProxies.TryAdd(cachedProxies[i].Guid, cachedProxies[i]))
                {
                    LogDebug($"Even though we checked at cache load, we have a spawn region mod data proxy guid collision at {cachedProxies[i].Guid} during de-caching!");
                }
                LogDebug($"Decached spawn region mod data proxy with guid {cachedProxies.Count}!");
            }
            LogDebug($"Decached {mUnmatchedSpawnRegionModDataProxies.Count} region data proxies");
        }


        public bool TryGetUnmatchedSpawnRegionModDataProxy(Guid guid, SpawnRegion spawnRegion, out SpawnRegionModDataProxy matchedProxy)
        {
            if (!mUnmatchedSpawnRegionModDataProxies.TryGetValue(guid, out matchedProxy))
            {
                LogDebug($"Couldnt find unmached spawn region mod data proxy with guid {guid}");
                return false;
            }
            LogDebug($"Matched against spawn region with hashcode {spawnRegion.GetHashCode()} and guid {guid}, uncaching and wrapping");
            mUnmatchedSpawnRegionModDataProxies.Remove(guid);
            return true;
        }

        #endregion


        #region SpawnModDataProxy Management

        //if we start getting too many more of these, im going to turn it into a damn generic library! lol
        public void LoadSpawnModDataProxies()
        {
            mSpawnModDataProxies.Clear();
            LogDebug($"Trying to load spawn  mod data proxies from suffix SpawnModDataProxies!");
            string proxiesString = mManager.LoadData($"SpawnModDataProxies");
            if (proxiesString != null)
            {
                Variant proxiesVariant = JSON.Load(proxiesString);
                foreach (var pathJSON in proxiesVariant as ProxyArray)
                {
                    SpawnModDataProxy newProxy = new SpawnModDataProxy();
                    JSON.Populate(pathJSON, newProxy);
                    if (!mSpawnModDataProxies.TryGetValue(newProxy.Scene, out List<SpawnModDataProxy> proxies))
                    {
                        proxies = new List<SpawnModDataProxy>();
                        mSpawnModDataProxies.Add(newProxy.Scene, proxies);
                    }
                    if (proxies.Contains(newProxy))
                    {
                        LogDebug($"Couldn't add new spawn region data proxy to data manager library with guid {newProxy.Guid} in scene {newProxy.Scene} because it already exists!");
                        continue;
                    }
                    LogDebug($"DataManager: Deserialized spawn region mod data proxy with guid {newProxy.Guid} in scene {newProxy.Scene}!");
                    proxies.Add(newProxy);
                }
            }
        }


        public void SaveSpawnModDataProxies()
        {
            LogDebug($"Saving spawn mod data proxies to suffix SpawnModDataProxies!");
            List<SpawnModDataProxy> masterProxyList = new List<SpawnModDataProxy>();
            foreach (List<SpawnModDataProxy> subProxyList in mSpawnModDataProxies.Values)
            {
                masterProxyList.AddRange(subProxyList);
            }
            string json = JSON.Dump(masterProxyList, EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints);
            if (json == null || json == string.Empty)
            {
                return;
            }
            mManager.SaveData(json, $"SpawnModDataProxies");
            LogDebug($"Saved!");
        }


        public void UncacheSpawnModDataProxies(string sceneName)
        {
            mActiveSpawnModDataProxies.Clear();
            mQueuedSpawnModDataProxiesByParentGuid.Clear();
            LogDebug($"Pulling spawn mod data proxies from cache for scene {sceneName}!");
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
                    LogDebug($"Even though we checked at cache load, we have a spawn mod data proxy guid collision at {cachedProxies[i].Guid} during de-caching!");
                    continue;
                }
                LogDebug($"Decached spawn mod data proxy with guid {cachedProxies[i].Guid} which needs pairing against spawn region wrapper with guid {cachedProxies[i].ParentGuid}!");
                List<Guid> proxyGuidsByParentGuid = GetCachedSpawnModDataProxiesByParentGuid(cachedProxies[i].ParentGuid);
                if (proxyGuidsByParentGuid.Contains(cachedProxies[i].Guid))
                {
                    LogDebug($"Even though we checked at cache load, we have a spawn mod data proxy guid collision at {cachedProxies[i].Guid} within parent queued spawn list for parent guid {cachedProxies[i].ParentGuid}!");
                    continue;
                }
                proxyGuidsByParentGuid.Add(cachedProxies[i].Guid);
                LogDebug($"Queued spawn mod data proxy with guid {cachedProxies[i].Guid} in spawn list for parent guid {cachedProxies[i].ParentGuid}");
            }
            LogDebug($"Decached {mActiveSpawnModDataProxies.Count} data proxies paired against {mQueuedSpawnModDataProxiesByParentGuid.Count} parent spawn regions!");
        }


        public List<SpawnModDataProxy> GetCachedSpawnModDataProxies(string sceneName)
        {
            if (!mSpawnModDataProxies.TryGetValue(sceneName, out List<SpawnModDataProxy> cachedProxies))
            {
                cachedProxies = new List<SpawnModDataProxy>();
                mSpawnModDataProxies.Add(sceneName, cachedProxies);
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
                LogDebug($"No available proxies queued, generate a new one.");
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
