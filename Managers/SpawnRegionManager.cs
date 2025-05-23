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
    //mid-implementation notes
    // 
    // Eventually like described below i want these to be their own routines so to speak.
    // For now, they are just wrappers used at runtime to centrally direct other mods to do stuff with the spawn region persistently.
    // 
    public sealed class SpawnRegionManager : BaseSubManager
    {
        // Plan for wrapping vanilla spawn regions, while providing json-adjustable entries for planned matches in game.
        //0) I want to create and load in a set of original, vanilla spawn region mod data proxies that contain a name and original vanilla location in scene plus spawned ai type.
        //This will need to be done with a debug tool, and i want it saved to json similar to other data constructs.
        //NOT storing this in data manager because this is specific to spawn region and spawn data management. It has a place, and that place is right here.
        // As soon as the "_WILDLIFE" scene hits (OR, if we can capture it being done dynamically consistently, during spawn region creation), we will:
        //1) Attempt to build a dictionary of in-scene spawn region data proxies and spawn data proxies.
        //   Try to load list of SpawnRegionModDataProxy instances from moddata. If first scene load, try to load some presets from json or some other source.
        //   Try to load list of SpawnDataProxy instances from moddata. Nothing on first scene load, havent generated any spawns yet. Maybe we can program in some later but doubt i will.
        //   Therefore, we'll start the scene with a List<SpawnRegionModDataProxy> loaded from mod data for step 2 and potentially a List<Spawn
        //2) Loop through in-scene spawn regions and attempt to wrap each spawn region in list of candidates.
        //   This will HAVE to be done the hard way: checking against name, position, type, etc.
        //   This will used the stored 'ORIGINAL POSITION' to match against since the spawn region would not have moved yet.
        //   If one isn't found, generate one.
        //   Put match in a Dictionary<GUID, SpawnRegionModDataProxy> mSpawnRegionModDataProxies.
        //   Discard any extras left over from json, wont be needed. Discard any spawn data proxies associated with the spawn region mod data proxy's guid.
        //   Flip a flag or add data to the spawn region manager to indicate that spawn regions have been loaded previously for this scene so future instances just default to creating new proxies.
        //3) After all regions are wrapped (or maybe during the wrapping process? makes sense honestly):
        //   The spawn region will be shifted to the "current" location serialized in the mod data proxy.
        //   Because spawn regions were wrapped before moving and have guids, we should be able to store the spawn region GUID in the spawn mod data proxy and match that way to the dictionary.
        //   This position will be available for read/write via api for other systems to interact with, including my own sub systems for migration. Will use prefix logic.
        //4) When something spawns, persistency management system will try to match against spawn region proxy list instead of the spawn region itself v ia guid.
        //5) I want all matching done via GUID. Spwan region mod data proxies, baseaidataproxy, etc. everything!


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
            if (!mInitializedScene && sceneName.Contains("WILDLIFE"))
            {
                LogDebug($"SpawnRegionManager initializing in scene {sceneName}");
                mInitializedScene = true;
                mLastSceneName = mManager.CurrentScene;
                InitializeSpawnRegionModDataProxies();
            }
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
                LogDebug("no queued spawns, deferring to ai manager random logic");
                return mManager.AiManager.TryInjectRandomCustomAi(baseAi, spawnRegion);
            }

            if (!mManager.AiManager.TryInjectCustomAi(baseAi, proxy.VariantSpawnType, spawnRegion))
            {
                LogError("Error in AiManager ai injection process while trying to respawn previously found ai variant!");
                return false;
            }
            LogDebug($"Successfully spawned a {proxy.VariantSpawnType} where it first spawned!");
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


        private void InitializeSpawnRegionModDataProxies()
        {
            mSpawnRegionModDataProxies.Clear();

            List<SpawnRegionModDataProxy> regionDataProxies = new List<SpawnRegionModDataProxy>();

            LogDebug($"Trying to load spawnregion mod data proxie from suffix {mManager.CurrentScene}_SpawnRegionModDataProxies!");
            string proxiesString = mManager.LoadData($"{mManager.CurrentScene}_SpawnRegionModDataProxies");
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
                        LogVerbose("AiSubtype mismatch");
                        continue;
                    }
                    if (SquaredDistance(sceneSpawnRegions[i].transform.position, regionDataProxies[j].OriginalPosition) > 1.0f)
                    {
                        LogVerbose("proximity misimatch");
                        continue;
                    }
                    //todo: turn into "injectCustomSpawnRegion" method similar to aimanager
                    matchFound = true;
                    mCustomSpawnRegions.Add(sceneSpawnRegions[i].GetHashCode(), new CustomBaseSpawnRegion(sceneSpawnRegions[i], regionDataProxies[j], mTimeOfDay));
                    mCustomSpawnRegionsByGuid.Add(regionDataProxies[j].Guid, mCustomSpawnRegions[sceneSpawnRegions[i].GetHashCode()]);
                    mSpawnRegionModDataProxies.Add(regionDataProxies[j].Guid, regionDataProxies[j]);
                    //LogDebug($"Registered custom spawn region using serialized region proxy data! lets gooooooooo");
                }
                if (!matchFound)
                {
                    SpawnRegionModDataProxy newProxy = new SpawnRegionModDataProxy(Guid.NewGuid(), mManager.CurrentScene, sceneSpawnRegions[i]); 
                    //LogDebug($"No match found, generated new proxy with guid {newProxy.Guid}");
                    mCustomSpawnRegions.Add(sceneSpawnRegions[i].GetHashCode(), new CustomBaseSpawnRegion(sceneSpawnRegions[i], newProxy, mTimeOfDay)); 
                    mCustomSpawnRegionsByGuid.Add(newProxy.Guid, mCustomSpawnRegions[sceneSpawnRegions[i].GetHashCode()]);
                    mSpawnRegionModDataProxies.Add(newProxy.Guid, newProxy);
                }
                matchFound = false;
            }
        }
    

        private void SaveSpawnRegionModDataProxies()
        {
            //LogDebug($"Saving spawnregion mod data proxies to path {mManager.CurrentScene}_SpawnRegionModDataProxies!");
            string json = JSON.Dump(mSpawnRegionModDataProxies.Values.ToList(), EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints);
            mManager.SaveData(json, $"{mLastSceneName}_SpawnRegionModDataProxies");
            mLastSceneName = string.Empty;
            //for now, intentionally no clearing until next load - id like the runtime data available until after aimanager is done with it during its shutdown
        }


        public override void Shutdown()
        {
            //SaveSpawnRegionModDataProxies(); no! bad shutdown! do NOT serialize without an actual save request!
            ClearCustomSpawnRegions();
            base.Shutdown();
        }
    }
}
