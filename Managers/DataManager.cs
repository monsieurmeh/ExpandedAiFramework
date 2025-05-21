using MelonLoader.TinyJSON;
using MelonLoader.Utils;
using ModData;
using UnityEngine;
using UnityEngine.AI;

namespace ExpandedAiFramework
{
    public class DataManager : BaseSubManager
    {
        private Dictionary<string, List<HidingSpot>> mHidingSpots = new Dictionary<string, List<HidingSpot>>();
        private Dictionary<string, List<WanderPath>> mWanderPaths = new Dictionary<string, List<WanderPath>>();
        private Dictionary<string, List<SpawnRegionModDataProxy>> mSpawnRegionModDataProxies = new Dictionary<string, List<SpawnRegionModDataProxy>>();
        private List<HidingSpot> mAvailableHidingSpots = new List<HidingSpot>();
        private List<WanderPath> mAvailableWanderPaths = new List<WanderPath>();
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

        public Dictionary<string, List<SpawnRegionModDataProxy>> SpawnRegionModDataProxies { get { return mSpawnRegionModDataProxies; } }


        public DataManager(EAFManager manager, ISubManager[] subManagers) : base(manager, subManagers) { }

        public override void Initialize(EAFManager manager, ISubManager[] subManagers)
        {
            base.Initialize(manager, subManagers);
            LoadMapData();
        }


        public override void Shutdown()
        {
            ClearMapData();
            base.Shutdown();
        }


        public override void OnInitializedScene()
        {
            RefreshAvailableMapData(mManager.CurrentScene);
        }


        public void RefreshAvailableMapData(string sceneName)
        {
            LogDebug($"Loading EAF map data for scene {sceneName}");
            mAvailableHidingSpots.Clear();
            mAvailableWanderPaths.Clear();
            if (HidingSpots.TryGetValue(sceneName, out List<HidingSpot> hidingSpots))
            {
                mAvailableHidingSpots.AddRange(hidingSpots);
            }
            for (int i = 0, iMax = mAvailableHidingSpots.Count; i < iMax; i++)
            {
                LogDebug($"Available Hiding spot {i}: {mAvailableHidingSpots[i]}");
            }
            if (WanderPaths.TryGetValue(sceneName, out List<WanderPath> wanderPaths))
            {
                mAvailableWanderPaths.AddRange(wanderPaths);
            }
            for (int i = 0, iMax = mAvailableWanderPaths.Count; i < iMax; i++)
            {
                LogDebug($"Available Wander Path {i}: {mAvailableWanderPaths[i]}");
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
            List<SpawnRegionModDataProxy> allSpawnRegionModDataProxies = new List<SpawnRegionModDataProxy>();
            foreach (string key in mSpawnRegionModDataProxies.Keys)
            {
                allSpawnRegionModDataProxies.AddRange(mSpawnRegionModDataProxies[key]);
            }
            string json = JSON.Dump(allPaths, EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints);
            mModData.Save(json, "SpawnRegionModDataProxies");
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
                        LogDebug($"Found {newSpot}, adding...");
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

            mSpawnRegionModDataProxies.Clear();
            string proxiesString = ModData.Load("SpawnRegionModDataProxies");
            if (proxiesString != null)
            {
                Variant proxiesVariant = JSON.Load(proxiesString);
                foreach (var pathJSON in proxiesVariant as ProxyArray)
                {
                    canAdd = true;
                    SpawnRegionModDataProxy newProxy = new SpawnRegionModDataProxy();
                    JSON.Populate(pathJSON, newProxy);
                    if (!mSpawnRegionModDataProxies.TryGetValue(newProxy.Scene, out List<SpawnRegionModDataProxy> proxies))
                    {
                        proxies = new List<SpawnRegionModDataProxy>();
                        mSpawnRegionModDataProxies.Add(newProxy.Scene, proxies);
                    }
                    for (int i = 0, iMax = proxies.Count; i < iMax; i++)
                    {
                        if (proxies[i] == newProxy)
                        {
                            LogWarning($"Can't add new proxy {newProxy} because it already exists!");
                            canAdd = false;
                        }
                    }
                    if (canAdd)
                    {
                        proxies.Add(newProxy);
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
    }
}
