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

            Il2CppArrayBase<SpawnRegion> sceneSpawnRegions = Component.FindObjectsOfType<SpawnRegion>();
            if (sceneSpawnRegions.Length == 0)
            {
                LogDebug("No spawn regions, aborting.");
                return;
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
            //there, this should help reduce the complexity over time dramatically. and avoid dumbass dictionary collisions! we can always generate a new proxy if needed. Its possible 1.0f is too large a distance? surprised, but who knows. ill have to do a survey in game at some point.
            HashSet<SpawnRegionModDataProxy> guidSet = new HashSet<SpawnRegionModDataProxy>();
            for (int i = regionDataProxies.Count - 1, iMin = 0; i >= iMin; i--)
            {
                if (mSpawnRegionModDataProxies.ContainsKey(regionDataProxies[i].Guid) || mCustomSpawnRegionsByGuid.ContainsKey(regionDataProxies[i].Guid))
                {
                    LogDebug($"found previously assigned spawn region mod data proxy {regionDataProxies[i].Guid} at {regionDataProxies[i].OriginalPosition} of ai type {regionDataProxies[i].AiSubType}, discarding.");
                    regionDataProxies.RemoveAt(i);
                    continue;
                }
                if (!guidSet.Add(regionDataProxies[i]))
                {
                    LogDebug("found duplicate guid region proxy, discarding. probably from a bug or previous version of the mod.");
                    regionDataProxies.RemoveAt(i);
                }
                LogDebug($"Retained spawn region mod data proxy {regionDataProxies[i].Guid} at {regionDataProxies[i].OriginalPosition} of ai type {regionDataProxies[i].AiSubType}");
            }

            LogDebug($"Retained {regionDataProxies.Count} region data proxies");

            bool matchFound = false;
            //Not a huge fan of the loop, but until I get spawnregions added to a quadtree or something this is the simplest method to match them up.
            //Will be done at scene init, hitches are kind of expected here. Im not worrying about it, since this will prevent hitches hopefully during spawning.
            for (int i = 0, iMax = sceneSpawnRegions.Count; i < iMax; i++)
            {
                if (mCustomSpawnRegions.ContainsKey(sceneSpawnRegions[i].GetHashCode()))
                {
                    continue;
                }
                for (int j = regionDataProxies.Count - 1, jMin = 0; j >= jMin; j--)
                {
                    //LogDebug($"Comparing data proxy {regionDataProxies[j]} with guid {regionDataProxies[j].Guid} against spawn region {sceneSpawnRegions[i]}...");
                    if (regionDataProxies[j].Guid == Guid.Empty)
                    {
                        LogError("Invalid guid in deserialized data proxies. what happened?");
                        continue;
                    }
                    if (sceneSpawnRegions[i].m_AiSubTypeSpawned != regionDataProxies[j].AiSubType)
                    {
                        //LogDebug("AiSubtype mismatch");
                        continue;
                    }
                    if (SquaredDistance(sceneSpawnRegions[i].transform.position, regionDataProxies[j].OriginalPosition) > 1.0f)
                    {
                        //LogDebug("proximity misimatch");
                        continue;
                    }
                    if (mCustomSpawnRegionsByGuid.ContainsKey(regionDataProxies[j].Guid) || mSpawnRegionModDataProxies.ContainsKey(regionDataProxies[j].Guid))
                    {
                        LogDebug($"Somehow {regionDataProxies[j].Guid} has entries in spawn regions by guid and/or spawnregionmoddataproxies, when it should have been previously removed. Skipping...");
                        continue;
                    }
                    matchFound = true;
                    if (mCustomSpawnRegions.ContainsKey(sceneSpawnRegions[i].GetHashCode()))
                    {
                        LogDebug($"REALLY not sure why but somehow mCustomSpawnRegions now contains a hashcode {sceneSpawnRegions[i].GetHashCode()} when it didn't before entering this loop. Skipping this spawn region immediately before corruption occurs!");
                        break;
                    }
                    mCustomSpawnRegions.Add(sceneSpawnRegions[i].GetHashCode(), new CustomBaseSpawnRegion(sceneSpawnRegions[i], regionDataProxies[j], mTimeOfDay));
                    mCustomSpawnRegionsByGuid.Add(regionDataProxies[j].Guid, mCustomSpawnRegions[sceneSpawnRegions[i].GetHashCode()]);
                    mSpawnRegionModDataProxies.Add(regionDataProxies[j].Guid, regionDataProxies[j]);
                    LogDebug($"Registered custom spawn region {sceneSpawnRegions[i]} using serialized region proxy {regionDataProxies[j].Guid}!");
                    regionDataProxies.RemoveAt(j);
                }
                if (!matchFound)
                {
                    if (mCustomSpawnRegions.ContainsKey(sceneSpawnRegions[i].GetHashCode()))
                    {
                        LogDebug($"REALLY not sure why but somehow mCustomSpawnRegions now contains a hashcode {sceneSpawnRegions[i].GetHashCode()} when it didn't before entering this loop OR finishing trying to match against proxies. Skipping this spawn region immediately before corruption occurs!");
                        continue;
                    }
                    SpawnRegionModDataProxy newProxy = new SpawnRegionModDataProxy(Guid.NewGuid(), mLastSceneName, sceneSpawnRegions[i]); 
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
