using Harmony;
using MelonLoader.TinyJSON;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2Cpp;
using Il2CppRewired.Utils;
using Il2CppTLD.PDID;
using MelonLoader.Utils;
using Il2CppTLD.AI;

namespace ExpandedAiFramework
{
    public sealed class SpawnRegionManager : BaseSubManager
    {
        #region Fields/Properties/Constructors


        private bool mPreLoading = false;
        private Il2Cpp.SpawnRegionManager mVanillaManager;

        private Dictionary<int, CustomSpawnRegion> mCustomSpawnRegions = new Dictionary<int, CustomSpawnRegion>();
        private Dictionary<Guid, CustomSpawnRegion> mCustomSpawnRegionsByGuid = new Dictionary<Guid, CustomSpawnRegion>();
        private List<CustomSpawnRegion> mCustomSpawnRegionsByIndex = new List<CustomSpawnRegion>();
        private DataManager mDataManager;
        private List<SpawnRegion> mSpawnRegionCatcher = new List<SpawnRegion>();
        private bool mReadyToProcessSpawnRegions = false;
        private Vector3 mPlayerStartPos;
        private HashSet<Guid> mPendingWrapOperations = new HashSet<Guid>();

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
        public Dictionary<int, CustomSpawnRegion> CustomSpawnRegions { get { return mCustomSpawnRegions; } }
        public Dictionary<Guid, CustomSpawnRegion> CustomSpawnRegionsByGuid { get { return mCustomSpawnRegionsByGuid; } }
        public bool ReadyToProcessSpawnRegions { get { return mReadyToProcessSpawnRegions; } }
        public bool PreLoading => mPreLoading;
        public Vector3 PlayerStartPos => mPlayerStartPos;

        #endregion


        #region BaseSubManager

        public override void Initialize(EAFManager manager, ISubManager[] subManagers)
        {
            base.Initialize(manager, subManagers);
            mDataManager = mManager.DataManager;
        }


        public override void OnStartNewGame()
        {
            if (!IsValidGameplayScene(mManager.CurrentScene, out _))
            {
                return;
            }
            mPreLoading = false;
            mManager.GameLoaded = true;
            //Lazy man's way of waiting for the slow computers to do their thing... maybe fix later lol
            Task.Run(() =>
            {
                Task.Delay(1000).Wait();
                DispatchManager.Instance.Dispatch(() =>
                {
                    PostDeserialize();
                });
            });
        }


        public override void OnLoadScene(string sceneName)
        {
            base.OnLoadScene(sceneName);
            ClearCustomSpawnRegions();
            mPreLoading = IsValidGameplayScene(sceneName, out _);
        }


        public override void Shutdown()
        {
            ClearCustomSpawnRegions();
            base.Shutdown();
        }


        public override void OnSaveGame()
        {
            base.OnSaveGame();
            foreach (CustomSpawnRegion region in mCustomSpawnRegions.Values)
            {
                region.Save();
            }
        }


        public override void Update()
        {
            base.Update();
            VanillaUpdate();
        }


        public override void OnQuitToMainMenu()
        {
            mPreLoading = false;
            ClearCustomSpawnRegions();
            base.OnQuitToMainMenu();
        }

        #endregion


        #region API

        public bool TryWrapNewSpawn(BaseAi baseAi, SpawnRegion spawnRegion, out CustomBaseAi newCustomBaseAi, SpawnModDataProxy proxy)
        {
            newCustomBaseAi = null;
            if (spawnRegion == null)
            {
                LogVerbose($"Null spawn region, can't intercept.");
                return false;
            }
            if (!mManager.AiManager.TryInjectCustomAi(baseAi, proxy.VariantSpawnType, spawnRegion, out newCustomBaseAi, proxy))
            {
                LogError($"Error in AiManager ai injection process while trying to respawn previously found ai variant in region with hash code {spawnRegion.GetHashCode()}!");
                return false;
            }
            LogVerbose($"Successfully wrapped a {proxy.VariantSpawnType.Name} with hash code {baseAi.GetHashCode()} with pre-queued proxy!");
            return true;
        }


        public bool TryRemoveCustomSpawnRegion(SpawnRegion spawnRegion)
        {
            if (spawnRegion == null)
            {
                return false;
            }
            if (!mCustomSpawnRegions.TryGetValue(spawnRegion.GetHashCode(), out CustomSpawnRegion customSpawnRegion))
            {
                return false;
            }
            customSpawnRegion.Save();
            //UnityEngine.Object.Destroy(customSpawnRegion.Self); won't be needed until (unless) we turn CustomBaseSpawnRegion into a ticking monobomb
            mCustomSpawnRegions.Remove(spawnRegion.GetHashCode());
            mVanillaManager.m_SpawnRegions.Remove(spawnRegion);
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


        #region CUT LIST - no longer supported with new single-list system 

        //Remove(SpawnRegion spawnRegion) ---> Despawn() just prior to Serialize


        #endregion


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
            DeserializeInternal(text);
            PostDeserialize();
        }


        private void DeserializeInternal(string text)
        {
            try
            {
                GameManager.GetPlayerManagerComponent().GetTeleportTransformAfterSceneLoad(out mPlayerStartPos, out _);
                LogTrace($"Deserializing Vanilla Data");
                if (!CheckVanillaManager())
                {
                    LogTrace("Can't get vanilla SpawnRegionManager");
                    return;
                }
                if (string.IsNullOrEmpty(text))
                {
                    LogTrace($"Null or empty deserialize text");
                    return;
                }
                SpawnRegionSaveList spawnRegionSaveList = Utils.DeserializeObject<SpawnRegionSaveList>(text);
                if (spawnRegionSaveList.IsNullOrDestroyed())
                {
                    LogError($"Could not deserialize text");
                    return;
                }
                Il2Cpp.SpawnRegionManager.m_NoPredatorSpawningInVoyageurHours = spawnRegionSaveList.m_NoPredatorSpawningInVoyageurHours;
                foreach (SpawnRegionSaveData data in spawnRegionSaveList.m_SerializedSpawnRegions)
                {
                    if (!mCustomSpawnRegionsByGuid.TryGetValue(new Guid(data.m_Guid), out CustomSpawnRegion customSpawnRegion))
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
                        LogTrace($"Wrapping spawn region during deserialize!");
                        WrapSpawnRegion(spawnRegion, new Guid(data.m_Guid));
                    }
                }
            }
            catch (Exception e)
            {
                LogError($"{e}");
            }
        }


        private void PostDeserialize()
        {
            try
            {
                LogTrace($"Processing caught spawn regions");
                foreach (SpawnRegion spawnRegion in mSpawnRegionCatcher)
                {
                    ProcessCaughtSpawnRegion(spawnRegion);
                }
                long startTime = DateTime.Now.Ticks;
                bool canContinue = false;
                while (!canContinue && DateTime.Now.Ticks <= startTime + 10000000)
                {
                    lock(mPendingWrapOperations)
                    {
                        canContinue = mPendingWrapOperations.Count == 0;
                    }
                }
                if (!canContinue)
                {
                    LogError($"Timeout on post deserialize waiting for spawn region processing!");
                }
                mSpawnRegionCatcher.Clear();
                LogAlways($"Prequeuing. We have {mCustomSpawnRegions.Values.Count} values in mCustomSpawnRegions!");
                foreach (CustomSpawnRegion customSpawnRegion in mCustomSpawnRegions.Values)
                {
                    customSpawnRegion.PreSpawn();
                }
            }
            catch (Exception e)
            {
                LogError($"{e}");
            }
            finally
            {
                mReadyToProcessSpawnRegions = true;
                mPreLoading = false;
                InterfaceManager.GetPanel<Panel_Loading>().Enable(false);
            }
        }


        public SpawnRegion FindSpawnRegionByGuid(string guid)
        {
            if (!CheckVanillaManager())
            {
                LogVerbose("Can't get vanilla SpawnRegionManager");
                return null;
            }
            if (mCustomSpawnRegionsByGuid.TryGetValue(new Guid(guid), out CustomSpawnRegion value) && !value.VanillaSpawnRegion.IsNullOrDestroyed())
            {
                LogVerbose($"Found EAF custom region");
                return value.VanillaSpawnRegion;
            }
            GameObject go = PdidTable.GetGameObject(guid);
            if (go.IsNullOrDestroyed())
            {
                LogVerbose($"Could not fetch object by guid");
                return null;
            }
            if (!go.TryGetComponent<SpawnRegion>(out SpawnRegion existingSpawnRegion))
            {
                LogError($"Could not fetch SpawnRegion from GUID-fetched object");
                return null;
            }
            LogVerbose($"Found spawn region by GUID");
            return existingSpawnRegion;
        }


        public SpawnRegion FindSpawnRegionByPosition(SpawnRegionSaveData saveData)
        {
            if (!CheckVanillaManager())
            {
                LogVerbose("Can't get vanilla SpawnRegionManager");
                return null;
            }
            for (int i = 0; i < mCustomSpawnRegionsByIndex.Count; i++)
            {
                SpawnRegion spawnRegion = mCustomSpawnRegionsByIndex[i].VanillaSpawnRegion;
                if (Vector3.Distance(spawnRegion.transform.position, saveData.m_Position) < 0.01f)
                {
                    LogVerbose($"Found spawn region by position");
                    return spawnRegion;
                }
            }
            LogVerbose($"Could not fetch spawn region by position");
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
            foreach (CustomSpawnRegion customBaseSpawnRegion in mCustomSpawnRegionsByIndex)
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
            foreach (CustomSpawnRegion customSpawnRegion in mCustomSpawnRegionsByIndex)
            {
                if (customSpawnRegion.VanillaSpawnRegion.IsNullOrDestroyed())
                {
                    LogWarning($"Null or destroyed SpawnRegion in mCustomSpawnRegionsByIndex");
                    continue;
                }
                customSpawnRegion.OnAuroraEnabled(enabled);
            }
        }


        //primarily used for getting closest active spawn?
        public SpawnRegion PointInsideActiveSpawnRegion(Vector3 pos, string filterSpawnablePrefabName)
        {
            return PointInsideSpawnRegionInternal(pos, filterSpawnablePrefabName, true);
        }


        //primarily used for getting closest active spawn?
        public SpawnRegion PointInsideSpawnRegion(Vector3 pos, string filterSpawnablePrefabName)
        {
            return PointInsideSpawnRegionInternal(pos, filterSpawnablePrefabName, false);
        }


        //primarily used for getting closest active spawn?
        private SpawnRegion PointInsideSpawnRegionInternal(Vector3 pos, string filterSpawnablePrefabName, bool checkActive)
        {
            if (!CheckVanillaManager())
            {
                LogTrace("Can't get vanilla SpawnRegionManager");
                return null;
            }
            bool shouldFilterByName = !string.IsNullOrEmpty(filterSpawnablePrefabName);
            foreach (CustomSpawnRegion customSpawnRegion in mCustomSpawnRegionsByIndex)
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


        //Should definitely be calling this ourselves
        public bool PointInsideNoSpawnRegion(Vector3 pos)
        {
            if (!CheckVanillaManager())
            {
                LogVerbose("Can't get vanilla SpawnRegionManager");
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
                    LogVerbose($"NoSpawnRegion too far, continuing");
                    continue;
                }
                if (!noSpawnRegion.gameObject.activeSelf)
                {
                    LogVerbose($"inactive spawnregion, continuing");
                    continue;
                }
                LogVerbose($"found containing NoSpawnRegion");
                return true;
            }
            LogVerbose($"Failed to find any containing NoSpawnRegion");
            return false;
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
            if (PreLoading)
            {
                LogTrace("Preloading...");
                return;
            }
            if (!CheckVanillaManager())
            {
                //LogTrace("Can't get vanilla SpawnRegionManager");
                return;
            }
            if (mCustomSpawnRegionsByIndex.Count <= 0)
            {
                LogVerbose($"Empty mCustomSpawnRegionsByIndex");
                return;
            }
            mVanillaManager.m_NextSpawnRegionIndexToUpdate++;
            if (mVanillaManager.m_NextSpawnRegionIndexToUpdate >= mCustomSpawnRegionsByIndex.Count)
            {
                mVanillaManager.m_NextSpawnRegionIndexToUpdate = 0;
            }
            //LogTrace($"Current index: {mVanillaManager.m_NextSpawnRegionIndexToUpdate} of {mVanillaManager.m_SpawnRegions.Count} total");
            CustomSpawnRegion spawnRegion = mCustomSpawnRegionsByIndex[mVanillaManager.m_NextSpawnRegionIndexToUpdate];
            if (spawnRegion.VanillaSpawnRegion.IsNullOrDestroyed())
            {
                LogTrace($"NullOrDestroyed SpawnRegion");
                return;
            }
            spawnRegion.MaybeReRollActive();

            if (spawnRegion.VanillaSpawnRegion.gameObject.activeInHierarchy)
            {
                spawnRegion.UpdateFromManager();
            }
            else if (ThreeDaysOfNight.IsActive())
            {
                if (!spawnRegion.VanillaSpawnRegion.transform.parent.IsNullOrDestroyed() 
                    && !spawnRegion.VanillaSpawnRegion.transform.parent.gameObject.activeInHierarchy)
                {
                    spawnRegion.VanillaSpawnRegion.transform.parent.gameObject.SetActive(true);
                }
                else
                {
                    spawnRegion.VanillaSpawnRegion.gameObject.SetActive(true);
                }
            }
        }

        #endregion


        #region SpawnRegion Vanilla Rerouting


        public bool TryGetClosestActiveSpawn(SpawnRegion __instance, Vector3 pos, ref GameObject __result)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomSpawnRegion customSpawnRegion))
            {
                return false;
            }
            __result = customSpawnRegion.GetClosestActiveSpawn(pos);
            return true;
        }


        public bool TryGetNumActiveSpawns(SpawnRegion __instance, ref int __result)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomSpawnRegion customSpawnRegion))
            {
                return false;
            }
            __result = customSpawnRegion.GetNumActiveSpawns();
            return true;
        }


        public bool TryGetWanderRegion(SpawnRegion __instance, Vector3 pos, ref WanderRegion __result)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomSpawnRegion customSpawnRegion))
            {
                return false;
            }
            __result = customSpawnRegion.GetWanderRegion(pos);
            return true;
        }
        
        public bool TryGetWaypointCircuit(SpawnRegion __instance, ref Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3> __result)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomSpawnRegion customSpawnRegion))
            {
                return false;
            }
            __result = customSpawnRegion.GetWaypointCircuit();
            return true;
        }


        public bool TrySetActive(SpawnRegion __instance, bool active)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomSpawnRegion customSpawnRegion))
            {
                return false;
            }
            customSpawnRegion.SetActive(active);
            return true;
        }


        public bool TryRemoveFromSpawnRegion(SpawnRegion __instance, BaseAi baseAi)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomSpawnRegion customSpawnRegion))
            {
                return false;
            }
            customSpawnRegion.RemoveFromSpawnRegion(baseAi);
            return true;
        }


        public bool TryOnAuroraEnabled(SpawnRegion __instance, bool enabled)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomSpawnRegion customSpawnRegion))
            {
                return false;
            }
            customSpawnRegion.OnAuroraEnabled(enabled);
            return true;
        }


        public bool TryMaybeReRollActive(SpawnRegion __instance)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomSpawnRegion customSpawnRegion))
            {
                return false;
            }
            customSpawnRegion.MaybeReRollActive();
            return true;
        }


        public bool TryAwake(SpawnRegion __instance)
        {
            if (!mReadyToProcessSpawnRegions && !mSpawnRegionCatcher.Contains(__instance))
            {
                mSpawnRegionCatcher.Add(__instance);
            }
            return true;
        }


        public bool TrySetRandomWaypointCircuit(SpawnRegion __instance)
        {
            if (!TryMatchSpawnRegion(__instance, out CustomSpawnRegion customSpawnRegion))
            {
                return false;
            }
            customSpawnRegion.SetRandomWaypointCircuit();
            return true;
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


        private bool TryMatchSpawnRegion(SpawnRegion spawnRegion, out CustomSpawnRegion customSpawnRegion)
        {
            return mCustomSpawnRegions.TryGetValue(spawnRegion.GetHashCode(), out customSpawnRegion);
        }


        private bool TryInjectCustomSpawnRegion(SpawnRegion spawnRegion)
        {
            if (spawnRegion == null)
            {
                LogWarning($"Null spawn region. cannot inject custom spawn region");
                return false;
            }
            if (mCustomSpawnRegions.ContainsKey(spawnRegion.GetHashCode()))
            {
                LogTrace($"Previously matched spawn region with hash code {spawnRegion.GetHashCode()}, skipping.");
                return false;
            }
            if (!spawnRegion.TryGetComponent<ObjectGuid>(out ObjectGuid guid))
            {
                LogError($"Could not find ObjectGuid on spawn region with hashcode {spawnRegion.GetHashCode()}!");
                return false;
            }
            Guid wrapperGuid = new Guid(guid.PDID);
            WrapSpawnRegion(spawnRegion, wrapperGuid);
            return true;
        }


        private bool ProcessCaughtSpawnRegion(SpawnRegion spawnRegion)
        {
            if (!TryInjectCustomSpawnRegion(spawnRegion))
            {
                return false;
            }
            LogTrace($"Successfully wrapped custom spawn region with hash code {spawnRegion.GetHashCode()}!");
            return true;
        }


        private void ClearCustomSpawnRegions()
        {
            LogTrace($"Clearing");
            foreach (CustomSpawnRegion customSpawnRegion in mCustomSpawnRegions.Values)
            {
                TryRemoveCustomSpawnRegion(customSpawnRegion.VanillaSpawnRegion);
            }
            mCustomSpawnRegions.Clear();
            mCustomSpawnRegionsByGuid.Clear();
            mCustomSpawnRegionsByIndex.Clear();
            mSpawnRegionCatcher.Clear();
            mReadyToProcessSpawnRegions = false;
        }


        private void WrapSpawnRegion(SpawnRegion spawnRegion, Guid guid)
        {
            lock (mPendingWrapOperations)
            {
                mPendingWrapOperations.Add(guid);
            }
            mDataManager.ScheduleSpawnRegionModDataProxyRequest(new GetDataByGuidRequest<SpawnRegionModDataProxy>(guid, mManager.CurrentScene, (proxy, result) =>
            {
                if (result == RequestResult.Succeeded)
                {
                    if (proxy.Connected)
                    {
                        EAFManager.LogWithStackTrace($"Trying to connect to an already-connected SpawnRegionModDataProxy with guid {guid}!"); 
                        lock (mPendingWrapOperations)
                        {
                            mPendingWrapOperations.Remove(guid);
                        }
                        return;
                    }
                    LogTrace($"Matched existing spawn region mod data proxy with guid {guid} against found spawn region!");
                }
                else
                {
                    LogTrace($"No spawn region mod data proxy matched to spawn region with hashcode {spawnRegion.GetHashCode()} and guid {guid}. creating then wrapping");
                    proxy = GenerateNewSpawnRegionModDataProxy(mManager.CurrentScene, spawnRegion, guid);
                }
                CustomSpawnRegion newSpawnRegionWrapper = GenerateCustomSpawnRegion(spawnRegion, proxy);
                mCustomSpawnRegions.Add(spawnRegion.GetHashCode(), newSpawnRegionWrapper);
                mCustomSpawnRegionsByGuid.Add(proxy.Guid, newSpawnRegionWrapper);
                mCustomSpawnRegionsByIndex.Add(newSpawnRegionWrapper);
                if (!mVanillaManager.m_SpawnRegions.Contains(spawnRegion))
                {
                    mVanillaManager.m_SpawnRegions.Add(spawnRegion);
                }
                lock (mPendingWrapOperations)
                {
                    mPendingWrapOperations.Remove(guid);
                }
                newSpawnRegionWrapper.Start();
            }));
        }


        private CustomSpawnRegion GenerateCustomSpawnRegion(SpawnRegion spawnRegion, SpawnRegionModDataProxy proxy)
        {
            //A lot of this was in / is repeated still in CustomBaseSpawnRegion.Initialize(); may be wiser to use an actual parent top object and delegate some logic to smaller serviec objects.
            if (spawnRegion.m_SpawnablePrefab.IsNullOrDestroyed())
            {
                LogVerbose($"null spawnable prefab on spawn region, fetching...");
                AssetReferenceAnimalPrefab animalReferencePrefab = spawnRegion.m_SpawnRegionAnimalTableSO.PickSpawnAnimal(WildlifeMode.Normal);
                spawnRegion.m_SpawnablePrefab = animalReferencePrefab.GetOrLoadAsset();
                animalReferencePrefab.ReleaseAsset();
            }
            if (!spawnRegion.m_SpawnablePrefab.TryGetComponent<BaseAi>(out BaseAi baseAi))
            {
                LogError($"Could not get base ai from spawn region!");
                return null;
            }
            if (baseAi.m_AiSubType == AiSubType.Wolf && !baseAi.NormalWolf.IsNullOrDestroyed())
            {
                return new BaseWolfSpawnRegion(spawnRegion, proxy, mTimeOfDay);
            }
            else
            {
                return new CustomSpawnRegion(spawnRegion, proxy, mTimeOfDay);
            }
        }


        private SpawnRegionModDataProxy GenerateNewSpawnRegionModDataProxy(string scene, SpawnRegion spawnRegion, Guid guid)
        {
            if (spawnRegion == null)
            {
                LogWarning($"Cant generate a new spawn mod data proxy without parent region!");
                return null;
            }
            if (mCustomSpawnRegions.ContainsKey(spawnRegion.GetHashCode()))
            {
                LogVerbose($"Spawn region with hash code {spawnRegion.GetHashCode()} already wrapped, cannot re-wrap");
                return null;
            }
            SpawnRegionModDataProxy newProxy = new SpawnRegionModDataProxy(guid, scene, spawnRegion);
            mDataManager.ScheduleRegisterSpawnRegionModDataProxyRequest(newProxy, (proxy, result) => { });
            return newProxy;
        }

        #endregion
    }
}
