using Il2Cpp;
using Il2CppRewired.Utils;
using Il2CppSWS;
using Il2CppTLD.AI;
using Il2CppTLD.Gameplay;
using Il2CppTLD.Gameplay.Tunable;
using UnityEngine;



namespace ExpandedAiFramework
{
    public class CustomSpawnRegion : ILogInfoProvider
    {
        protected SpawnRegion mSpawnRegion;
        protected TimeOfDay mTimeOfDay;
        protected SpawnRegionManager mManager;
        protected SpawnRegionModDataProxy mModDataProxy;
        protected DataManager mDataManager;
        protected List<CustomBaseAi> mActiveSpawns = new List<CustomBaseAi>();
        protected Queue<SpawnModDataProxy> mPendingSpawns = new Queue<SpawnModDataProxy>();
        protected int mProxiesUnderConstruction = 0;

        public SpawnRegionManager Manager { get { return mManager; } }
        public DataManager DataManager { get { return mDataManager; } }
        public SpawnRegion VanillaSpawnRegion { get { return mSpawnRegion; } }
        public SpawnRegionModDataProxy ModDataProxy { get { return mModDataProxy; } }
        public virtual string InstanceInfo { get { return mModDataProxy != null ? $"{mModDataProxy.Guid} of type {mModDataProxy.AiSubType}": !VanillaSpawnRegion.IsNullOrDestroyed() ? VanillaSpawnRegion.GetHashCode().ToString() : "NULL"; } }
        public virtual string TypeInfo { get { return GetType().Name; } }
        public List<CustomBaseAi> ActiveSpawns { get { return mActiveSpawns; } }


        public CustomSpawnRegion(SpawnRegion spawnRegion, SpawnRegionModDataProxy dataProxy, TimeOfDay timeOfDay)
        {
            Initialize(spawnRegion, dataProxy, timeOfDay);
        }


        public virtual void Initialize(SpawnRegion spawnRegion, SpawnRegionModDataProxy dataProxy, TimeOfDay timeOfDay)
        {
            mSpawnRegion = spawnRegion;
            mModDataProxy = dataProxy;
            mTimeOfDay = timeOfDay;
            mManager = EAFManager.Instance.SpawnRegionManager;
            mDataManager = mManager.Manager.DataManager;
            mSpawnRegion.m_Registered = true;

            mSpawnRegion.m_ElapsedHoursAtLastActiveReRoll = mModDataProxy?.ElapsedHoursAtLastActiveReRoll ?? 0f;
            mSpawnRegion.m_NumRespawnsPending = mModDataProxy?.NumRespawnsPending ?? 0;
            mSpawnRegion.m_ElapasedHoursNextRespawnAllowed = mModDataProxy?.ElapasedHoursNextRespawnAllowed ?? 0f;
            mSpawnRegion.m_NumTrapped = mModDataProxy?.NumTrapped ?? 0;
            mSpawnRegion.m_HoursNextTrapReset = mModDataProxy?.HoursNextTrapReset ?? 0f;
            mSpawnRegion.m_CurrentWaypointPathIndex = mModDataProxy?.CurrentWaypointPathIndex ?? 0;
            mSpawnRegion.m_WildlifeMode = mModDataProxy?.WildlifeMode ?? WildlifeMode.Normal;
            mSpawnRegion.m_HasBeenDisabledByAurora = mModDataProxy?.HasBeenDisabledByAurora ?? false;
            mSpawnRegion.m_WasActiveBeforeAurora = mModDataProxy?.WasActiveBeforeAurora ?? true;
            mSpawnRegion.m_CooldownTimerHours = mModDataProxy?.CooldownTimerHours ?? 0f;

            if (mSpawnRegion.m_SpawnablePrefab.IsNullOrDestroyed())
            {
                this.LogErrorInstanced($"Could not fetch spawnable prefab!");
                return;
            }
            if (!mSpawnRegion.m_SpawnablePrefab.TryGetComponent<BaseAi>(out BaseAi baseAi))
            {
                this.LogErrorInstanced($"Could not fetch baseai script from spawnable prefab");
                return;
            }
            if (GetSpawnablePrefabName() == string.Empty)
            {
                this.LogErrorInstanced($"Could not set spawnable prefab name!");
                return;
            }
            dataProxy.AiType = mSpawnRegion.m_AiTypeSpawned = baseAi.m_AiType;
            dataProxy.AiSubType = mSpawnRegion.m_AiSubTypeSpawned = baseAi.m_AiSubType;
            if (mSpawnRegion.m_AiSubTypeSpawned == AiSubType.Wolf)
            {
                dataProxy.WolfType = mSpawnRegion.m_WolfTypeSpawned = baseAi.Timberwolf.IsNullOrDestroyed() ? WolfType.Normal : WolfType.Timberwolf;
            }
            GameModeConfig gameModeConfig = ExperienceModeManager.s_CurrentGameMode;
            if (gameModeConfig.IsNullOrDestroyed())
            {
                this.LogErrorInstanced($"null GameModeConfig");
                return;
            }
            ExperienceMode currentExperienceMode = gameModeConfig.m_XPMode;
            if (currentExperienceMode.IsNullOrDestroyed())
            {
                this.LogErrorInstanced($"null ExperienceMode");
                return;
            }
            ExperienceModeManager experienceModeManager = GameManager.m_ExperienceModeManager;
            if (experienceModeManager.IsNullOrDestroyed())
            {
                this.LogErrorInstanced($"null ExperienceModeManager");
                return;
            }
            mSpawnRegion.m_SpawnLevel = currentExperienceMode.GetSpawnRegionLevel(baseAi.m_AiTag);
            float maxRespawnsPerDay = mSpawnRegion.m_DifficultySettings[(int)mSpawnRegion.m_SpawnLevel].m_MaxRespawnsPerDay;
            mSpawnRegion.m_NumHoursBetweenRespawns = Math.Abs(maxRespawnsPerDay) <= 0.0001 ? float.MaxValue : (24.0f / maxRespawnsPerDay);
            if (GameManager.InCustomMode())
            {
                CustomExperienceMode customMode = experienceModeManager.GetCustomMode();
                if (customMode.IsNullOrDestroyed())
                {
                    this.LogErrorInstanced($"Null CustomMode");
                    return;
                }
                mSpawnRegion.m_NumHoursBetweenRespawns *= customMode.m_LookupTable.m_WildlifeRespawnTimeModifierList.GetValue(customMode.m_WildlifeSpawnFrequency);
            }
            mSpawnRegion.m_PathManagers = (Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<PathManager>)mSpawnRegion.GetComponentsInChildren<PathManager>(false);
            mSpawnRegion.m_Den = mSpawnRegion.GetComponent<Den>();
            if (mModDataProxy != null && ModDataProxy.CurrentPosition != Vector3.zero)
            {
                mSpawnRegion.m_Center = mModDataProxy.CurrentPosition;
                mManager.Manager.DispatchManager.Dispatch(() =>
                {
                    mSpawnRegion.transform.position = mSpawnRegion.m_Center;
                });
            }
            else 
            {
                mSpawnRegion.m_Center = mSpawnRegion.transform.position;
            }
            if (mSpawnRegion.m_PathManagers == null)
            {
                return;
            }

            if (mSpawnRegion.m_PathManagers.Length != 0)
            {
                mSpawnRegion.SetRandomWaypointCircuit();
            }
            ActiveSpawns.Clear();
            if (dataProxy.Fresh)
            {
                this.LogDebugInstanced($"Fresh proxy, rerolling active chance", LogCategoryFlags.SpawnRegion);
                RerollChanceActive();
                dataProxy.IsActive = mSpawnRegion.gameObject.activeSelf;
            }
            else
            {
                this.LogDebugInstanced($"Not fresh proxy, deferring to IsActive state: {dataProxy.IsActive}", LogCategoryFlags.SpawnRegion);
                mSpawnRegion.gameObject.SetActive(dataProxy.IsActive);
            }


            SetBoundingSphereBasedOnWaypoints(mModDataProxy?.CurrentWaypointPathIndex ?? 0);
            PreQueue();
        }


        protected virtual void PreQueue()
        {
            bool closeEnoughForPrespawning = mSpawnRegion.m_Radius + GameManager.m_SpawnRegionManager.m_SpawnRegionDisableDistance >= Vector3.Distance(mManager.PlayerStartPos, mSpawnRegion.m_Center);
            mDataManager.SchedulePreQueueRequest(this, WildlifeMode.Normal, closeEnoughForPrespawning);
            if (mSpawnRegion.m_AiTypeSpawned == AiType.Predator)
            {
                mDataManager.SchedulePreQueueRequest(this, WildlifeMode.Aurora, closeEnoughForPrespawning);
            }
        }


        public virtual void Save()
        {
            mModDataProxy.Save(this);
        }


        public void PreSpawn()
        {
            if (mSpawnRegion.m_HasBeenDisabledByAurora)
            {
                return;
            }
            mDataManager.ScheduleSpawnModDataProxyRequest(new PreSpawnRequest(this), mSpawnRegion.m_WildlifeMode);
        }


        #region Self-Management


        private void SetBoundingSphereBasedOnWaypoints(int waypointIndex)
        {
            if (waypointIndex < 0 || waypointIndex >= (mSpawnRegion?.m_PathManagers?.Length ?? 0))
            {
                this.LogTraceInstanced($"Invalid waypoint index {waypointIndex} (waypoints available: {mSpawnRegion?.m_PathManagers?.Length ?? 0})", LogCategoryFlags.SpawnRegion);
                return;
            }
            Vector3[] pathPoints = mSpawnRegion.m_PathManagers[mSpawnRegion.m_CurrentWaypointPathIndex].GetPathPoints();
            BoundingSphereFromPoints.Calculate(pathPoints, pathPoints.Length, 0, 0);
            mSpawnRegion.m_Radius = BoundingSphereFromPoints.m_Radius;
            mSpawnRegion.m_Center = BoundingSphereFromPoints.m_Center;
            this.LogTraceInstanced($"Set bounding sphere to center {mSpawnRegion.m_Center} and radius {mSpawnRegion.m_Radius}", LogCategoryFlags.SpawnRegion);
        }


        public void SetRandomWaypointCircuit()
        {
            if (mSpawnRegion.m_PathManagers.Count <= 0)
            {
                this.LogTraceInstanced($"No pathmanagers, cannot set waypoint circuit", LogCategoryFlags.SpawnRegion);
                return;
            }
            mSpawnRegion.m_CurrentWaypointPathIndex = UnityEngine.Random.Range(0, mSpawnRegion.m_PathManagers.Count);
            this.LogTraceInstanced($"mSpawnRegion.m_CurrentWaypointIndex set to {mSpawnRegion.m_CurrentWaypointPathIndex}. Not, that this really matters... EAF uses its own wander system!", LogCategoryFlags.SpawnRegion);
        }


        public virtual void UpdateFromManager()
        {
            if (mSpawnRegion.m_HasBeenDisabledByAurora)
            {
                mSpawnRegion.gameObject.SetActive(false);
            }
            ProcessPendingSpawnQueue();
            MaybeReducePendingRespawns();
            MaybeReduceNumTrapped();
            AdjustActiveSpawnRegionPopulation();
        }

        #endregion


        #region OBSOLETE/NOT USED DUE TO INLINING

        #region Den/Sleep

        //Inlined within BaseAi.HandleLastWaypoint 
        public float GetDenSleepDurationInHours()
        {
            if (mSpawnRegion.m_Den.IsNullOrDestroyed())
            {
                this.LogDebugInstanced($"No den, no sleep duration", LogCategoryFlags.SpawnRegion);
                return 0.0f;
            }
            return GameManager.m_TimeOfDay.IsDay()
                ? UnityEngine.Random.Range(mSpawnRegion.m_Den.m_MinSleepHoursDay, mSpawnRegion.m_Den.m_MaxSleepHoursDay)
                : UnityEngine.Random.Range(mSpawnRegion.m_Den.m_MinSleepHoursNight, mSpawnRegion.m_Den.m_MaxSleepHoursNight);
        }


        //Inlined within BaseAi.HandleLastWaypoint 
        public bool ShouldSleepInDenAfterWaypointLoop()
        {
            if (mSpawnRegion.m_Den.IsNullOrDestroyed())
            {
                this.LogDebugInstanced($"Null den, no sleep", LogCategoryFlags.SpawnRegion);
                return false;
            }
            float sleepChance = GameManager.m_TimeOfDay.IsDay()
                ? mSpawnRegion.m_Den.m_ChanceSleepAfterWaypointsLoopDay
                : mSpawnRegion.m_Den.m_ChanceSleepAfterWaypointsLoopNight;
            bool shouldSleep = Utils.RollChance(sleepChance);
            this.LogDebugInstanced($"Rolling against chance to sleep of {sleepChance}: {shouldSleep}", LogCategoryFlags.SpawnRegion);
            return shouldSleep;
        }

        #endregion


        #region Trap

        //CanTrap: Inlined within SnareItem
        //DoTrap: Inlined within SnareItem

        #endregion

        #endregion


        #region Spawning Method Chain

        public void Spawn(WildlifeMode mode)
        {
            mDataManager.ScheduleSpawnModDataProxyRequest(new GetNextAvailableSpawnRequest(mModDataProxy.Guid, mModDataProxy.Scene, false, (availableProxy, result) =>
            {
                if (result == RequestResult.Succeeded)
                {
                    QueueSpawn(availableProxy);
                }
                else
                {
                    if (mProxiesUnderConstruction > 0)
                    {
                        this.LogDebugInstanced($"No queued spawns for spawn region with guid {mModDataProxy.Guid} for mode {mode}. Wait for pending proxies under construction.", LogCategoryFlags.SpawnRegion);
                        return;
                    }
                    this.LogDebugInstanced($"No queued spawns for spawn region with guid {mModDataProxy.Guid} for mode {mode}. Queueing request for new proxy and spawn...", LogCategoryFlags.SpawnRegion);
                    GenerateNewRandomSpawnModDataProxy((s) =>
                    {
                        QueueSpawn(s);
                    }, mode, true);
                }
            }, false), mode);
        }


        public void QueueSpawn(SpawnModDataProxy proxy)
        {
            lock(mPendingSpawns)
            {
                if (!proxy.Available)
                {
                    this.LogWarningInstanced($"Proxy {proxy.Guid} unavailable", LogCategoryFlags.SpawnRegion);
                    return;
                }
                if (mPendingSpawns.Contains(proxy))
                {
                    this.LogWarningInstanced($"Attempting to double-spawn proxy, aborting!", LogCategoryFlags.SpawnRegion);
                    return;
                }
                for (int i = 0, iMax = mActiveSpawns.Count; i < iMax; i++)
                {
                    if (mActiveSpawns[i].ModDataProxy == proxy)
                    {
                        this.LogWarningInstanced($"Attempting to double-spawn proxy, aborting!", LogCategoryFlags.SpawnRegion);
                        return;
                    }
                }
                proxy.Available = false;
                this.LogTraceInstanced($"Setting proxy with guid {proxy.Guid} to UNAVAILABLE", LogCategoryFlags.SpawnRegion);
                mPendingSpawns.Enqueue(proxy);
            }
        }


        private void ProcessPendingSpawnQueue()
        {
            lock (mPendingSpawns)
            {
                while (mPendingSpawns.Count > 0)
                {
                    InstantiateSpawnAndSetUnavailable(mPendingSpawns.Dequeue());
                }
            }
        }

        protected void InstantiateSpawnAndSetUnavailable(SpawnModDataProxy modDataProxy)
        {
            CustomBaseAi customBaseAi = InstantiateSpawn(modDataProxy);
            modDataProxy.Available = customBaseAi == null;
        }


        protected CustomBaseAi InstantiateSpawn(SpawnModDataProxy modDataProxy)
        {
            if (!modDataProxy.ForceSpawn && !ValidSpawn(modDataProxy))
            {
                return null;
            }
            CustomBaseAi customBaseAi = InstantiateSpawnInternal(modDataProxy);
            if (!PostProcessInstantiatedSpawn(customBaseAi))
            {
                return null;
            }
            if (customBaseAi != null)
            {
                modDataProxy.Spawned = true; //Ensure that it sticks around until the AI perishes
                mDataManager.ScheduleSpawnModDataProxyRequest(new ClaimAvailableSpawnRequest(modDataProxy.Guid, modDataProxy.Scene, null, false), modDataProxy.WildlifeMode);
            } 
            return customBaseAi;
        }


        private CustomBaseAi InstantiateSpawnInternal(SpawnModDataProxy modDataProxy)
        {
            GameObject spawnablePrefab = mSpawnRegion.m_SpawnablePrefab;
            if (mSpawnRegion.m_AuroraSpawnablePrefab != null && modDataProxy.WildlifeMode == WildlifeMode.Aurora)
            {
                this.LogTraceInstanced($"Wildlife mode is aurora and aurora spawnable prefab available, overriding param prefab", LogCategoryFlags.SpawnRegion);
                spawnablePrefab = mSpawnRegion.m_AuroraSpawnablePrefab;
            }
            GameObject newInstance = GameObject.Instantiate(spawnablePrefab, modDataProxy.CurrentPosition, modDataProxy.CurrentRotation);
            if (!newInstance.TryGetComponent<BaseAi>(out BaseAi newBaseAi))
            {
                this.LogErrorInstanced($"Cannot extract BaseAi component from newly instantiated BaseAi spawnable prefab!");
                return null;
            }
            newInstance.name = spawnablePrefab.name + $"_{mSpawnRegion.m_AutoCloneIndex}";
            mSpawnRegion.m_AutoCloneIndex++;
            if (newInstance.TryGetComponent<PackAnimal>(out PackAnimal newPackAnimal))
            {
                //eww
                newPackAnimal.gameObject.tag = mSpawnRegion.m_PackGroupId;
            }
            if (!mManager.TryWrapNewSpawn(newBaseAi, mSpawnRegion, out CustomBaseAi newCustomBaseAi, modDataProxy))
            {
                this.LogErrorInstanced($"Error wrapping new spawn!");
                return null;
            }
            return newCustomBaseAi;
        }


        private bool PostProcessInstantiatedSpawn(CustomBaseAi customBaseAi)
        {
            BaseAi baseAi = customBaseAi.BaseAi;
            if (customBaseAi.IsNullOrDestroyed())
            {
                this.LogWarningInstanced($"InstantiateSpawnInternal returned null BaseAi, aborting", LogCategoryFlags.SpawnRegion);
                return false;
            }
            Transform transform = mSpawnRegion.transform;
            if (mSpawnRegion.m_WanderRegion != null)
            {
                this.LogTraceInstanced($"Wander region found, setting move agent transform to wander region?", LogCategoryFlags.SpawnRegion);
                transform = mSpawnRegion.m_WanderRegion.transform;
            }
            if (BaseAiManager.CreateMoveAgent(transform, baseAi, customBaseAi.ModDataProxy.CurrentPosition))
            {
                baseAi.ReparentBaseAi(transform, true);
            }
            baseAi.SetSpawnRegionParent(mSpawnRegion);
            AiDifficultySettings aiDifficultySettings = GameManager.m_AiDifficultySettings;
            if (aiDifficultySettings.IsNullOrDestroyed())
            {
                this.LogErrorInstanced($"Null AiDifficultySettings, aborting");
                return false;
            }
            baseAi.m_AiDifficultySetting = aiDifficultySettings.GetSetting(mSpawnRegion.m_AiDifficulty, baseAi.m_AiSubType);
            ObjectGuid.MaybeAttachObjectGuidAndRegister(baseAi.gameObject, customBaseAi.ModDataProxy.Guid.ToString());
            baseAi.Deserialize(customBaseAi.ModDataProxy.BaseAiSerialized);
            lock (mActiveSpawns)
            {
                mActiveSpawns.Add(customBaseAi);
            }
            return true;
        }




        #endregion


        #region SpawnModDataProxy Generation

        public void GenerateNewRandomSpawnModDataProxy(Action<SpawnModDataProxy> callback, WildlifeMode wildlifeMode, bool async = true)
        {
            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;
            if (!TryGetSpawnPositionAndRotation(ref spawnPosition, ref spawnRotation))
            {
                this.LogWarningInstanced($"Potential error: Could not get spawn position and rotation. Aborting", LogCategoryFlags.SpawnRegion);
                return;
            }
            // first priority: spawn region
            Type spawnType = OverrideSpawnType();

            // second priority: submanager array
            if (spawnType == typeof(void))
            {
                foreach (ISpawnManager subManager in mManager.Manager.EnumerateSpawnManagers())
                {
                    if (subManager.ShouldInterceptSpawn(this))
                    {
                        this.LogTraceInstanced($"Spawn intercept from submanager {subManager}! new type: {subManager.SpawnType}", LogCategoryFlags.SpawnRegion);
                        spawnType = subManager.SpawnType;
                        break;
                    }
                }
            }
            // third priority: random spawn picker
            if (spawnType == typeof(void))
            {
                BaseAi spawnableAi = mSpawnRegion.m_SpawnablePrefab.GetComponent<BaseAi>();
                if (spawnableAi == null)
                {
                    //PANIK!!1
                    this.LogErrorInstanced($"Could not get spawnable Ai for type picker! Aborting");
                    return;
                }
                spawnableAi.m_WildlifeMode = wildlifeMode;
                this.LogTraceInstanced($"No submanager interceptions, attempting to randomly pick a valid spawn type...", LogCategoryFlags.SpawnRegion);
                if (async)
                {
                    this.LogTraceInstanced($"Async spawn mod data proxy generation, mProxiesUnderConstruction incremented to {mProxiesUnderConstruction + 1}", LogCategoryFlags.SpawnRegion);
                    mProxiesUnderConstruction++;
                    mManager.Manager.TypePicker.PickTypeAsync(spawnableAi, (type) =>
                    {
                        this.LogTraceInstanced($"Async spawn mod data proxy generation, mProxiesUnderConstruction decremented to {mProxiesUnderConstruction - 1}", LogCategoryFlags.SpawnRegion);
                        mProxiesUnderConstruction--;
                        callback.Invoke(GenerateNewSpawnModDataProxy(type, wildlifeMode, spawnPosition, spawnRotation));
                    });
                    return;
                }
                else
                {
                    spawnType = mManager.Manager.TypePicker.PickType(spawnableAi);
                }
            }
            callback.Invoke(GenerateNewSpawnModDataProxy(spawnType, wildlifeMode, spawnPosition, spawnRotation));
        }

        protected virtual Type OverrideSpawnType() => typeof(void);

        private SpawnModDataProxy GenerateNewSpawnModDataProxy(Type variantSpawnType, WildlifeMode wildlifeMode, Vector3 position, Quaternion rotation)
        {
            if (variantSpawnType == null)
            {
                this.LogErrorInstanced($"Can't generate new spawn mod data proxy with null variant spawn type!");
                return null;
            }
            SpawnModDataProxy newProxy = new SpawnModDataProxy(Guid.NewGuid(), mManager.Manager.CurrentScene, position, rotation, mSpawnRegion.m_AiSubTypeSpawned, wildlifeMode, variantSpawnType);
            newProxy.ParentGuid = mModDataProxy.Guid;
            mDataManager.ScheduleRegisterSpawnModDataProxyRequest(newProxy, (proxy, result) =>
            {
                mManager.Manager.PostProcessNewSpawnModDataProxy(newProxy);
                if (newProxy.ForceSpawn && newProxy.WildlifeMode == VanillaSpawnRegion.m_WildlifeMode)
                {
                    this.LogDebugInstanced($"FORCE spawning on creation!", LogCategoryFlags.SpawnRegion);
                    mManager.Manager.DataManager.IncrementForceSpawnCount(wildlifeMode);
                    QueueSpawn(newProxy);
                }
            });
            return newProxy;
        }


        #endregion


        #region Spawn Population Control 


        private void AdjustActiveSpawnRegionPopulation()
        {
            WildlifeMode currentMode = mSpawnRegion.m_WildlifeMode;
            WildlifeMode oppositeMode = currentMode == WildlifeMode.Normal ? WildlifeMode.Aurora : WildlifeMode.Normal;
            int targetPop = CalculateTargetPopulation();
            int currentActivePopulation = GetCurrentActivePopulation(currentMode);
            int otherModeActivePopulation = GetCurrentActivePopulation(oppositeMode);
            if (otherModeActivePopulation > 0)
            {
                this.LogDebugInstanced($"{otherModeActivePopulation} active wildlife of opposite type, removing", LogCategoryFlags.SpawnRegion);
                RemoveActiveSpawns(otherModeActivePopulation, currentMode, true);
            }
            int targetDelta = targetPop - currentActivePopulation;
            if (targetDelta > 0)
            {
                if (mSpawnRegion.m_HasBeenDisabledByAurora)
                {
                    this.LogTraceInstanced($"Disabled by aurora, aborting", LogCategoryFlags.SpawnRegion);
                    return;
                }
                if (!SpawnRegionCloseEnoughForSpawning())
                {
                    this.LogTraceInstanced($"Not close enough for spawning, aborting", LogCategoryFlags.SpawnRegion);
                    return;
                }
                if (targetDelta <= 0)
                {
                    this.LogTraceInstanced($"numToActivate ({targetDelta}) invalid, aborting", LogCategoryFlags.SpawnRegion);
                    return;
                }
                this.LogDebugInstanced($"{targetDelta} ({currentActivePopulation} vs {targetPop}) missing active wildlife of current type, adding", LogCategoryFlags.SpawnRegion);
                Spawn(currentMode);
            }
            else if (targetDelta < 0)
            {
                this.LogDebugInstanced($"{-targetDelta} ({currentActivePopulation} vs {targetPop}) excess active wildlife of current type, removing", LogCategoryFlags.SpawnRegion);
                RemoveActiveSpawns(-targetDelta, currentMode, false);
            }
        }


        public int CalculateTargetPopulation()
        {
            if (!OverrideCalculateTargetPopulation(out int customTarget))
            {
                this.LogTraceInstanced($"Spawning target overridden to {customTarget}!", LogCategoryFlags.SpawnRegion);
                return customTarget;
            }
            if (SpawningSuppressedByExperienceMode())
            {
                this.LogTraceInstanced($"Spawning suppressed by experience mode", LogCategoryFlags.SpawnRegion);
                return 0;
            }
            if (!mSpawnRegion.m_CanSpawnInBlizzard && GameManager.m_Weather.IsBlizzard())
            {
                this.LogTraceInstanced($"Cannot spawn in blizzard", LogCategoryFlags.SpawnRegion);
                return 0;
            }
            return CalculateTargetPopulationInternal();
        }

        
        protected virtual bool OverrideCalculateTargetPopulation(out int customTarget)
        {
            customTarget = 0;
            return true;
        }


        private int CalculateTargetPopulationInternal()
        {
            int maxSimultaneousSpawns = GameManager.m_TimeOfDay.IsDay()
                ? GetMaxSimultaneousSpawnsDay()
                : GetMaxSimultaneousSpawnsNight();
            maxSimultaneousSpawns -= mSpawnRegion.m_NumTrapped;
            maxSimultaneousSpawns -= mSpawnRegion.m_NumRespawnsPending;
            return Math.Max(maxSimultaneousSpawns, 0);
        }

        protected virtual int AdditionalSimultaneousSpawnAllowance() => 0;


        public int GetCurrentActivePopulation(WildlifeMode wildlifeMode)
        {
            int count = 0;
            lock (mActiveSpawns)
            {
                for (int i = 0, iMax = mActiveSpawns.Count; i < iMax; i++)
                {
                    if (mActiveSpawns[i].BaseAi.m_WildlifeMode != wildlifeMode)
                    {
                        continue;
                    }
                    if (!mActiveSpawns[i].gameObject.activeSelf)
                    {
                        continue;
                    }
                    count++;
                }
            }
            return count;
        }


        public virtual int GetMaxSimultaneousSpawnsDay()
        {
            if (mSpawnRegion.m_DifficultySettings == null)
            {
                this.LogErrorInstanced($"Null mSpawnRegion.m_DifficultySettings");
                return 0;
            }
            return mSpawnRegion.m_DifficultySettings[(int)mSpawnRegion.m_SpawnLevel].m_MaxSimultaneousSpawnsDay + AdditionalSimultaneousSpawnAllowance();
        }


        public virtual int GetMaxSimultaneousSpawnsNight()
        {
            if (mSpawnRegion.m_DifficultySettings == null)
            {
                this.LogErrorInstanced($"Null mSpawnRegion.m_DifficultySettings");
                return 0;
            }
            return mSpawnRegion.m_DifficultySettings[(int)mSpawnRegion.m_SpawnLevel].m_MaxSimultaneousSpawnsNight + AdditionalSimultaneousSpawnAllowance();
        }


        public int GetNumActiveSpawns()
        {
            if (mSpawnRegion.gameObject.IsNullOrDestroyed())
            {
                this.LogErrorInstanced($"Null mSpawnRegion.gameObject; this is an ACTUAL error!");
                return 0;
            }
            if (!mSpawnRegion.gameObject.activeSelf || !mSpawnRegion.isActiveAndEnabled)
            {
                return 0;
            }
            return GetCurrentActivePopulation(mSpawnRegion.m_WildlifeMode);
        }


        private void RemoveActiveSpawns(int numToDeactivate, WildlifeMode wildlifeMode, bool isAdjustingOtherWildlifeMode)
        {
            bool playerGhost = GameManager.m_PlayerManager.m_Ghost;
            lock (mActiveSpawns)
            {
                for (int i = 0, iMax = mActiveSpawns.Count; i < iMax && numToDeactivate > 0; i++)
                {
                    BaseAi spawn = mActiveSpawns[i].BaseAi;
                    if (spawn.IsNullOrDestroyed() || !spawn.gameObject.activeSelf)
                    {
                        continue;
                    }
                    GameManager.GetPackManager().UnregisterPackAnimal(spawn.m_PackAnimal, onDeath: false);
                    //force wildlife to run until they are eligible for removal
                    bool canDespawn = false;
                    if (isAdjustingOtherWildlifeMode && HasSameWildlifeMode(spawn, wildlifeMode))
                    {
                        this.LogTraceInstanced($"Adjusting other wildlife mode and spawn mode matches called mode, wildlifeMode matched for despawn", LogCategoryFlags.SpawnRegion);
                        canDespawn = true;
                    }
                    if (!canDespawn && !HasSameWildlifeMode(spawn, wildlifeMode))
                    {
                        this.LogTraceInstanced($"NOT adjusting other wildlife mode and spawn mode does NOT match called mode, wildlifeMode matched for despawn", LogCategoryFlags.SpawnRegion);
                        canDespawn = true;
                    }
                    //if (canDespawn
                    //    && spawn.GetAiMode() != AiMode.Flee
                    //    && spawn.GetAiMode() != AiMode.Dead)
                    //{
                    //    this.LogTraceInstanced($"Can despawn && and spawn is not fleeing or dead, setting flee", LogCategoryFlags.SpawnRegion);
                    //    spawn.SetAiMode(AiMode.Flee);
                    //}
                    Vector3 spawnPos = spawn.m_CachedTransform.position;
                    bool canDespawnDueToProximity = playerGhost;
                    if (canDespawnDueToProximity)
                    {
                        this.LogTraceInstanced($"Ghost, proximity check passed", LogCategoryFlags.SpawnRegion);
                    }
                    if (!canDespawnDueToProximity
                        && Utils.DistanceToMainCamera(spawnPos) >= GameManager.GetSpawnRegionManager().m_DisallowDespawnBelowDistance
                        && (!Utils.PositionIsOnscreen(spawnPos) || Utils.DistanceToMainCamera(spawnPos) >= GameManager.GetSpawnRegionManager().m_AllowDespawnOnscreenDistance)
                        && !Utils.PositionIsInLOSOfPlayer(spawnPos)) //Why is this last one needed...?
                    {
                        this.LogTraceInstanced($"Ai is not visible, proximity check passed", LogCategoryFlags.SpawnRegion);
                        canDespawnDueToProximity = true;
                    }
                    if (!canDespawnDueToProximity)
                    {
                        this.LogTraceInstanced($"Proximity check failed, cannot despawn", LogCategoryFlags.SpawnRegion);
                        continue;
                    }
                    if (!canDespawn)
                    {
                        if (!spawn.m_CurrentTarget.IsNullOrDestroyed() && spawn.m_CurrentTarget.IsPlayer())
                        {
                            this.LogTraceInstanced($"Failed to match wildlifeMode for forced removal and spawn is targetting player, cannot despawn", LogCategoryFlags.SpawnRegion);
                            continue;
                        }
                        if (spawn.m_CurrentMode == AiMode.Feeding)
                        {
                            this.LogTraceInstanced($"Failed to match wildlifeMode for forced removal and spawn is eating, cannot despawn", LogCategoryFlags.SpawnRegion);
                            continue;
                        }
                        if (spawn.m_CurrentMode == AiMode.Sleep)
                        {
                            this.LogTraceInstanced($"Failed to match wildlifeMode for forced removal and spawn is sleeping, cannot despawn", LogCategoryFlags.SpawnRegion);
                            continue;
                        }
                        if (spawn.IsBleedingOut())
                        {
                            this.LogTraceInstanced($"Failed to match wildlifeMode for forced removal and spawn is bleeding, cannot despawn", LogCategoryFlags.SpawnRegion);
                            continue;
                        }
                        if (!spawn.NormalWolf.IsNullOrDestroyed() && spawn.m_CurrentMode == AiMode.WanderPaused)
                        {
                            this.LogTraceInstanced($"Failed to match wildlifeMode for forced removal and spawn is normal wolf in AiMode.WanderPaused, cannot despawn", LogCategoryFlags.SpawnRegion);
                            continue;
                        }
                        if (mActiveSpawns[i].ModDataProxy != null)
                        {
                            // ensure despawned entities get saved to moddata on despawn, otherwise we will lose out on position data later on
                            mActiveSpawns[i].ModDataProxy.Save(mActiveSpawns[i]);
                        }
                        spawn.Despawn();
                        numToDeactivate--;
                    }
                }
            }
        }


        public void RemoveFromSpawnRegion(BaseAi baseAi)
        {
            int removeIndex = -1;
            lock (mActiveSpawns)
            {
                for (int i = 0, iMax = mActiveSpawns.Count; i < iMax; i++)
                {
                    if (mActiveSpawns[i].BaseAi == baseAi)
                    {
                        removeIndex = i;
                        break;
                    }
                }
                if (removeIndex == -1)
                {
                    this.LogErrorInstanced($"BaseAI not found in ActiveSpawns");
                    return;
                }
                mActiveSpawns[removeIndex].ModDataProxy.Disconnected = true;
                mActiveSpawns.RemoveAt(removeIndex);
                mSpawnRegion.m_NumRespawnsPending++;
                mSpawnRegion.m_ElapasedHoursNextRespawnAllowed = GetCurrentTimelinePoint() + GetNumHoursBetweenRespawns();
            }
        }



        #endregion


        #region Region Activity Control

        private bool CanDoReRoll()
        {
            if (mSpawnRegion.m_ControlledByRandomSpawner)
            {
                return false;
            }   
            return GetNumActiveSpawns() == 0;
        }


        private float GetCustomSpawnRegionChanceActiveScale()
        {
            CustomExperienceMode customMode = GameManager.GetCustomMode();
            this.LogTraceInstanced($"GetCustomSpawnRegionChanceActiveScale triggered");
            if (customMode.IsNullOrDestroyed())
            {
                this.LogErrorInstanced($"No custom mode found");
                return 1.0f;
            }
            CustomExperienceModeTunableLookupTable lookupTable = customMode.m_LookupTable;
            if (lookupTable.IsNullOrDestroyed())
            {
                this.LogErrorInstanced($"No custom mode lookup table found");
                return 1.0f;
            }
            Il2CppSystem.Collections.Generic.List<ExperienceMode> experienceModes = lookupTable.m_BaseExperienceModes;
            if (experienceModes.IsNullOrDestroyed())
            {
                this.LogErrorInstanced($"No base experience mode table found");
                return 1.0f;
            }
            if (experienceModes.Count < 4)
            {
                this.LogErrorInstanced($"Cannot fetch all base experience modes");
                return 1.0f;
            }
            ExperienceMode pilgrimExperienceMode = experienceModes[0];
            ExperienceMode voyagerExperienceMode = experienceModes[1];
            ExperienceMode stalkerExperienceMode = experienceModes[2];
            ExperienceMode interloperExperienceMode = experienceModes[3];
            CustomTunableNLMHV spawnChance = CustomTunableNLMHV.None;
            switch (mSpawnRegion.m_AiSubTypeSpawned)
            {
                case AiSubType.Wolf: 
                    spawnChance = customMode.GetWolfSpawnChance(mSpawnRegion.m_WolfTypeSpawned);
                    this.LogTraceInstanced($"Wolf ({mSpawnRegion.m_WolfTypeSpawned}) CustomTunableNLMHV spawn chance from custom mode {customMode}: {spawnChance}", LogCategoryFlags.SpawnRegion);
                    break;
                case AiSubType.Bear:
                    spawnChance = customMode.m_BearSpawnChance;
                    this.LogTraceInstanced($"Bear spawn CustomTunableNLMHV chance from custom mode {customMode}: {spawnChance}", LogCategoryFlags.SpawnRegion);
                    break;
                case AiSubType.Stag:
                    spawnChance = customMode.m_DeerSpawnChance;
                    this.LogTraceInstanced($"Stag spawn CustomTunableNLMHV chance from custom mode {customMode}: {spawnChance}", LogCategoryFlags.SpawnRegion);
                    break;
                case AiSubType.Rabbit:
                    spawnChance = customMode.m_RabbitSpawnChance;
                    this.LogTraceInstanced($"Rabbit spawn CustomTunableNLMHV chance from custom mode {customMode}: {spawnChance}", LogCategoryFlags.SpawnRegion);
                    break;
                case AiSubType.Moose:
                    spawnChance = customMode.m_MooseSpawnChance;
                    this.LogTraceInstanced($"Moose spawn CustomTunableNLMHV chance from custom mode {customMode}: {spawnChance}", LogCategoryFlags.SpawnRegion);
                    break;
                case AiSubType.Cougar:
                    this.LogTraceInstanced($"Cougar 100% spawn chance", LogCategoryFlags.SpawnRegion);
                    return 1.0f;
            }
            if (spawnChance == CustomTunableNLMHV.None)
            {
                this.LogTraceInstanced($"Zero spawn chance triggered");
                return 0.0f;
            }
            float activeChance = 0.0f;
            if (mSpawnRegion.m_AiTypeSpawned == AiType.Ambient)
            {
                switch (spawnChance)
                {
                    case CustomTunableNLMHV.Low: 
                        activeChance = interloperExperienceMode.m_SpawnRegionChanceActiveScale;
                        this.LogTraceInstanced($"{customMode} on ambient spawn, resulting chance active from interloper experience mode: {activeChance}", LogCategoryFlags.SpawnRegion);
                        break;
                    case CustomTunableNLMHV.Medium: 
                        activeChance = stalkerExperienceMode.m_SpawnRegionChanceActiveScale;
                        this.LogTraceInstanced($"{customMode} on ambient spawn, resulting chance active from stalker experience mode: {activeChance}", LogCategoryFlags.SpawnRegion);
                        break;
                    case CustomTunableNLMHV.High: 
                        activeChance = voyagerExperienceMode.m_SpawnRegionChanceActiveScale;
                        this.LogTraceInstanced($"{customMode} on ambient spawn, resulting chance active from voyager experience mode: {activeChance}", LogCategoryFlags.SpawnRegion);
                        break;
                    case CustomTunableNLMHV.VeryHigh: 
                        activeChance = pilgrimExperienceMode.m_SpawnRegionChanceActiveScale;
                        this.LogTraceInstanced($"{customMode} on ambient spawn, resulting chance active from pilgrim experience mode: {activeChance}", LogCategoryFlags.SpawnRegion);
                        break;
                    default: 
                        activeChance = 1.0f;
                        this.LogTraceInstanced($"(fallback) on ambient spawn, 100% active change", LogCategoryFlags.SpawnRegion);
                        break;
                }
            }
            else
            {
                switch (spawnChance)
                {
                    case CustomTunableNLMHV.Low: 
                        activeChance = pilgrimExperienceMode.m_SpawnRegionChanceActiveScale;
                        this.LogTraceInstanced($"{customMode} on predator spawn, resulting chance active from pilgrim experience mode: {activeChance}", LogCategoryFlags.SpawnRegion);
                        break;
                    case CustomTunableNLMHV.Medium: 
                        activeChance = voyagerExperienceMode.m_SpawnRegionChanceActiveScale;
                        this.LogTraceInstanced($"{customMode} on predator spawn, resulting chance active from voyager experience mode: {activeChance}", LogCategoryFlags.SpawnRegion);
                        break;
                    case CustomTunableNLMHV.High: 
                        activeChance = interloperExperienceMode.m_SpawnRegionChanceActiveScale;
                        this.LogTraceInstanced($"{customMode} on predator spawn, resulting chance active from interloper experience mode: {activeChance}", LogCategoryFlags.SpawnRegion);
                        break;
                    case CustomTunableNLMHV.VeryHigh: 
                        activeChance = stalkerExperienceMode.m_SpawnRegionChanceActiveScale;
                        this.LogTraceInstanced($"{customMode} on predator spawn, resulting chance active from stalker experience mode: {activeChance}", LogCategoryFlags.SpawnRegion);
                        break;
                    default:
                        activeChance = 1.0f;
                        this.LogTraceInstanced($"(fallback) on ambient spawn, 100% active change", LogCategoryFlags.SpawnRegion);
                        break;
                }
            }
            return activeChance;
        }


        public void MaybeReRollActive()
        {
            if (mSpawnRegion.m_HasBeenDisabledByAurora)
            {
                this.LogTraceInstanced($"Disabled by aurora, aborting", LogCategoryFlags.SpawnRegion);
                return;
            }
            if (mSpawnRegion.m_AiSubTypeSpawned == AiSubType.Cougar)
            {
                this.LogTraceInstanced($"Cougar override", LogCategoryFlags.SpawnRegion);
                return;
            }
            if (mSpawnRegion.m_WasForceDisabled)
            {
                this.LogTraceInstanced($"Force disabled", LogCategoryFlags.SpawnRegion);
                return;
            }
            if (mSpawnRegion.m_HoursReRollActive <= 0.0001f)
            {
                this.LogTraceInstanced($"Effectively zero mSpawnRegion.m_HoursReRollActive, aborting to prevent div by near zero", LogCategoryFlags.SpawnRegion);
                return;
            }
            if (GetCurrentTimelinePoint() - mSpawnRegion.m_ElapsedHoursAtLastActiveReRoll < mSpawnRegion.m_HoursReRollActive)
            {
                this.LogTraceInstanced($"Not yet time", LogCategoryFlags.SpawnRegion);
                return;
            }
            if (!CanDoReRoll())
            {
                this.LogTraceInstanced($"Ineligible for ReRoll", LogCategoryFlags.SpawnRegion);
                return;
            }
            RerollChanceActive();
            mSpawnRegion.m_ElapsedHoursAtLastActiveReRoll = GetCurrentTimelinePoint();
        }


        private void RerollChanceActive()
        {
            float chanceActive = mSpawnRegion.m_ChanceActive;
            chanceActive *= GameManager.InCustomMode()
                            ? GetCustomSpawnRegionChanceActiveScale()
                            : GameManager.m_ExperienceModeManager.GetSpawnRegionChanceActiveScale();
            bool active = Utils.RollChance(chanceActive);
            this.LogDebugInstanced($"Rolled {active} with a success chance of {chanceActive} (base: {mSpawnRegion.m_ChanceActive})", LogCategoryFlags.SpawnRegion);
            mSpawnRegion.gameObject.SetActive(active);  
            mModDataProxy.IsActive = active;
        }


        // Very heavy vanilla element, mostly just handling from the patch... not using it myself. bleh
        public void SetActive(bool active)
        {
            if (mSpawnRegion.m_AiSubTypeSpawned == AiSubType.Cougar)
            {
                this.LogDebugInstanced($"Cougar Override", LogCategoryFlags.SpawnRegion);
                return;
            }
            if (mSpawnRegion.m_WasForceDisabled && active)
            {
                this.LogDebugInstanced($"WasForceDisabled, cannot activate", LogCategoryFlags.SpawnRegion);
                return;
            }
            if (!active)
            {
                BaseAi spawn;
                for (int i = 0, iMax = ActiveSpawns.Count; i < iMax; i++)
                {
                    spawn = ActiveSpawns[i].BaseAi;
                    if (spawn == null)
                    {
                        this.LogWarningInstanced($"Null spawn", LogCategoryFlags.SpawnRegion);
                        continue;
                    }
                    if (!spawn.gameObject.activeSelf)
                    {
                        continue;
                    }
                    spawn.Despawn();
                }
            }
            mSpawnRegion.gameObject.SetActive(active);
        }

        #endregion


        #region Helpers

        public GameObject GetClosestActiveSpawn(Vector3 pos)
        {
            float closestDist = float.MaxValue;
            GameObject closestObj = null;
            for (int i = 0, iMax = ActiveSpawns.Count; i < iMax; i++)
            {
                if (ActiveSpawns[i].IsNullOrDestroyed())
                {
                    continue;
                }
                if (!ActiveSpawns[i].gameObject.activeSelf)
                {
                    continue;
                }
                float currentDist = SquaredDistance(pos, ActiveSpawns[i].transform.position);
                if (currentDist < closestDist)
                {
                    closestDist = currentDist;
                    closestObj = ActiveSpawns[i].gameObject;
                }
            }
            return closestObj;
        }
       

        public WanderRegion GetWanderRegion(Vector3 pos)
        {
            foreach (WanderRegion wanderRegion in mSpawnRegion.gameObject.GetComponentsInChildren<WanderRegion>())
            {
                if (wanderRegion.IsNullOrDestroyed())
                {
                    continue;
                }
                if (SquaredDistance(pos, wanderRegion.transform.position) < 0.001f)
                {
                    return wanderRegion;
                }
            }
            return null;
        }


        public Vector3[] GetWaypointCircuit()
        {
            if (mSpawnRegion.m_PathManagers == null)
            {
                this.LogErrorInstanced($"Null mSpawnRegion.m_PathManagers");
                return null;
            }
            if (mSpawnRegion.m_PathManagers.Length == 0)
            {
                this.LogErrorInstanced($"Empty mSpawnRegion.m_PathManagers");
                return null;
            }
            if (mSpawnRegion.m_CurrentWaypointPathIndex >= mSpawnRegion.m_PathManagers.Length)
            {
                this.LogErrorInstanced($"mSpawnRegion.m_CurrentWaypointIndex >= mSpawnRegion.m_PathManagers.Length");
                return null;
            }
            if (mSpawnRegion.m_PathManagers[mSpawnRegion.m_CurrentWaypointPathIndex].IsNullOrDestroyed())
            {
                this.LogErrorInstanced($"Path manager for current waypoint index is null");
                return null;
            }
            foreach (WanderRegion wanderRegion in mSpawnRegion.m_PathManagers[mSpawnRegion.m_CurrentWaypointPathIndex].gameObject.GetComponentsInChildren<WanderRegion>())
            {
                if (wanderRegion.IsNullOrDestroyed())
                {
                    this.LogErrorInstanced($"Null wander region found in pathmanager");
                    return null;
                }
            }
            return mSpawnRegion.m_PathManagers[mSpawnRegion.m_CurrentWaypointPathIndex].GetPathPoints();
        }


        private bool HasSameWildlifeMode(BaseAi baseAi, WildlifeMode wildlifeMode)
        {
            if (baseAi.IsNullOrDestroyed())
            {
                this.LogErrorInstanced($"Null baseAi");
                return false;
            }
            return baseAi.m_WildlifeMode == mSpawnRegion.m_WildlifeMode;
        }


        private bool TryGetSpawnPositionAndRotation(ref Vector3 spawnPos, ref Quaternion spawnRotation)
        {
            if (!mSpawnRegion.m_Den.IsNullOrDestroyed())
            {
                spawnPos = mSpawnRegion.m_Den.transform.position;
                spawnRotation = mSpawnRegion.m_Den.transform.rotation;
                this.LogTraceInstanced($"Found Den, returning spawn position", LogCategoryFlags.SpawnRegion);
                return true;
            }
            AreaMarkupManager areaMarkupManager = GameManager.m_AreaMarkupManager;
            if (areaMarkupManager.IsNullOrDestroyed())
            {
                this.LogErrorInstanced($"null AreaMarkupManager");
                return false;
            }
            AreaMarkup areaMarkup = areaMarkupManager.GetRandomSpawnAreaMarkupGivenSpawnRegion(mSpawnRegion);
            if (!areaMarkup.IsNullOrDestroyed())
            {
                this.LogTraceInstanced($"Found AreaMarkup", LogCategoryFlags.SpawnRegion);
                spawnPos = areaMarkup.transform.position;
                return true;
            }
            if (AiUtils.GetRandomPointOnNavmesh(out spawnPos,
                                                new Vector3(mSpawnRegion.m_Center.x,
                                                            mSpawnRegion.m_Center.y + mSpawnRegion.m_TopDownTerrainHeight,
                                                            mSpawnRegion.m_Center.z),
                                                0.0f,
                                                mSpawnRegion.m_Radius,
                                                AiUtils.GetNavmeshArea(mSpawnRegion.transform.position)))
            {
                this.LogTraceInstanced($"Found Random navmesh point", LogCategoryFlags.SpawnRegion);
                return true;
            }
            this.LogWarningInstanced($"Couldnt get a valid position and rotation", LogCategoryFlags.SpawnRegion);
            return false;
        }


        #endregion


        #region Respawning

        private float GetNumHoursBetweenRespawns()
        {
            float daysSurvived = GetCurrentTimelinePoint() / 24.0f;
            ExperienceMode currentExperienceMode = GameManager.m_ExperienceModeManager.GetCurrentExperienceMode();
            if (daysSurvived <= currentExperienceMode.m_RespawnHoursScaleDayStart)
            {
                return mSpawnRegion.m_NumHoursBetweenRespawns;
            }
            if (daysSurvived <= currentExperienceMode.m_RespawnHoursScaleDayFinal)
            {
                float normalizedBoundedDaysSurvivedMultiplier = Mathf.Clamp01(daysSurvived - currentExperienceMode.m_RespawnHoursScaleDayStart) / (currentExperienceMode.m_RespawnHoursScaleDayFinal - currentExperienceMode.m_RespawnHoursScaleDayStart);
                return mSpawnRegion.m_NumHoursBetweenRespawns * (currentExperienceMode.m_RespawnHoursScaleMax - 1.0f) * (normalizedBoundedDaysSurvivedMultiplier + 1.0f);
            }
            else
            {
                return currentExperienceMode.m_RespawnHoursScaleMax * mSpawnRegion.m_NumHoursBetweenRespawns;
            }
        }

        private void MaybeReducePendingRespawns()
        {
            if (!RespawnAllowed())
            {
                return;
            }
            mSpawnRegion.m_NumRespawnsPending--;
            mSpawnRegion.m_ElapasedHoursNextRespawnAllowed = GetCurrentTimelinePoint() + GetNumHoursBetweenRespawns();
            this.LogDebugInstanced($"Reducing pending respawns: {mSpawnRegion.m_NumHoursBetweenRespawns + 1} -> {mSpawnRegion.m_NumHoursBetweenRespawns}", LogCategoryFlags.SpawnRegion);
        }
        


        private bool RespawnAllowed()
        {
            if (mSpawnRegion.m_NumRespawnsPending < 1)
            {
                this.LogTraceInstanced($"No pending respawns, no respawn allowed", LogCategoryFlags.SpawnRegion | LogCategoryFlags.UpdateLoop);
                return false;
            }
            if (mSpawnRegion.m_ElapasedHoursNextRespawnAllowed >= GetCurrentTimelinePoint())
            {
                this.LogTraceInstanced($"Not yet time, no respawn allowed", LogCategoryFlags.SpawnRegion | LogCategoryFlags.UpdateLoop);
                return false;
            }
            this.LogTraceInstanced($"Respawn allowed", LogCategoryFlags.SpawnRegion);
            return true;
        }


        #endregion


        #region Trap

        private void MaybeReduceNumTrapped()
        {
            if (mSpawnRegion.m_HoursNextTrapReset > GetCurrentTimelinePoint())
            {
                this.LogTraceInstanced($"Not yet time", LogCategoryFlags.SpawnRegion | LogCategoryFlags.UpdateLoop);
                return;
            }
            if (mSpawnRegion.m_NumTrapped > 0)
            {
                mSpawnRegion.m_NumTrapped--;
                mSpawnRegion.m_HoursNextTrapReset += GetNumHoursBetweenRespawns();
                this.LogDebugInstanced($"Decrementing trapped count to {mSpawnRegion.m_NumTrapped} and updating hours next trap reset to {mSpawnRegion.m_HoursNextTrapReset}", LogCategoryFlags.SpawnRegion);
            }
            else
            {
                mSpawnRegion.m_HoursNextTrapReset = GetCurrentTimelinePoint() + GetNumHoursBetweenRespawns();
                this.LogDebugInstanced($"No trapped animals, resetting hours next trap reset to {mSpawnRegion.m_HoursNextTrapReset}", LogCategoryFlags.SpawnRegion);
            }
        }

        #endregion


        #region Aurora Management

        private void MaybeResumeAfterAurora()
        {
            if (mSpawnRegion.m_WasActiveBeforeAurora)
            {
                this.LogDebugInstanced($"Aurora enabled, activating previously active spawn region", LogCategoryFlags.SpawnRegion);
                mSpawnRegion.gameObject.SetActive(true);
            }
            mSpawnRegion.m_HasBeenDisabledByAurora = false;
            return;
        }


        private void MaybeSuspendForAurora()
        {
            if (mSpawnRegion.m_AiTypeSpawned == AiType.Predator)
            {
                return;
            }
            this.LogDebugInstanced($"Aurora enabled, suspending ambient spawn region", LogCategoryFlags.SpawnRegion);
            mSpawnRegion.m_WasActiveBeforeAurora = mSpawnRegion.gameObject.activeInHierarchy;
            mSpawnRegion.m_HasBeenDisabledByAurora = true;
            mSpawnRegion.gameObject.SetActive(false);
        }


        public void OnAuroraEnabled(bool enabled)
        {
            if (mSpawnRegion.m_WildlifeMode == (enabled ? WildlifeMode.Aurora : WildlifeMode.Normal))
            {
                // early-out for no change needed
                return;
            }
            if (enabled)
            {
                MaybeSuspendForAurora();
            }
            else
            {
                MaybeResumeAfterAurora();
            }
            RemoveActiveSpawns(GetCurrentActivePopulation(mSpawnRegion.m_WildlifeMode), mSpawnRegion.m_WildlifeMode, true);
            mSpawnRegion.m_WildlifeMode = enabled ? WildlifeMode.Aurora : WildlifeMode.Normal;
        }

        #endregion


        #region Spawning Validation

        protected virtual bool ValidSpawn(SpawnModDataProxy modDataProxy)
        {
            if (modDataProxy.ForceSpawn)
            {
                return true;
            }
            if (!PositionValidForSpawn(modDataProxy.CurrentPosition))
            {
                this.LogTraceInstanced($"invalid spawn location. Set force spawn to bypass this check!", LogCategoryFlags.SpawnRegion);
                return false;
            }
            if (GetCurrentActivePopulation(VanillaSpawnRegion.m_WildlifeMode) >= CalculateTargetPopulationInternal())
            {
                this.LogTraceInstanced($"Population maxed. Set force spawn to bypass this check!", LogCategoryFlags.SpawnRegion);
                return false;
            }
            PlayerManager playerManager = GameManager.m_PlayerManager;
            if (playerManager == null)
            {
                this.LogErrorInstanced($"null PlayerManager");
                return false;
            }
            playerManager.GetTeleportTransformAfterSceneLoad(out Vector3 position, out Quaternion rotation);
            float distanceToPlayer = Vector3.Distance(position, modDataProxy.CurrentPosition);
            Il2Cpp.SpawnRegionManager spawnRegionManager = GameManager.m_SpawnRegionManager;
            if (spawnRegionManager == null)
            {
                this.LogErrorInstanced($"null Il2Cpp.SpawnRegionManager");
                return false;
            }
            float minSpawnDist = spawnRegionManager.m_ClosestSpawnDistanceToPlayerAfterSceneTransition;
            ExperienceModeManager experienceModeManager = GameManager.m_ExperienceModeManager;
            if (experienceModeManager == null)
            {
                this.LogErrorInstanced($"null ExperienceModEmanager");
                return false;
            }
            ExperienceMode currentExperienceMode = experienceModeManager.GetCurrentExperienceMode();
            float closestSpawnDistanceAfterTransitionScale = 1.0f;
            if (currentExperienceMode != null)
            {
                closestSpawnDistanceAfterTransitionScale = currentExperienceMode.m_ClosestSpawnDistanceAfterTransitionScale;
            }
            if (distanceToPlayer < minSpawnDist * closestSpawnDistanceAfterTransitionScale)
            {
                this.LogTraceInstanced($"Player is too close, aborting. Set force spawn to bypass this check!", LogCategoryFlags.SpawnRegion);
                return false;
            }
            if (!AiUtils.IsNavmeshPosValid(modDataProxy.CurrentPosition, 0.5f, 1.0f))
            {
                this.LogTraceInstanced($"Invalid spawn position per AiUtils.IsNavmeshPosValid, aborting", LogCategoryFlags.SpawnRegion);
                return false;
            }
            if (mSpawnRegion.m_HasBeenDisabledByAurora)
            {
                this.LogTraceInstanced($"Disabled by aurora, aborting", LogCategoryFlags.SpawnRegion);
                return false;
            }
            return true;
        }


        private bool PositionValidForSpawn(Vector3 position)
        {
            if (mManager.PointInsideNoSpawnRegion(position))
            {
                this.LogTraceInstanced($"Encountered NoSpawn region", LogCategoryFlags.SpawnRegion);
                return false;
            }
            if (GameManager.m_Weather.IsIndoorEnvironment())
            {
                this.LogTraceInstanced($"Encountered indoor environment auto-accept positon... but why??", LogCategoryFlags.SpawnRegion);
                return true;
            }
            if (!mManager.PreSpawning && SpawnPositionOnScreenTooClose(position))
            {
                this.LogTraceInstanced($"NOT pre-spawning and spawn position on screen too close", LogCategoryFlags.SpawnRegion);
                return false;
            }
            if (!mManager.PreSpawning && SpawnPositionTooCloseToCamera(position))
            {
                this.LogTraceInstanced($"NOT pre-spawning and spawn position too close to camera", LogCategoryFlags.SpawnRegion);
                return false;
            }
            return true;
        }


        private bool SpawningSuppressedByExperienceMode()
        {
            if (mSpawnRegion.m_SpawnLevel == 0)
            {
                this.LogTraceInstanced($"Suppressed by zero sxpawn level", LogCategoryFlags.SpawnRegion | LogCategoryFlags.UpdateLoop);
                return true;
            }
            if (!ExperienceModeManager.s_CurrentGameMode.m_XPMode.m_NoPredatorsFirstDay)
            {
                this.LogTraceInstanced($"No predator grace period, not suppressed", LogCategoryFlags.SpawnRegion | LogCategoryFlags.UpdateLoop);
                return false;
            }
            if (mSpawnRegion.m_AiTypeSpawned != AiType.Predator)
            {
                this.LogTraceInstanced($"Non-predator spawn region, not suppressed", LogCategoryFlags.SpawnRegion | LogCategoryFlags.UpdateLoop);
                return false;
            }
            if (mSpawnRegion.m_ForcePredatorOverride)
            {
                this.LogTraceInstanced($"Forced predator override, not suppressed", LogCategoryFlags.SpawnRegion | LogCategoryFlags.UpdateLoop);
                return false;
            }
            if (GetCurrentTimelinePoint() >= Il2Cpp.SpawnRegionManager.m_NoPredatorSpawningInVoyageurHours)
            {
                this.LogTraceInstanced($"Past grace period, not suppressed", LogCategoryFlags.SpawnRegion | LogCategoryFlags.UpdateLoop);
                return false;
            }
            this.LogTraceInstanced($"Predator spawn region suppressed during predator grace period", LogCategoryFlags.SpawnRegion | LogCategoryFlags.UpdateLoop);
            return true;
        }


        private bool SpawnPositionOnScreenTooClose(Vector3 spawnPos)
        {
            PlayerManager playerManager = GameManager.m_PlayerManager;
            if (playerManager.m_Ghost)
            {
                this.LogTraceInstanced($"Ghost, not too close", LogCategoryFlags.SpawnRegion | LogCategoryFlags.UpdateLoop);
                return false;
            }
            if (Utils.PositionIsOnscreen(spawnPos)
                && Utils.DistanceToMainCamera(spawnPos) < GameManager.m_SpawnRegionManager.m_AllowSpawnOnscreenDistance
                && !mSpawnRegion.m_OverrideDistanceToCamera)
            {
                this.LogTraceInstanced($"Position is on screen and dist to main camera within allowSpawnOnScreenDistance and spawnRegion.m_OverrideDistanceToCamera is false, too close for spawn", LogCategoryFlags.SpawnRegion | LogCategoryFlags.UpdateLoop);
                return true;
            }
            if (Utils.PositionIsInLOSOfPlayer(spawnPos) && !mSpawnRegion.m_OverrideCameraLineOfSight)
            {
                this.LogTraceInstanced($"Position in LOS of player and no camera LOS override, too close to spawn", LogCategoryFlags.SpawnRegion | LogCategoryFlags.UpdateLoop);
                return true;
            }
            this.LogTraceInstanced($"ScreenPos not too close to spawn!", LogCategoryFlags.SpawnRegion | LogCategoryFlags.UpdateLoop);
            return false;
        }


        private bool SpawnPositionTooCloseToCamera(Vector3 spawnPos)
        {
            PlayerManager playerManager = GameManager.m_PlayerManager;
            if (playerManager.m_Ghost)
            {
                this.LogTraceInstanced($"Ghost, not too close", LogCategoryFlags.SpawnRegion | LogCategoryFlags.UpdateLoop);
                return false;
            }

            float closestDistToPlayer = GameManager.m_SpawnRegionManager.m_ClosestSpawnDistanceToPlayer;
            if (GameManager.m_Weather.IsIndoorEnvironment())
            {
                closestDistToPlayer *= 0.5f;
            }
            if (Vector3.Distance(GameManager.m_vpFPSCamera.transform.position, spawnPos) <= closestDistToPlayer)
            {
                this.LogTraceInstanced($"Too close to spawn", LogCategoryFlags.SpawnRegion | LogCategoryFlags.UpdateLoop);
                return true;
            }
            this.LogTraceInstanced($"NOT Too close to spawn", LogCategoryFlags.SpawnRegion);
            return false;
        }


        private bool SpawnRegionCloseEnoughForSpawning()
        {
            float dist = Utils.DistanceToMainCamera(mSpawnRegion.m_Center);
            bool closeEnough = mSpawnRegion.m_Radius + GameManager.m_SpawnRegionManager.m_SpawnRegionDisableDistance >= dist;
            this.LogTraceInstanced($"Checking if spawn region at {mSpawnRegion.m_Center} with radius {mSpawnRegion.m_Radius} plus disable dist {GameManager.m_SpawnRegionManager.m_SpawnRegionDisableDistance} and distance to camera {dist} is close enough for spawning: {closeEnough}", LogCategoryFlags.SpawnRegion | LogCategoryFlags.UpdateLoop);
            return closeEnough;
        }


        #endregion


        #region Spawnable Prefab Management


        private string GetSpawnablePrefabName()
        {
            if (string.IsNullOrEmpty(mSpawnRegion.m_SpawnablePrefabName))
            {
                if (mSpawnRegion.m_SpawnablePrefab.IsNullOrDestroyed())
                {
                    this.LogTraceInstanced($"null spawnable prefab on spawn region, fetching...", LogCategoryFlags.SpawnRegion);
                    AssetReferenceAnimalPrefab animalReferencePrefab = mSpawnRegion.m_SpawnRegionAnimalTableSO.PickSpawnAnimal(WildlifeMode.Normal);
                    mSpawnRegion.m_SpawnablePrefab = animalReferencePrefab.GetOrLoadAsset();
                    animalReferencePrefab.ReleaseAsset();
                }
                if (mSpawnRegion.m_SpawnablePrefab.IsNullOrDestroyed())
                {
                    this.LogErrorInstanced($"Could not fetch spawnable prefab");
                    return string.Empty;
                }
            }
            mSpawnRegion.m_SpawnablePrefabName = mSpawnRegion.m_SpawnablePrefab.name;
            return mSpawnRegion.m_SpawnablePrefabName;
        }


        public bool TryGetSpawnableBaseAi(out BaseAi baseAi)
        {
            baseAi = null;
            if (mSpawnRegion.IsNullOrDestroyed() || mSpawnRegion.m_SpawnablePrefab == null || !mSpawnRegion.m_SpawnablePrefab.TryGetComponent<BaseAi>(out baseAi) || baseAi.IsNullOrDestroyed())
            {
                this.LogTraceInstanced($"Can't get spawnable ai script from spawn region, no pre-queueing spawns. Next...", LogCategoryFlags.SpawnRegion);
                return false;
            }
            return true;
        }

        #endregion
    }
}