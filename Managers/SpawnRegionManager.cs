using Harmony;
using MelonLoader.TinyJSON;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2Cpp;


namespace ExpandedAiFramework
{
    public sealed class SpawnRegionManager : BaseSubManager
    {
        private Dictionary<Guid, SpawnRegionModDataProxy> mUnmatchedSpawnRegionModDataProxies = new Dictionary<Guid, SpawnRegionModDataProxy>(); // holds unmatched proxies during init for easy matching
        private Dictionary<Guid, ICustomSpawnRegion> mCustomSpawnRegionsByGuid = new Dictionary<Guid, ICustomSpawnRegion>(); // provides easy map to spawn regions from their proxy (do we still need this?)
        private Dictionary<int, ICustomSpawnRegion> mCustomSpawnRegionsByHashCode = new Dictionary<int, ICustomSpawnRegion>(); //interface for patch injections or hooking into system directly from spawn regions
        private bool mInitializedScene = false;
        private string mLastSceneName;

        public SpawnRegionManager(EAFManager manager, ISubManager[] subManagers) : base(manager, subManagers) { }
        public Dictionary<int, ICustomSpawnRegion> CustomSpawnRegions { get { return mCustomSpawnRegionsByHashCode; } }



        public override void OnLoadScene()
        {
            base.OnLoadScene();
            SaveSpawnRegionModDataProxies();
            ClearCustomSpawnRegions();
            mInitializedScene = false;
        }


        public override void OnInitializedScene(string sceneName)
        {
            base.OnInitializedScene(sceneName);
            LogVerbose($"SpawnRegionManager initializing in scene {sceneName}");
            mLastSceneName = mManager.CurrentScene;
            InitializeSpawnRegionModDataProxies(sceneName, !mInitializedScene);
            mInitializedScene = true;
        }


        public void ClearCustomSpawnRegions()
        {
            foreach (ICustomSpawnRegion customSpawnRegion in mCustomSpawnRegionsByHashCode.Values)
            {
                TryRemoveCustomSpawnRegion(customSpawnRegion.SpawnRegion);
            }
            mCustomSpawnRegionsByHashCode.Clear();
            mCustomSpawnRegionsByGuid.Clear();
        }


        public bool TryInterceptSpawn(BaseAi baseAi, SpawnRegion spawnRegion)
        {
            if (spawnRegion == null)
            {
                LogDebug("Null spawn region, can't intercept.");
                return false;
            }
            if (!mCustomSpawnRegionsByHashCode.TryGetValue(spawnRegion.GetHashCode(), out ICustomSpawnRegion customSpawnRegion))
            {
                LogDebug($"SpawnRegion with hashcode {spawnRegion.GetHashCode()} was not pre-wrapped on scene load, wrapping...");
                //option a: try to inject
                //option b: fallback look for object guid and try to rematch there? maybe more consistent. we'll see

                //option a first
                if (!TryInjectCustomSpawnRegion(spawnRegion, out customSpawnRegion))
                {
                    LogDebug($"Failed to inject new custom region during fallback spawn interception. Might be time to try option b!");
                    return false;
                }
            }
            if (!mManager.AiManager.TryGetNextAvailableSpawnModDataProxy(customSpawnRegion.ModDataProxy.Guid, out SpawnModDataProxy proxy))
            {
                LogDebug($"no queued spawns in region with hash code {spawnRegion.GetHashCode()}, deferring to ai manager random logic");
                return mManager.AiManager.TryInjectRandomCustomAi(baseAi, spawnRegion);
            }

            if (!mManager.AiManager.TryInjectCustomAi(baseAi, proxy.VariantSpawnType, spawnRegion))
            {
                LogError($"Error in AiManager ai injection process while trying to respawn previously found ai variant in region with hash code {spawnRegion.GetHashCode()}!");
                return false;
            }
            LogDebug($"Successfully spawned a {proxy.VariantSpawnType.Name} where it first spawned in spawn region with hash code {spawnRegion.GetHashCode()}!");
            return true;
        }


        public bool TryRemoveCustomSpawnRegion(SpawnRegion spawnRegion)
        {
            if (spawnRegion == null)
            {
                return false;
            }
            if (!mCustomSpawnRegionsByHashCode.TryGetValue(spawnRegion.GetHashCode(), out ICustomSpawnRegion customSpawnRegion))
            {
                return false;
            }
            customSpawnRegion.Despawn(GetCurrentTimelinePoint());
            //UnityEngine.Object.Destroy(customSpawnRegion.Self); won't be needed until (unless) we turn CustomBaseSpawnRegion into a ticking monobomb
            mCustomSpawnRegionsByGuid.Remove(customSpawnRegion.ModDataProxy.Guid);
            mCustomSpawnRegionsByHashCode.Remove(spawnRegion.GetHashCode());
            return true;
        }


        private void InitializeSpawnRegionModDataProxies(string sceneName, bool firstPass)
        {
            if (firstPass)
            {
                mUnmatchedSpawnRegionModDataProxies.Clear();
                LogDebug($"Trying to load spawnregion mod data proxie from suffix {mLastSceneName}_SpawnRegionModDataProxies!");
                string proxiesString = mManager.LoadData($"{mLastSceneName}_SpawnRegionModDataProxies");
                if (proxiesString != null)
                {
                    LogDebug($"Loading spawnregion mod data proxies!");
                    Variant proxiesVariant = JSON.Load(proxiesString);
                    foreach (var pathJSON in proxiesVariant as ProxyArray)
                    {
                        SpawnRegionModDataProxy newProxy = new SpawnRegionModDataProxy();
                        JSON.Populate(pathJSON, newProxy);
                        mUnmatchedSpawnRegionModDataProxies.Add(newProxy.Guid, newProxy);
                    }
                }
                LogDebug($"Deserialized {mUnmatchedSpawnRegionModDataProxies.Count} region data proxies");
            }
            LogDebug($"{mUnmatchedSpawnRegionModDataProxies.Count} unmatched spawn region mod data proxies remaining in cache");

            /*
            Il2CppArrayBase<GameObject> rootGameObjects = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName).GetRootGameObjects();
            List<SpawnRegion> sceneSpawnRegions = new List<SpawnRegion>();
            for (int i = 0, iMax = rootGameObjects.Length; i < iMax; i++)
            {
                sceneSpawnRegions.AddRange(rootGameObjects[i].GetComponentsInChildren<SpawnRegion>());
            }
            */
            //INCREDIBLY frustrated with hinterland that I can't seem to load spawn regions by scene, some aren't being caught if I dont re-do them every time a new one is added >:(
            //smart cookie thought: Maybe we program spawn regions to try to hook into us on start instead? "Wow!"
            List<SpawnRegion> sceneSpawnRegions = GameObject.FindObjectsOfType<SpawnRegion>().ToList();
            if (sceneSpawnRegions.Count == 0)
            {
                LogDebug("No spawn regions, aborting.");
                return;
            }

            for (int i = 0, iMax = sceneSpawnRegions.Count; i < iMax; i++)
            {
                if (!TryInjectCustomSpawnRegion(sceneSpawnRegions[i], out ICustomSpawnRegion newSpawnRegionWrapper))
                {
                    continue;
                }
                // Try and fetch existing spawn mod data proxies for spawning
                if (sceneSpawnRegions[i].m_SpawnablePrefab == null)
                {
                    LogDebug($"Null spawnable prefab on spawn region with hashcode {sceneSpawnRegions[i].GetHashCode()}! This happens, spawn region will try to wrap itself during spawn intercept instead. This is just an optimized step!");
                    continue;
                }
                if (!sceneSpawnRegions[i].m_SpawnablePrefab.TryGetComponent<BaseAi>(out BaseAi spawnableAi))
                {
                    LogDebug($"Could not get base ai script from spawnable prefab on spawn region with hashcode {sceneSpawnRegions[i].GetHashCode()}!");
                    continue;
                }
                LogDebug($"Region with hashcode {sceneSpawnRegions[i].GetHashCode()} and region guid {newSpawnRegionWrapper.ModDataProxy.Guid} wrapped and registered.");
                //Had some pre-queuing behavior here, it really messed up development at the time. Delegating to "future nick" to implement... oneday...lol
            }
        }


        private bool TryInjectCustomSpawnRegion(SpawnRegion spawnRegion, out ICustomSpawnRegion customSpawnRegion)
        {
            customSpawnRegion = null;
            if (spawnRegion == null)
            {
                LogDebug("Null spawn region. cannot inject custom spawn region");
                return false;
            }
            if (mCustomSpawnRegionsByHashCode.TryGetValue(spawnRegion.GetHashCode(), out customSpawnRegion))
            {
                LogDebug($"Previously matched spawn region with hash code {spawnRegion.GetHashCode()} and guid {customSpawnRegion.ModDataProxy.Guid}, skipping.");
                return false;
            }
            if (!spawnRegion.TryGetComponent<ObjectGuid>(out ObjectGuid guid))
            {
                LogError($"Could not find ObjectGuid on spawn region with hashcode {spawnRegion.GetHashCode()}!");
                return false;
            }
            Guid wrapperGuid = new Guid(guid.PDID);
            InjectCustomSpawnRegion(spawnRegion, wrapperGuid);
            if (!mCustomSpawnRegionsByGuid.TryGetValue(wrapperGuid, out customSpawnRegion))
            {
                LogDebug($"Could not fetch newly created custom spawn region wrapper from dictionary! Error! Error!");
                return false;
            }
            return true;
        }


        private void InjectCustomSpawnRegion(SpawnRegion spawnRegion, Guid guid)
        {
            if (mUnmatchedSpawnRegionModDataProxies.TryGetValue(guid, out SpawnRegionModDataProxy matchedProxy))
            {
                LogDebug($"Matched against spawn region with hashcode {spawnRegion.GetHashCode()} and guid {guid}, uncaching and wrapping");
                //mSpawnRegionModDataProxies.Add(regionGuid, matchedProxy);
                mUnmatchedSpawnRegionModDataProxies.Remove(guid);
            }
            else
            {
                LogDebug($"No spawn region mod data proxy matched to spawn region with hashcode {spawnRegion.GetHashCode()} and guid {guid}. creating then wrapping");
                matchedProxy = new SpawnRegionModDataProxy(guid, mLastSceneName, spawnRegion);
                //mSpawnRegionModDataProxies.Add(regionGuid, matchedProxy);
            }
            CustomBaseSpawnRegion newSpawnRegionWrapper = new CustomBaseSpawnRegion(spawnRegion, matchedProxy, mTimeOfDay);
            mCustomSpawnRegionsByGuid.Add(guid, newSpawnRegionWrapper);
            mCustomSpawnRegionsByHashCode.Add(spawnRegion.GetHashCode(), newSpawnRegionWrapper);
        }


        public bool TryGetCustomSpawnRegionByGuid(Guid guid, out ICustomSpawnRegion customSpawnRegion)
        {
            return mCustomSpawnRegionsByGuid.TryGetValue(guid, out customSpawnRegion);
        }


        private void SaveSpawnRegionModDataProxies()
        {
            LogDebug($"Saving spawn region mod data proxies to suffix {mLastSceneName}_SpawnRegionModDataProxies!");
            List<SpawnRegionModDataProxy> saveList = new List<SpawnRegionModDataProxy>();
            foreach (ICustomSpawnRegion customSpawnRegion in mCustomSpawnRegionsByHashCode.Values)
            {
                LogDebug($"Adding spawn region mod data proxies with guid {customSpawnRegion.ModDataProxy.Guid}");
                saveList.Add(customSpawnRegion.ModDataProxy);
            }
            string json = JSON.Dump(saveList, EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints);
            mManager.SaveData(json, $"{mLastSceneName}_SpawnRegionModDataProxies");
            LogDebug($"Saved!");
        }


        public override void Shutdown()
        {
            //SaveSpawnRegionModDataProxies(); no! bad shutdown! do NOT serialize without an actual save request!
            ClearCustomSpawnRegions();
            base.Shutdown();
        }
    }
}
