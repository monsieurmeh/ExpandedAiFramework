using Harmony;
using MelonLoader.TinyJSON;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2Cpp;
using Il2CppRewired.Utils;
using System.Collections.Generic;


namespace ExpandedAiFramework
{
    public sealed class SpawnRegionManager : BaseSubManager
    {
        private Dictionary<int, CustomBaseSpawnRegion> mCustomSpawnRegions = new Dictionary<int, CustomBaseSpawnRegion>();
        private Dictionary<Guid, CustomBaseSpawnRegion> mCustomSpawnRegionsByGuid = new Dictionary<Guid, CustomBaseSpawnRegion>();
        private HashSet<SpawnRegion> mSpawnRegionCatcher = new HashSet<SpawnRegion>();
        private DataManager mDataManager;

        public SpawnRegionManager(EAFManager manager, ISubManager[] subManagers) : base(manager, subManagers) { }
        public Dictionary<int, CustomBaseSpawnRegion> CustomSpawnRegions { get { return mCustomSpawnRegions; } }
        public Dictionary<Guid, CustomBaseSpawnRegion> CustomSpawnRegionsByGuid { get { return mCustomSpawnRegionsByGuid; } }


        public override void Initialize(EAFManager manager, ISubManager[] subManagers)
        {
            base.Initialize(manager, subManagers);
            mDataManager = mManager.DataManager;
        }


        public override void OnLoadScene(string sceneName)
        {
            base.OnLoadScene(sceneName);
            ClearCustomSpawnRegions();
        }


        public override void OnInitializedScene(string sceneName)
        {
            base.OnInitializedScene(sceneName);
        }


        public bool TryStart(SpawnRegion spawnRegion)
        {
            if (!Utility.IsValidGameplayScene(mManager.CurrentScene, out _))
            {
                LogTrace($"[SpawnRegionManager.TryStart] SpawnRegion with hash code {spawnRegion.GetHashCode()} caught outside of valid scene, catching and queueing for match-up on scene init...");
                if (mSpawnRegionCatcher.Contains(spawnRegion))
                {
                    LogTrace($"[SpawnRegionManager.TryStart] SpawnRegion with hash code {spawnRegion.GetHashCode()} already caught, aborting...");
                    return false;
                }
                mSpawnRegionCatcher.Add(spawnRegion);
                return true;
            }
            LogTrace($"[SpawnRegionManager.TryStart] SpawnRegion with hash code {spawnRegion.GetHashCode()} caught after scene init, processing immediately..");
            return ProcessCaughtSpawnRegion(spawnRegion);
        }


        private bool ProcessCaughtSpawnRegion(SpawnRegion spawnRegion)
        {
            int hashcode = spawnRegion.GetHashCode();
            if (mCustomSpawnRegions.ContainsKey(hashcode))
            {
                LogTrace($"[SpawnRegionManager.ProcessCaughtSpawnRegion] Already started spawn region with hash code {hashcode}, aborting...");
                return false;
            }
            if (!TryInjectCustomSpawnRegion(spawnRegion, out CustomBaseSpawnRegion customSpawnRegion))
            {
                LogError($"[SpawnRegionManager.ProcessCaughtSpawnRegion] Could not inject custom base spawn region to wrap spawn region with hashcode {hashcode}!");
                return false;
            }
            //customSpawnRegion.OverrideStart();
            mDataManager.QueueNewSpawns(customSpawnRegion);
            LogTrace($"[SpawnRegionManager.ProcessCaughtSpawnRegion] Successfully started spawn region with hash code {hashcode}!");
            return true;
        }


        public void ProcessCaughtSpawnRegions()
        {
            foreach (SpawnRegion spawnRegion in mSpawnRegionCatcher)
            {
                if (spawnRegion == null)
                {
                    //from previous scene or something, discard
                    continue;
                }
                LogTrace($"[SpawnRegionManager.ProcessCaughtSpawnRegions]: Caught spawn region loading before scene initialization with hash code {spawnRegion.GetHashCode()}, processing...");
                ProcessCaughtSpawnRegion(spawnRegion);
            }
            mSpawnRegionCatcher.Clear();
        }

        public void ClearCustomSpawnRegions()
        {
            foreach (CustomBaseSpawnRegion customSpawnRegion in mCustomSpawnRegions.Values)
            {
                TryRemoveCustomSpawnRegion(customSpawnRegion.SpawnRegion);
            }
            mSpawnRegionCatcher.Clear();
            mCustomSpawnRegions.Clear();
            mCustomSpawnRegionsByGuid.Clear();
        }


        public bool TryInterceptSpawn(BaseAi baseAi, SpawnRegion spawnRegion)
        {
            if (spawnRegion == null)
            {
                LogTrace($"[SpawnRegionManager.TryInterceptSpawn] Null spawn region, can't intercept.");
                return false;
            }
            if (!mCustomSpawnRegions.TryGetValue(spawnRegion.GetHashCode(), out CustomBaseSpawnRegion customSpawnRegion))
            {
                LogTrace($"[SpawnRegionManager.TryInterceptSpawn] SpawnRegion with hashcode {spawnRegion.GetHashCode()} was not pre-wrapped on scene load, wrapping...");
                //option a: try to inject
                //option b: fallback look for object guid and try to rematch there? maybe more consistent. we'll see

                //option a first
                if (!TryInjectCustomSpawnRegion(spawnRegion, out customSpawnRegion))
                {
                    LogTrace($"[SpawnRegionManager.TryInterceptSpawn] Failed to inject new custom region during fallback spawn interception. Might be time to try option b!");
                    return false;
                }
            }
            if (!mDataManager.TryGetNextAvailableSpawnModDataProxy(customSpawnRegion.ModDataProxy.Guid, out SpawnModDataProxy proxy))
            {
                LogTrace($"[SpawnRegionManager.TryInterceptSpawn] no queued spawns in region with hash code {spawnRegion.GetHashCode()}, deferring to ai manager random logic");
                return mManager.AiManager.TryInjectRandomCustomAi(baseAi, spawnRegion);
            }

            if (!mManager.AiManager.TryInjectCustomAi(baseAi, proxy.VariantSpawnType, spawnRegion))
            {
                LogError($"[SpawnRegionManager.TryInterceptSpawn] Error in AiManager ai injection process while trying to respawn previously found ai variant in region with hash code {spawnRegion.GetHashCode()}!");
                return false;
            }
            LogTrace($"[SpawnRegionManager.TryInterceptSpawn] Successfully spawned a {proxy.VariantSpawnType.Name} where it first spawned in spawn region with hash code {spawnRegion.GetHashCode()}!");
            return true;
        }


        public bool TryRemoveCustomSpawnRegion(SpawnRegion spawnRegion)
        {
            if (spawnRegion == null)
            {
                return false;
            }
            if (!mCustomSpawnRegions.TryGetValue(spawnRegion.GetHashCode(), out CustomBaseSpawnRegion customSpawnRegion))
            {
                return false;
            }
            customSpawnRegion.Despawn(GetCurrentTimelinePoint());
            //UnityEngine.Object.Destroy(customSpawnRegion.Self); won't be needed until (unless) we turn CustomBaseSpawnRegion into a ticking monobomb
            mCustomSpawnRegions.Remove(spawnRegion.GetHashCode());
            if (customSpawnRegion.ModDataProxy != null)
            {
                mCustomSpawnRegionsByGuid.Remove(customSpawnRegion.ModDataProxy.Guid);
            }
            return true;
        }


        private bool TryInjectCustomSpawnRegion(SpawnRegion spawnRegion, out CustomBaseSpawnRegion customSpawnRegion)
        {
            customSpawnRegion = null;
            if (spawnRegion == null)
            {
                LogTrace($"[SpawnRegionManager.TryInjectCustomSpawnRegion] Null spawn region. cannot inject custom spawn region");
                return false;
            }
            if (mCustomSpawnRegions.TryGetValue(spawnRegion.GetHashCode(), out customSpawnRegion))
            {
                //LogTrace($"Previously matched spawn region with hash code {spawnRegion.GetHashCode()} and guid {customSpawnRegion.ModDataProxy.Guid}, skipping.");
                return false;
            }
            if (!spawnRegion.TryGetComponent<ObjectGuid>(out ObjectGuid guid))
            {
                LogError($"[SpawnRegionManager.TryInjectCustomSpawnRegion]: Could not find ObjectGuid on spawn region with hashcode {spawnRegion.GetHashCode()}!");
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
                LogTrace($"[SpawnRegionManager.InjectCustomSpawnRegion] No spawn region mod data proxy matched to spawn region with hashcode {spawnRegion.GetHashCode()} and guid {guid}. creating then wrapping");
                matchedProxy = GenerateNewSpawnRegionModDataProxy(mManager.CurrentScene, spawnRegion, guid);
            }
            else
            {
                LogTrace($"[SpawnRegionManager.InjectCustomSpawnRegion] Matched existing spawn region mod data proxy with guid {guid} against found spawn region!");
            }
            CustomBaseSpawnRegion newSpawnRegionWrapper = new CustomBaseSpawnRegion(spawnRegion, matchedProxy, mTimeOfDay);
            mCustomSpawnRegions.Add(spawnRegion.GetHashCode(), newSpawnRegionWrapper);
            mCustomSpawnRegionsByGuid.Add(matchedProxy.Guid, newSpawnRegionWrapper);
            return newSpawnRegionWrapper;
        }


        public SpawnRegionModDataProxy GenerateNewSpawnRegionModDataProxy(string scene, SpawnRegion spawnRegion, Guid guid)
        {
            if (spawnRegion == null)
            {
                LogTrace($"[SpawnRegionManager.GenerateNewSpawnRegionModDataProxy]: Cant generate a new spawn mod data proxy without parent region!");
                return null;
            }
            if (mCustomSpawnRegions.TryGetValue(spawnRegion.GetHashCode(), out CustomBaseSpawnRegion customSpawnRegion))
            {
                LogTrace($"[AiManager.GenerateNewSpawnModDataProxy] Spawn region with hash code {spawnRegion.GetHashCode()} already wrapped, cannot re-wrap!");
                return null;
            }
            SpawnRegionModDataProxy newProxy = new SpawnRegionModDataProxy(guid, scene, spawnRegion);
            if (!mDataManager.TryRegisterActiveSpawnRegionModDataProxy(newProxy))
            {
                LogTrace($"[AiManager.GenerateNewSpawnModDataProxy] Couldnt register new spawn region mod data proxy with guid {newProxy.Guid} due to guid collision!");
                return null;
            }
            return newProxy;
        }


        public override void Shutdown()
        {
            ClearCustomSpawnRegions();
            base.Shutdown();
        }
    }
}
