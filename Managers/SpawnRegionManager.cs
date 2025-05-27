using Harmony;
using MelonLoader.TinyJSON;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2Cpp;


namespace ExpandedAiFramework
{
    public sealed class SpawnRegionManager : BaseSubManager
    {
        private Dictionary<int, ICustomSpawnRegion> mCustomSpawnRegions = new Dictionary<int, ICustomSpawnRegion>(); //interface for patch injections or hooking into system directly from spawn regions
        private DataManager mDataManager;
        private string mLastSceneName;
        private bool mInitializedScene = false;

        public SpawnRegionManager(EAFManager manager, ISubManager[] subManagers) : base(manager, subManagers) { }
        public Dictionary<int, ICustomSpawnRegion> CustomSpawnRegions { get { return mCustomSpawnRegions; } }


        public override void Initialize(EAFManager manager, ISubManager[] subManagers)
        {
            mDataManager = mManager.DataManager; //should be ok since data manager always loads first
        }


        public override void OnLoadScene()
        {
            base.OnLoadScene();
            CacheSpawnRegionModDataProxies();
            ClearCustomSpawnRegions();
            mInitializedScene = false;
        }


        public override void OnInitializedScene(string sceneName)
        {
            base.OnInitializedScene(sceneName);
            LogVerbose($"SpawnRegionManager initializing in scene {sceneName}");
            mLastSceneName = mManager.CurrentScene;
            if (!mInitializedScene)
            {
                mInitializedScene = true;
                mDataManager.UncacheSpawnRegionModDataProxies(mLastSceneName);
            }

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


        public void ClearCustomSpawnRegions()
        {
            foreach (ICustomSpawnRegion customSpawnRegion in mCustomSpawnRegions.Values)
            {
                TryRemoveCustomSpawnRegion(customSpawnRegion.SpawnRegion);
            }
            mCustomSpawnRegions.Clear();
        }


        public bool TryInterceptSpawn(BaseAi baseAi, SpawnRegion spawnRegion)
        {
            if (spawnRegion == null)
            {
                LogDebug("Null spawn region, can't intercept.");
                return false;
            }
            if (!mCustomSpawnRegions.TryGetValue(spawnRegion.GetHashCode(), out ICustomSpawnRegion customSpawnRegion))
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
            if (!mDataManager.TryGetNextAvailableSpawnModDataProxy(customSpawnRegion.ModDataProxy.Guid, out SpawnModDataProxy proxy))
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
            if (!mCustomSpawnRegions.TryGetValue(spawnRegion.GetHashCode(), out ICustomSpawnRegion customSpawnRegion))
            {
                return false;
            }
            customSpawnRegion.Despawn(GetCurrentTimelinePoint());
            //UnityEngine.Object.Destroy(customSpawnRegion.Self); won't be needed until (unless) we turn CustomBaseSpawnRegion into a ticking monobomb
            mCustomSpawnRegions.Remove(spawnRegion.GetHashCode());
            return true;
        }


        private bool TryInjectCustomSpawnRegion(SpawnRegion spawnRegion, out ICustomSpawnRegion customSpawnRegion)
        {
            customSpawnRegion = null;
            if (spawnRegion == null)
            {
                LogDebug("Null spawn region. cannot inject custom spawn region");
                return false;
            }
            if (mCustomSpawnRegions.TryGetValue(spawnRegion.GetHashCode(), out customSpawnRegion))
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
            customSpawnRegion = InjectCustomSpawnRegion(spawnRegion, wrapperGuid);
            return true;
        }


        private CustomBaseSpawnRegion InjectCustomSpawnRegion(SpawnRegion spawnRegion, Guid guid)
        {
            if (!mDataManager.TryGetUnmatchedSpawnRegionModDataProxy(guid, spawnRegion, out SpawnRegionModDataProxy matchedProxy))
            { 
                LogDebug($"No spawn region mod data proxy matched to spawn region with hashcode {spawnRegion.GetHashCode()} and guid {guid}. creating then wrapping");
                matchedProxy = new SpawnRegionModDataProxy(guid, mLastSceneName, spawnRegion);
            }
            CustomBaseSpawnRegion newSpawnRegionWrapper = new CustomBaseSpawnRegion(spawnRegion, matchedProxy, mTimeOfDay);
            mCustomSpawnRegions.Add(spawnRegion.GetHashCode(), newSpawnRegionWrapper);
            return newSpawnRegionWrapper;
        }


        private void CacheSpawnRegionModDataProxies()
        {
            LogDebug($"Caching spawn region mod data proxies to scene {mLastSceneName}!");
            List<SpawnRegionModDataProxy> cachedProxies = mDataManager.GetCachedSpawnRegionModDataProxies(mLastSceneName);
            cachedProxies.Clear();
            foreach (ICustomSpawnRegion customSpawnRegion in mCustomSpawnRegions.Values)
            {
                LogDebug($"Caching spawn region mod data proxies with guid {customSpawnRegion.ModDataProxy.Guid}");
                cachedProxies.Add(customSpawnRegion.ModDataProxy);
            }
        }


        public override void Shutdown()
        {
            //SaveSpawnRegionModDataProxies(); no! bad shutdown! do NOT serialize without an actual save request!
            ClearCustomSpawnRegions();
            base.Shutdown();
        }
    }
}
