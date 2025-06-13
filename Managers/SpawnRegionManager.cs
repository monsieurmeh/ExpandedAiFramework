using Harmony;
using MelonLoader.TinyJSON;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2Cpp;
using Il2CppRewired.Utils;
using Il2CppTLD.PDID;

namespace ExpandedAiFramework
{
    public sealed class SpawnRegionManager : BaseSubManager
    {
        #region Fields/Properties/Constructors

        private Il2Cpp.SpawnRegionManager mVanillaManager;
        private Dictionary<int, CustomBaseSpawnRegion> mCustomSpawnRegions = new Dictionary<int, CustomBaseSpawnRegion>();
        private Dictionary<Guid, CustomBaseSpawnRegion> mCustomSpawnRegionsByGuid = new Dictionary<Guid, CustomBaseSpawnRegion>();
        private List<CustomBaseSpawnRegion> mCustomSpawnRegionsByIndex = new List<CustomBaseSpawnRegion>();
        private DataManager mDataManager;

        private Il2Cpp.SpawnRegionManager safeVanillaManager
        {
            get
            {
                if (mVanillaManager.IsNullOrDestroyed())
                {
                    mVanillaManager = GameManager.m_SpawnRegionManager;
                }
                return mVanillaManager;
            }
        }

        public SpawnRegionManager(EAFManager manager, ISubManager[] subManagers) : base(manager, subManagers) { }
        public Dictionary<int, CustomBaseSpawnRegion> CustomSpawnRegions { get { return mCustomSpawnRegions; } }
        public Dictionary<Guid, CustomBaseSpawnRegion> CustomSpawnRegionsByGuid { get { return mCustomSpawnRegionsByGuid; } }

        #endregion


        #region BaseSubManager

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


        public override void Shutdown()
        {
            ClearCustomSpawnRegions();
            base.Shutdown();
        }


        public override void Update()
        {
            base.Update();
            VanillaUpdate();
        }

        #endregion


        #region API

        public void QueueNewSpawns(CustomBaseSpawnRegion spawnRegion)
        {
            if (spawnRegion.ModDataProxy == null || spawnRegion.ModDataProxy.Guid == Guid.Empty)
            {
                return;
            }
            List<Guid> guids = mManager.DataManager.GetCachedSpawnModDataProxiesByParentGuid(spawnRegion.ModDataProxy.Guid);
            if (spawnRegion.VanillaSpawnRegion == null || spawnRegion.VanillaSpawnRegion.m_SpawnablePrefab == null || !spawnRegion.VanillaSpawnRegion.m_SpawnablePrefab.TryGetComponent<BaseAi>(out BaseAi spawnableAi) || spawnableAi.IsNullOrDestroyed())
            {
                LogTrace($"Can't get spawnable ai script from spawn region, no pre-queueing spawns. Next...");
                return;
            }
            for (int i = guids.Count, iMax = 3; i < iMax; i++)
            {
                LogTrace($"Queueing new spawn for spawn region with guid {spawnRegion.ModDataProxy.Guid} #{i}");
                mManager.TypePicker.PickTypeAsync(spawnableAi, (type) =>
                {
                    SpawnModDataProxy newProxy = mManager.AiManager.GenerateNewSpawnModDataProxy(mManager.CurrentScene, spawnRegion.VanillaSpawnRegion, type);
                    guids.Add(newProxy.Guid);
                });
            }
        }


        public bool TryInterceptSpawn(BaseAi baseAi, SpawnRegion spawnRegion)
        {
            if (spawnRegion == null)
            {
                LogTrace($"Null spawn region, can't intercept.");
                return false;
            }
            if (!mCustomSpawnRegions.TryGetValue(spawnRegion.GetHashCode(), out CustomBaseSpawnRegion customSpawnRegion))
            {
                LogError($"Shouldnt be happening anymore!");
                return false;
                LogTrace($"SpawnRegion with hashcode {spawnRegion.GetHashCode()} was not pre-wrapped on scene load, wrapping...");
                //option a: try to inject
                //option b: fallback look for object guid and try to rematch there? maybe more consistent. we'll see

                //option a first
                if (!TryInjectCustomSpawnRegion(spawnRegion, out customSpawnRegion))
                {
                    LogTrace($"Failed to inject new custom region during fallback spawn interception. Might be time to try option b!");
                    return false;
                }
            }
            if (!mDataManager.TryGetNextAvailableSpawnModDataProxy(customSpawnRegion.ModDataProxy.Guid, out SpawnModDataProxy proxy))
            {
                LogTrace($"no queued spawns in region with hash code {spawnRegion.GetHashCode()}, deferring to ai manager random logic");
                return mManager.AiManager.TryInjectRandomCustomAi(baseAi, spawnRegion);
            }

            if (!mManager.AiManager.TryInjectCustomAi(baseAi, proxy.VariantSpawnType, spawnRegion, proxy))
            {
                LogError($"Error in AiManager ai injection process while trying to respawn previously found ai variant in region with hash code {spawnRegion.GetHashCode()}!");
                return false;
            }
            LogTrace($"Successfully spawned a {proxy.VariantSpawnType.Name} where it first spawned in spawn region with hash code {spawnRegion.GetHashCode()}!");
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


        #region SpawnRegionManager Vanilla Rerouting

        /* No longer supporting "adding" spawn regions, they get caught during awake now.
        public void Add(SpawnRegion spawnRegion)
        {
            if (!CheckVanillaManager())
            {
                LogTrace("Can't get vanilla SpawnRegionManager");
                return;
            }
            if (mVanillaManager.m_SpawnRegions.IsNullOrDestroyed())
            {
                LogError($"Null mVanillaManager.m_SpawnRegions");
                return;
            }
            if (mVanillaManager.m_SpawnRegions.Contains(spawnRegion))
            {
                LogError($"mVanillaManager already containts spawn region");
                return;
            }
            mVanillaManager.m_SpawnRegions.Add(spawnRegion);
        }
        */

        public void Add(NoSpawnRegion noSpawnRegion)
        {
            if (!CheckVanillaManager())
            {
                LogTrace("Can't get vanilla SpawnRegionManager");
                return;
            }
            if (mVanillaManager.m_NoSpawnRegions.IsNullOrDestroyed())
            {
                LogError($"Null mVanillaManager.m_NoSpawnRegions");
                return;
            }
            if (mVanillaManager.m_NoSpawnRegions.Contains(noSpawnRegion))
            {
                LogError($"mVanillaManager already containts no spawn region");
                return;
            }
            mVanillaManager.m_NoSpawnRegions.Add(noSpawnRegion);
        }


        public void Deserialize(string text)
        {
            try
            {
                if (!CheckVanillaManager())
                {
                    LogTrace("Can't get vanilla SpawnRegionManager");
                    return;
                }
                if (string.IsNullOrEmpty(text))
                {
                    LogError($"Null or empty deserialize text");
                    return;
                }
                SpawnRegionSaveList spawnRegionSaveList = Utils.DeserializeObject<SpawnRegionSaveList>(text);
                if (spawnRegionSaveList.IsNullOrDestroyed())
                {
                    LogError($"Could not deserialize text");
                    return;
                }
                mManager.DataManager.LoadProxies();
                mManager.DataManager.UncacheProxies();
                Il2Cpp.SpawnRegionManager.m_NoPredatorSpawningInVoyageurHours = spawnRegionSaveList.m_NoPredatorSpawningInVoyageurHours;
                foreach (SpawnRegionSaveData data in spawnRegionSaveList.m_SerializedSpawnRegions)
                {
                    SpawnRegion spawnRegion = FindSpawnRegionByGuid(data.m_Guid);
                    if (spawnRegion.IsNullOrDestroyed())
                    {
                        LogTrace($"Could not fetch spawn region by guid, attempting to fetch by position");
                        spawnRegion = FindSpawnRegionByPosition(data);
                    }
                    if (spawnRegion.IsNullOrDestroyed())
                    {
                        LogError($"Could not fetch spawn region by guid OR position, skipping");
                        continue;
                    }
                    spawnRegion.Deserialize(data.m_SearializedSpawnRegion);
                    ProcessCaughtSpawnRegion(spawnRegion, out CustomBaseSpawnRegion customSpawnRegion);
                    LogTrace($"Deserialized spawn region");
                }
            }
            catch (Exception e)
            {
                LogError($"SpawnRegionManager.Deserialize error: {e}");
            }
        }


        public SpawnRegion FindSpawnRegionByGuid(string guid)
        {
            if (!CheckVanillaManager())
            {
                LogTrace("Can't get vanilla SpawnRegionManager");
                return null;
            }
            GameObject go = PdidTable.GetGameObject(guid);
            if (go.IsNullOrDestroyed())
            {
                LogTrace($"Could not fetch object by guid");
                return null;
            }
            if (!go.TryGetComponent<SpawnRegion>(out SpawnRegion existingSpawnRegion))
            {
                LogError($"Could not fetch SpawnRegion from GUID-fetched object");
                return null;
            }
            LogTrace($"Found spawn region by GUID");
            return existingSpawnRegion;
        }


        public SpawnRegion FindSpawnRegionByPosition(SpawnRegionSaveData saveData)
        {
            if (!CheckVanillaManager())
            {
                LogTrace("Can't get vanilla SpawnRegionManager");
                return null;
            }
            for (int i = 0; i < mCustomSpawnRegionsByIndex.Count; i++)
            {
                SpawnRegion spawnRegion = mCustomSpawnRegionsByIndex[i].VanillaSpawnRegion;
                if (Vector3.Distance(spawnRegion.transform.position, saveData.m_Position) < 0.01f)
                {
                    LogTrace($"Found spawn region by position");
                    return spawnRegion;
                }
            }
            LogTrace($"Could not fetch spawn region by position");
            return null;
        }


        public GameObject GetClosestActiveSpawn(Vector3 pos, string filterSpawnablePrefabName)
        {
            if (!CheckVanillaManager())
            {
                LogTrace("Can't get vanilla SpawnRegionManager");
                return null;
            }
            SpawnRegion pointInsideSpawnRegion = PointInsideSpawnRegion(pos, filterSpawnablePrefabName);
            if (pointInsideSpawnRegion.IsNullOrDestroyed())
            {
                LogTrace($"No spawn region found containing position");
                return null;
            }
            GameObject closestSpawn = pointInsideSpawnRegion.GetClosestActiveSpawn(pos);
            if (closestSpawn.IsNullOrDestroyed())
            {
                LogTrace($"No active spawns found");
                return null;
            }
            return closestSpawn;
        }


        public void MaybeEnableSpawnRegionsInRange(SpawnRegion otherSpawnRegion, float range, bool enable)
        {
            if (!CheckVanillaManager())
            {
                LogTrace("Can't get vanilla SpawnRegionManager");
                return;
            }
            if (otherSpawnRegion.IsNullOrDestroyed())
            {
                LogError($"Recieved null otherSpawnRegion");
                return;
            }
            range *= range;
            foreach (CustomBaseSpawnRegion customBaseSpawnRegion in mCustomSpawnRegionsByIndex)
            {
                if (customBaseSpawnRegion.VanillaSpawnRegion.IsNullOrDestroyed())
                {
                    LogWarning($"Found null or destroyed spawn region in mCustomSpawnRegionsByIndex");
                    continue;
                }
                if (otherSpawnRegion.m_AiSubTypeSpawned != customBaseSpawnRegion.VanillaSpawnRegion.m_AiSubTypeSpawned)
                {
                    LogVerbose($"Spawn region subtype mismatch");
                    continue;
                }
                customBaseSpawnRegion.VanillaSpawnRegion.m_WasForceDisabled = !enable;
                if (!customBaseSpawnRegion.VanillaSpawnRegion.m_WasEnabled && enable)
                {
                    LogVerbose($"spawnRegion was not enabled and enable is set, continuing");
                    continue;
                }
                if (SquaredDistance(otherSpawnRegion.transform.position, customBaseSpawnRegion.VanillaSpawnRegion.transform.position) > range)
                {
                    LogVerbose($"spawnRegion out of range, continuing");
                    continue;
                }
                customBaseSpawnRegion.VanillaSpawnRegion.m_WasEnabled = customBaseSpawnRegion.VanillaSpawnRegion.isActiveAndEnabled;
                customBaseSpawnRegion.VanillaSpawnRegion.gameObject.SetActive(enable);
                LogTrace($"spawnregion was enabled: <<<{customBaseSpawnRegion.VanillaSpawnRegion.m_WasEnabled}>>> and is now enabled: <<<{enable}>>>");
            }
        }


        public void OnAuroraEnabled(bool enabled)
        {
            if (!CheckVanillaManager())
            {
                LogTrace("Can't get vanilla SpawnRegionManager");
                return;
            }
            foreach (CustomBaseSpawnRegion customSpawnRegion in mCustomSpawnRegionsByIndex)
            {
                if (customSpawnRegion.VanillaSpawnRegion.IsNullOrDestroyed())
                {
                    LogWarning($"Null or destroyed SpawnRegion in mCustomSpawnRegionsByIndex");
                    continue;
                }
                customSpawnRegion.OnAuroraEnabled(enabled);
            }
        }


        public SpawnRegion PointInsideActiveSpawnRegion(Vector3 pos, string filterSpawnablePrefabName)
        {
            return PointInsideSpawnRegionInternal(pos, filterSpawnablePrefabName, true);
        }


        public SpawnRegion PointInsideSpawnRegion(Vector3 pos, string filterSpawnablePrefabName)
        {
            return PointInsideSpawnRegionInternal(pos, filterSpawnablePrefabName, false);
        }


        private SpawnRegion PointInsideSpawnRegionInternal(Vector3 pos, string filterSpawnablePrefabName, bool checkActive)
        {
            if (!CheckVanillaManager())
            {
                LogTrace("Can't get vanilla SpawnRegionManager");
                return null;
            }
            bool shouldFilterByName = !string.IsNullOrEmpty(filterSpawnablePrefabName);
            foreach (CustomBaseSpawnRegion customSpawnRegion in mCustomSpawnRegionsByIndex)
            {
                if (customSpawnRegion.VanillaSpawnRegion.IsNullOrDestroyed())
                {
                    LogWarning($"Null or destroyed SpawnRegion in mCustomSpawnRegionsByIndex, continuing");
                    continue;
                }
                if (SquaredDistance(pos, customSpawnRegion.VanillaSpawnRegion.transform.position) > customSpawnRegion.VanillaSpawnRegion.m_Radius * customSpawnRegion.VanillaSpawnRegion.m_Radius)
                {
                    LogTrace($"SpawnRegion too far, continuing");
                    continue;
                }
                if (shouldFilterByName && filterSpawnablePrefabName != customSpawnRegion.VanillaSpawnRegion.GetSpawnablePrefabName())
                {
                    LogTrace($"Failed to match prefab name, continuing");
                    continue;
                }
                if (checkActive && !customSpawnRegion.VanillaSpawnRegion.gameObject.activeSelf)
                {
                    LogTrace($"inactive spawnregion, continuing");
                    continue;
                }
                LogTrace($"found containing SpawnRegion");
                return customSpawnRegion.VanillaSpawnRegion;
            }
            LogTrace($"Failed to find any containing SpawnRegion");
            return null;
        }


        public bool PointInsideNoSpawnRegion(Vector3 pos)
        {
            if (!CheckVanillaManager())
            {
                LogTrace("Can't get vanilla SpawnRegionManager");
                return false;
            }
            if (mVanillaManager.m_NoSpawnRegions.IsNullOrDestroyed())
            {
                LogError($"NullOrDestroyed mVanillaManager.m_NoSpawnRegions");
                return false;
            }
            foreach (NoSpawnRegion noSpawnRegion in mVanillaManager.m_NoSpawnRegions)
            {
                if (noSpawnRegion.IsNullOrDestroyed())
                {
                    LogWarning($"Null or destroyed NoSpawnRegion in mVanillaManager.m_NoSpawnRegions, continuing");
                    continue;
                }
                if (SquaredDistance(pos, noSpawnRegion.transform.position) > noSpawnRegion.m_Radius * noSpawnRegion.m_Radius)
                {
                    LogTrace($"NoSpawnRegion too far, continuing");
                    continue;
                }
                if (!noSpawnRegion.gameObject.activeSelf)
                {
                    LogTrace($"inactive spawnregion, continuing");
                    continue;
                }
                LogTrace($"found containing NoSpawnRegion");
                return true;
            }
            LogTrace($"Failed to find any containing NoSpawnRegion");
            return false;
        }


        //I see no reason to support "removing" spawn regions; just disable them...
        /*
        public void Remove(SpawnRegion spawnRegion)
        {
            if (!CheckVanillaManager())
            {
                LogTrace("Can't get vanilla SpawnRegionManager");
                return;
            }
            if (mVanillaManager.m_SpawnRegions.IsNullOrDestroyed())
            {
                LogError($"NullOrDestroyed mVanillaManager.m_SpawnRegions");
                return;
            }
            if (mVanillaManager.m_SpawnRegions.Count <= 0)
            {
                LogTrace($"Empty mVanillaManager.m_SpawnRegions");
                return;
            }
            if (!mVanillaManager.m_SpawnRegions.Contains(spawnRegion))
            {
                LogTrace($"!mVanillaManager.Contains(spawnRegion)");
                return;
            }
            if (!mCustomSpawnRegions.TryGetValue(spawnRegion.GetHashCode(), out CustomBaseSpawnRegion customSpawnRegion))
            {
                LogTrace($"Cannot find associated custom spawn region");
                return;
            }
            if (!mVanillaManager.m_SpawnRegions.Remove(spawnRegion) || TryRemoveCustomSpawnRegion(spawnRegion))
            {
                LogError($"Failed to remove spawnRegion");
                return;
            }
            LogTrace($"SpawnRegion removed");
        }
        */


        public string Serialize()
        {
            try
            {
                if (!CheckVanillaManager())
                {
                    LogTrace("Can't get vanilla SpawnRegionManager");
                    return string.Empty;
                }
                SpawnRegionSaveList spawnRegionSaveList = new SpawnRegionSaveList();
                spawnRegionSaveList.m_SerializedSpawnRegions.Clear();
                for (int i = 0; i < mCustomSpawnRegionsByIndex.Count; i++)
                {
                    if (mCustomSpawnRegionsByIndex[i].VanillaSpawnRegion.IsNullOrDestroyed())
                    {
                        LogWarning($"NullOrDestroyed SpawnRegion");
                        continue;
                    }
                    if (!mCustomSpawnRegionsByIndex[i].VanillaSpawnRegion.gameObject.TryGetComponent(out ObjectGuid objectGuid))
                    {
                        LogError($"Could not find objectGuid on SpawnRegion");
                        continue;
                    }
                    SpawnRegionSaveData spawnRegionSaveData = new SpawnRegionSaveData();
                    spawnRegionSaveData.m_Position = mCustomSpawnRegionsByIndex[i].VanillaSpawnRegion.transform.position;
                    spawnRegionSaveData.m_SearializedSpawnRegion = mCustomSpawnRegionsByIndex[i].Serialize();
                    spawnRegionSaveData.m_Guid = objectGuid.PDID;
                    spawnRegionSaveList.m_SerializedSpawnRegions.Add(spawnRegionSaveData);
                }
                spawnRegionSaveList.m_NoPredatorSpawningInVoyageurHours = Il2Cpp.SpawnRegionManager.m_NoPredatorSpawningInVoyageurHours;
                return Utils.SerializeObject(spawnRegionSaveList);
            }
            catch (Exception e)
            {
                LogError($"SpawnRegionManager.Serialize error: {e}");
                return string.Empty;
            }
        }


        public void Start()
        {
            if (!CheckVanillaManager())
            {
                LogTrace("Can't get vanilla SpawnRegionManager");
                return;
            }
            Il2Cpp.SpawnRegionManager.m_NoPredatorSpawningInVoyageurHours = UnityEngine.Random.Range(mVanillaManager.m_NoPredatorSpawningInVoyageurHoursMin, mVanillaManager.m_NoPredatorSpawningInVoyageurHoursMax);
            mVanillaManager.m_NextSpawnRegionIndexToUpdate = 0;
        }


        public void VanillaUpdate()
        {
            if (GameManager.m_IsPaused)
            {
                LogVerbose($"Paused");
                return;
            }
            if (!CheckVanillaManager())
            {
                //LogTrace("Can't get vanilla SpawnRegionManager");
                return;
            }
            if (mCustomSpawnRegionsByIndex.Count <= 0)
            {
                LogTrace($"Empty mCustomSpawnRegionsByIndex");
                return;
            }
            mVanillaManager.m_NextSpawnRegionIndexToUpdate++;
            if (mVanillaManager.m_NextSpawnRegionIndexToUpdate >= mCustomSpawnRegionsByIndex.Count)
            {
                mVanillaManager.m_NextSpawnRegionIndexToUpdate = 0;
            }
            //LogTrace($"Current index: {mVanillaManager.m_NextSpawnRegionIndexToUpdate} of {mVanillaManager.m_SpawnRegions.Count} total");
            SpawnRegion spawnRegion = mCustomSpawnRegionsByIndex[mVanillaManager.m_NextSpawnRegionIndexToUpdate].VanillaSpawnRegion;
            if (spawnRegion.IsNullOrDestroyed())
            {
                LogTrace($"NullOrDestroyed SpawnRegion");
                return;
            }
            spawnRegion.UpdateDeferredDeserialize();
            spawnRegion.MaybeReRollActive();

            if (spawnRegion.gameObject.activeInHierarchy)
            {
                spawnRegion.UpdateFromManager();
            }
            else if (ThreeDaysOfNight.IsActive())
            {
                if (!spawnRegion.transform.parent.IsNullOrDestroyed() && !spawnRegion.transform.parent.gameObject.activeInHierarchy)
                {
                    spawnRegion.transform.parent.gameObject.SetActive(true);
                }
                else
                {
                    spawnRegion.gameObject.SetActive(true);
                }
            }
        }

        #endregion


        #region SpawnRegion Vanilla Rerouting

        public bool TryAwake(SpawnRegion __instance)
        {
            return false;
            // We are now handling this stuff...
            // Worried about breaking some mods though?
            // In theory, post fixes should be immune to being blocked by prefixes, only exceptions can do that...right??
        }


        public bool TryCanTrap(SpawnRegion __instance, ref bool __result)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomBaseSpawnRegion customSpawnRegion))
            {
                return true;
            }
            __result = customSpawnRegion.CanTrap();
            return false;
        }


        public bool TryGetClosestActiveSpawn(SpawnRegion __instance, Vector3 pos, ref GameObject __result)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomBaseSpawnRegion customSpawnRegion))
            {
                return true;
            }
            __result = customSpawnRegion.GetClosestActiveSpawn(pos);
            return false;
        }


        public bool TryGetDenSleepDurationInHours(SpawnRegion __instance, ref float __result)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomBaseSpawnRegion customSpawnRegion))
            {
                return true;
            }
            __result = customSpawnRegion.GetDenSleepDurationInHours();
            return false;
        }


        public bool TryGetNumActiveSpawns(SpawnRegion __instance, ref int __result)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomBaseSpawnRegion customSpawnRegion))
            {
                return true;
            }
            __result = customSpawnRegion.GetNumActiveSpawns();
            return false;
        }


        public bool TryGetWanderRegion(SpawnRegion __instance, Vector3 pos, ref WanderRegion __result)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomBaseSpawnRegion customSpawnRegion))
            {
                return true;
            }
            __result = customSpawnRegion.GetWanderRegion(pos);
            return false;
        }
        
        public bool TryGetWaypointCircuit(SpawnRegion __instance, ref Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3> __result)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomBaseSpawnRegion customSpawnRegion))
            {
                return true;
            }
            __result = customSpawnRegion.GetWaypointCircuit();
            return false;
        }


        public bool TryUpdateDeferredDeserializeFromManager(SpawnRegion __instance)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomBaseSpawnRegion customSpawnRegion))
            {
                return true;
            }
            customSpawnRegion.UpdateDeferredDeserializeFromManager();
            return false;
        }

        public bool TryUpdateFromManager(SpawnRegion __instance)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomBaseSpawnRegion customSpawnRegion))
            {
                return true;
            }
            customSpawnRegion.UpdateFromManager();
            return false;
        }

        public bool TrySetRandomWaypointCircuit(SpawnRegion __instance)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomBaseSpawnRegion customSpawnRegion))
            {
                return true;
            }
            customSpawnRegion.SetRandomWaypointCircuit();
            return false;
        }

        public bool TryShouldSleepInDenAfterWaypointLoop(SpawnRegion __instance, ref bool __result)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomBaseSpawnRegion customSpawnRegion))
            {
                return true;
            }
            __result = customSpawnRegion.ShouldSleepInDenAfterWaypointLoop();
            return false;
        }

        public bool TrySetActive(SpawnRegion __instance, bool active)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomBaseSpawnRegion customSpawnRegion))
            {
                return true;
            }
            customSpawnRegion.SetActive(active);
            return false;
        }

        public bool TrySerialize(SpawnRegion __instance, ref string __result)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomBaseSpawnRegion customSpawnRegion))
            {
                return true;
            }
            __result = customSpawnRegion.Serialize();
            return false;
        }

        public bool TryRemoveFromSpawnRegion(SpawnRegion __instance, BaseAi baseAi)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomBaseSpawnRegion customSpawnRegion))
            {
                return true;
            }
            customSpawnRegion.RemoveFromSpawnRegion(baseAi);
            return false;
        }

        public bool TryOnAuroraEnabled(SpawnRegion __instance, bool enabled)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomBaseSpawnRegion customSpawnRegion))
            {
                return true;
            }
            customSpawnRegion.OnAuroraEnabled(enabled);
            return false;
        }

        public bool TryMaybeReRollActive(SpawnRegion __instance)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomBaseSpawnRegion customSpawnRegion))
            {
                return true;
            }
            customSpawnRegion.MaybeReRollActive();
            return false;
        }


        public bool TryStart(SpawnRegion __instance)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomBaseSpawnRegion customSpawnRegion))
            {
                if (!TryInjectCustomSpawnRegion(__instance, out customSpawnRegion))
                {
                    LogError($"Could not match OR create new wrapper for spawn region with hash code {__instance.GetHashCode()} during start!");
                    return false;
                }
            }
            customSpawnRegion.Start();
            return false;
        }


        public bool TryDeserialize(SpawnRegion __instance, string text)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomBaseSpawnRegion customSpawnRegion))
            {
                return true;
            }
            customSpawnRegion.Deserialize(text);
            return false;
        }




        #endregion

        #endregion


        #region Private Methods


        private bool CheckVanillaManager()
        {
            if (safeVanillaManager.IsNullOrDestroyed())
            {
                return false;
            }
            return true;
        }


        private bool TryMatchSpawnRegion(SpawnRegion spawnRegion, out CustomBaseSpawnRegion customSpawnRegion)
        {
            return mCustomSpawnRegions.TryGetValue(spawnRegion.GetHashCode(), out customSpawnRegion);
        }


        private bool TryInjectCustomSpawnRegion(SpawnRegion spawnRegion, out CustomBaseSpawnRegion customSpawnRegion)
        {
            customSpawnRegion = null;
            if (spawnRegion == null)
            {
                LogTrace($"Null spawn region. cannot inject custom spawn region");
                return false;
            }
            if (mCustomSpawnRegions.TryGetValue(spawnRegion.GetHashCode(), out customSpawnRegion))
            {
                //LogTrace($"Previously matched spawn region with hash code {spawnRegion.GetHashCode()} and guid {customSpawnRegion.ModDataProxy.Guid}, skipping.");
                return false;
            }
            //NOTE: This SHOULD already be in place from vanilla! we never added this, and I dont see anything in spawnregion itself that adds this!
            if (!spawnRegion.TryGetComponent<ObjectGuid>(out ObjectGuid guid))
            {
                LogError($"Could not find ObjectGuid on spawn region with hashcode {spawnRegion.GetHashCode()}!");
                return false;
            }
            Guid wrapperGuid = new Guid(guid.PDID);
            customSpawnRegion = InjectCustomSpawnRegion(spawnRegion, wrapperGuid);
            return true;
        }


        private bool ProcessCaughtSpawnRegion(SpawnRegion spawnRegion,out CustomBaseSpawnRegion customSpawnRegion)
        {
            int hashcode = spawnRegion.GetHashCode();
            customSpawnRegion = null;
            if (mCustomSpawnRegions.ContainsKey(hashcode))
            {
                LogTrace($"Already started spawn region with hash code {hashcode}, aborting...");
                return false;
            }
            if (!TryInjectCustomSpawnRegion(spawnRegion, out customSpawnRegion))
            {
                LogError($"Could not inject custom base spawn region to wrap spawn region with hashcode {hashcode}!");
                return false;
            }
            LogTrace($"Successfully wrapped custom spawn region with hash code {hashcode}!");
            return true;
        }


        private void ClearCustomSpawnRegions()
        {
            foreach (CustomBaseSpawnRegion customSpawnRegion in mCustomSpawnRegions.Values)
            {
                TryRemoveCustomSpawnRegion(customSpawnRegion.VanillaSpawnRegion);
            }
            mCustomSpawnRegions.Clear();
            mCustomSpawnRegionsByGuid.Clear();
            mCustomSpawnRegionsByIndex.Clear();
        }


        private CustomBaseSpawnRegion InjectCustomSpawnRegion(SpawnRegion spawnRegion, Guid guid)
        {
            if (!mDataManager.TryGetUnmatchedSpawnRegionModDataProxy(guid, spawnRegion, out SpawnRegionModDataProxy matchedProxy))
            {
                LogTrace($"No spawn region mod data proxy matched to spawn region with hashcode {spawnRegion.GetHashCode()} and guid {guid}. creating then wrapping");
                matchedProxy = GenerateNewSpawnRegionModDataProxy(mManager.CurrentScene, spawnRegion, guid);
            }
            else
            {
                LogTrace($"Matched existing spawn region mod data proxy with guid {guid} against found spawn region!");
            }
            CustomBaseSpawnRegion newSpawnRegionWrapper = new CustomBaseSpawnRegion(spawnRegion, matchedProxy, mTimeOfDay);
            mCustomSpawnRegions.Add(spawnRegion.GetHashCode(), newSpawnRegionWrapper);
            mCustomSpawnRegionsByGuid.Add(matchedProxy.Guid, newSpawnRegionWrapper);
            mCustomSpawnRegionsByIndex.Add(newSpawnRegionWrapper);
            if (!mVanillaManager.m_SpawnRegions.Contains(spawnRegion))
            {
                mVanillaManager.m_SpawnRegions.Add(spawnRegion);
            }
            return newSpawnRegionWrapper;
        }


        private SpawnRegionModDataProxy GenerateNewSpawnRegionModDataProxy(string scene, SpawnRegion spawnRegion, Guid guid)
        {
            if (spawnRegion == null)
            {
                LogTrace($"Cant generate a new spawn mod data proxy without parent region!");
                return null;
            }
            if (mCustomSpawnRegions.TryGetValue(spawnRegion.GetHashCode(), out CustomBaseSpawnRegion customSpawnRegion))
            {
                LogTrace($"Spawn region with hash code {spawnRegion.GetHashCode()} already wrapped, cannot re-wrap!");
                return null;
            }
            SpawnRegionModDataProxy newProxy = new SpawnRegionModDataProxy(guid, scene, spawnRegion);
            if (!mDataManager.TryRegisterActiveSpawnRegionModDataProxy(newProxy))
            {
                LogTrace($"Couldnt register new spawn region mod data proxy with guid {newProxy.Guid} due to guid collision!");
                return null;
            }
            return newProxy;
        }

        #endregion
    }
}
