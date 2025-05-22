using Harmony;
using MelonLoader.TinyJSON;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace ExpandedAiFramework
{
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

        private bool mInitializedScene = false;

        public override void OnLoadScene()
        {
            mInitializedScene = false;
        }


        public override void OnInitializedScene()
        {
            base.OnInitializedScene();
            if (!mInitializedScene && GameManager.m_ActiveScene.Contains("WILDLIFE"))
            {
                mInitializedScene = true;
                Load();
            }
        }


        private void Load()
        {
            mSpawnRegionModDataProxies.Clear();

            List<SpawnRegionModDataProxy> regionDataProxies = new List<SpawnRegionModDataProxy>();

            string proxiesString = mManager.LoadData($"{mManager.CurrentScene}_SpawnRegionModDataProxies");
            if (proxiesString != null)
            {
                Variant proxiesVariant = JSON.Load(proxiesString);
                foreach (var pathJSON in proxiesVariant as ProxyArray)
                {
                    SpawnRegionModDataProxy newProxy = new SpawnRegionModDataProxy();
                    JSON.Populate(pathJSON, newProxy);
                    regionDataProxies.Add(newProxy);
                }
            }

            Il2CppArrayBase<SpawnRegion> sceneSpawnRegions = Component.FindObjectsOfType<SpawnRegion>();

            //Not a huge fan of the loop, but until I get spawnregions added to a quadtree or something this is the simplest method to match them up.
            //Will be done at scene init, hitches are kind of expected here. Im not worrying about it, since this will prevent hitches hopefully during spawning.
            for (int i = 0, iMax = sceneSpawnRegions.Count; i < iMax; i++)
            {
                for (int j = 0, jMax = regionDataProxies.Count; j < jMax; j++)
                {
                    if (sceneSpawnRegions[i].m_AiSubTypeSpawned == regionDataProxies[j].AiSubType && SquaredDistance(sceneSpawnRegions[i].transform.position, regionDataProxies[j].OriginalPosition) <= 1.0f)
                    {
                        // match found, do something..?
                    }
                }
            }
        }
    


        private void Save()
        {
            string json = JSON.Dump(mSpawnRegionModDataProxies.Values.ToList(), EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints);
            mManager.SaveData(json, $"{mManager.CurrentScene}_SpawnRegionModDataProxies");
        }


         /* 
        // Make sure
        mSpawnRegionModDataProxies.Clear();
        string proxiesString = ModData.Load("SpawnRegionModDataProxies");
        if (proxiesString != null)
        {
            Variant proxiesVariant = JSON.Load(proxiesString);
            foreach (var pathJSON in proxiesVariant as ProxyArray)
            {
                canAdd = true;
                TypeSpecificSpawnRegionModDataProxy newProxy = new TypeSpecificSpawnRegionModDataProxy();
                JSON.Populate(pathJSON, newProxy);
                if (!mSpawnRegionModDataProxies.TryGetValue(newProxy.Scene, out List<TypeSpecificSpawnRegionModDataProxy> proxies))
                {
                    proxies = new List<TypeSpecificSpawnRegionModDataProxy>();
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
        */

        //move this to its own data file similar 

        public SpawnRegionManager(EAFManager manager, ISubManager[] subManagers)
            : base(manager, subManagers) { }

        public override void Shutdown()
        {
        }


        void a() 
        {

        }

    }
}
