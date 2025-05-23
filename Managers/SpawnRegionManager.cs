using Harmony;
using MelonLoader.TinyJSON;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using static Il2Cpp.PlayerVoice;
using Il2Cpp;

namespace ExpandedAiFramework
{
    public sealed class SpawnRegionManager : BaseSubManager
    {
        private Dictionary<Guid, SpawnRegionModDataProxy> mSpawnRegionModDataProxies = new Dictionary<Guid, SpawnRegionModDataProxy>();
        private Dictionary<Guid, ICustomSpawnRegion> mCustomSpawnRegionsByGuid = new Dictionary<Guid, ICustomSpawnRegion>();
        private Dictionary<int, ICustomSpawnRegion> mCustomSpawnRegions = new Dictionary<int, ICustomSpawnRegion>();
        private bool mInitializedScene = false;
        private string mLastSceneName;

        public SpawnRegionManager(EAFManager manager, ISubManager[] subManagers) : base(manager, subManagers) { }
        public Dictionary<int, ICustomSpawnRegion> CustomSpawnRegions { get { return mCustomSpawnRegions; } }



        public override void OnLoadScene()
        {
            base.OnLoadScene();
            ClearCustomSpawnRegions();
            SaveSpawnRegionModDataProxies(); //because we're storing the spawn region mod data proxy in a separate dictionary, destroying the runtime construct should still leave everything behind here
            mInitializedScene = false;
        }


        public override void OnInitializedScene(string sceneName)
        {
            base.OnInitializedScene(sceneName);
            LogVerbose($"SpawnRegionManager initializing in scene {sceneName}");
            mLastSceneName = mManager.CurrentScene;
            InitializeSpawnRegionModDataProxies(!mInitializedScene);
            mInitializedScene = true;
        }


        public void ClearCustomSpawnRegions()
        {
            foreach (ICustomSpawnRegion customSpawnRegion in mCustomSpawnRegions.Values)
            {
                TryRemoveCustomSpawnRegion(customSpawnRegion.SpawnRegion);
            }
            mCustomSpawnRegions.Clear();
            mCustomSpawnRegionsByGuid.Clear();
        }


        public bool TryInterceptSpawn(BaseAi baseAi, SpawnRegion spawnRegion)
        {
            if (spawnRegion == null)
            {
                LogVerbose("Null spawn region, can't intercept.");
                return false;
            }

            if (!mCustomSpawnRegions.TryGetValue(spawnRegion.GetHashCode(), out ICustomSpawnRegion customSpawnRegion))
            {
                LogError("Trying to intercept spawn from region that is not wrapped, aborting!");
                return false;
            }

            if (!customSpawnRegion.TryGetQueuedSpawn(out SpawnModDataProxy proxy))
            {
                LogVerbose("no queued spawns, deferring to ai manager random logic");
                return mManager.AiManager.TryInjectRandomCustomAi(baseAi, spawnRegion);
            }

            if (!mManager.AiManager.TryInjectCustomAi(baseAi, proxy.VariantSpawnType, spawnRegion))
            {
                LogError("Error in AiManager ai injection process while trying to respawn previously found ai variant!");
                return false;
            }
            LogVerbose($"Successfully spawned a {proxy.VariantSpawnType} where it first spawned!");
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
            mCustomSpawnRegionsByGuid.Remove(customSpawnRegion.ModDataProxy.Guid);
            mCustomSpawnRegions.Remove(spawnRegion.GetHashCode());
            return true;
        }

        public bool TryGetSpawnRegionModDataProxy(Guid guid, out SpawnRegionModDataProxy proxy)
        {
            return mSpawnRegionModDataProxies.TryGetValue(guid, out proxy);
        }


        public bool TryGetCustomSpawnRegionByProxyGuid(Guid guid, out ICustomSpawnRegion customSpawnRegion)
        {
            return mCustomSpawnRegionsByGuid.TryGetValue(guid, out customSpawnRegion);
        }


        private void InitializeSpawnRegionModDataProxies(bool firstPass)
        {
            if (firstPass)
            {
                mSpawnRegionModDataProxies.Clear();
            }

            List<SpawnRegionModDataProxy> regionDataProxies = new List<SpawnRegionModDataProxy>();

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
                    regionDataProxies.Add(newProxy);
                }
            }

            LogDebug($"Deserialized {regionDataProxies.Count} region data proxies");
            Il2CppArrayBase<SpawnRegion> sceneSpawnRegions = Component.FindObjectsOfType<SpawnRegion>();
            bool matchFound = false;

            //Not a huge fan of the loop, but until I get spawnregions added to a quadtree or something this is the simplest method to match them up.
            //Will be done at scene init, hitches are kind of expected here. Im not worrying about it, since this will prevent hitches hopefully during spawning.
            for (int i = 0, iMax = sceneSpawnRegions.Count; i < iMax; i++)
            {
                if (mCustomSpawnRegions.ContainsKey(sceneSpawnRegions[i].GetHashCode()))
                {
                    continue;
                }
                for (int j = 0, jMax = regionDataProxies.Count; j < jMax; j++)
                {
                    //LogDebug($"Comparing data proxy {regionDataProxies[j]} with guid {regionDataProxies[j].Guid} against spawn region {sceneSpawnRegions[i]}...");
                    if (regionDataProxies[j].Guid == Guid.Empty)
                    {
                        LogError("Invalid guid in deserialized data proxies. what happened?");
                        continue;
                    }
                    if (sceneSpawnRegions[i].m_AiSubTypeSpawned != regionDataProxies[j].AiSubType)
                    {
                        LogDebug("AiSubtype mismatch");
                        continue;
                    }
                    if (SquaredDistance(sceneSpawnRegions[i].transform.position, regionDataProxies[j].OriginalPosition) > 1.0f)
                    {
                        LogDebug("proximity misimatch");
                        continue;
                    }
                    //todo: turn into "injectCustomSpawnRegion" method similar to aimanager
                    matchFound = true;
                    mCustomSpawnRegions.Add(sceneSpawnRegions[i].GetHashCode(), new CustomBaseSpawnRegion(sceneSpawnRegions[i], regionDataProxies[j], mTimeOfDay));
                    mCustomSpawnRegionsByGuid.Add(regionDataProxies[j].Guid, mCustomSpawnRegions[sceneSpawnRegions[i].GetHashCode()]);
                    mSpawnRegionModDataProxies.Add(regionDataProxies[j].Guid, regionDataProxies[j]);
                    LogDebug($"Registered custom spawn region using serialized region proxy data! lets gooooooooo");
                }
                if (!matchFound)
                {
                    SpawnRegionModDataProxy newProxy = new SpawnRegionModDataProxy(Guid.NewGuid(), mManager.CurrentScene, sceneSpawnRegions[i]); 
                    LogDebug($"No match found, generated new proxy with guid {newProxy.Guid}");
                    mCustomSpawnRegions.Add(sceneSpawnRegions[i].GetHashCode(), new CustomBaseSpawnRegion(sceneSpawnRegions[i], newProxy, mTimeOfDay)); 
                    mCustomSpawnRegionsByGuid.Add(newProxy.Guid, mCustomSpawnRegions[sceneSpawnRegions[i].GetHashCode()]);
                    mSpawnRegionModDataProxies.Add(newProxy.Guid, newProxy);
                }
                matchFound = false;
            }
        }


        private void SaveSpawnRegionModDataProxies()
        {
            string json = JSON.Dump(mSpawnRegionModDataProxies.Values.ToList(), EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints);
            mManager.SaveData(json, $"{mLastSceneName}_SpawnRegionModDataProxies");
        }


        public override void Shutdown()
        {
            //SaveSpawnRegionModDataProxies(); no! bad shutdown! do NOT serialize without an actual save request!
            ClearCustomSpawnRegions();
            base.Shutdown();
        }
    }
}
