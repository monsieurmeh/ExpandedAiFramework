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
        public virtual string InstanceInfo { get { return !VanillaSpawnRegion.IsNullOrDestroyed() ? VanillaSpawnRegion.GetHashCode().ToString() : "NULL"; } }
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
            mSpawnRegion.m_AiTypeSpawned = baseAi.m_AiType;
            mSpawnRegion.m_AiSubTypeSpawned = baseAi.m_AiSubType;
            if (mSpawnRegion.m_AiSubTypeSpawned == AiSubType.Wolf)
            {
                mSpawnRegion.m_WolfTypeSpawned = baseAi.NormalWolf.IsNullOrDestroyed() ? WolfType.Timberwolf : WolfType.Normal;
            }
            mSpawnRegion.m_ElapasedHoursNextRespawnAllowed = 0.0f;
            mSpawnRegion.m_ElapsedHoursAtLastActiveReRoll = 0.0f;
            mSpawnRegion.m_HoursNextTrapReset = 0.0f;
            mSpawnRegion.m_NumRespawnsPending = 0;
            mSpawnRegion.m_CooldownTimerHours = 0.0f;
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
            mSpawnRegion.m_Center = mSpawnRegion.transform.position;
            if (mSpawnRegion.m_PathManagers == null)
            {
                this.LogErrorInstanced($"Could not construct mSpawnRegion.m_PathManagers");
                return;
            }

            if (mSpawnRegion.m_PathManagers.Length != 0)
            {
                mSpawnRegion.SetRandomWaypointCircuit();
            }
            ActiveSpawns.Clear();


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
                this.LogVerboseInstanced($"Invalid waypoint index !_{waypointIndex}_! (waypoints available: !_{mSpawnRegion?.m_PathManagers?.Length ?? 0}_!)");
                return;
            }
            Vector3[] pathPoints = mSpawnRegion.m_PathManagers[mSpawnRegion.m_CurrentWaypointPathIndex].GetPathPoints();
            BoundingSphereFromPoints.Calculate(pathPoints, pathPoints.Length, 0, 0);
            mSpawnRegion.m_Radius = BoundingSphereFromPoints.m_Radius;
            mSpawnRegion.m_Center = BoundingSphereFromPoints.m_Center;
            this.LogVerboseInstanced($"Set bounding sphere to center !_{mSpawnRegion.m_Center}_! and radius !_{mSpawnRegion.m_Radius}_!");
        }


        public void SetRandomWaypointCircuit()
        {
            if (mSpawnRegion.m_PathManagers.Count <= 0)
            {
                this.LogVerboseInstanced($"No pathmanagers, cannot set waypoint circuit");
                return;
            }
            mSpawnRegion.m_CurrentWaypointPathIndex = UnityEngine.Random.Range(0, mSpawnRegion.m_PathManagers.Count);
            this.LogVerboseInstanced($"mSpawnRegion.m_CurrentWaypointIndex set to !_{mSpawnRegion.m_CurrentWaypointPathIndex}_!");
        }


        public void Start()
        {
            if (mSpawnRegion.m_StartHasBeenCalled)
            {
                return;
            }
            mSpawnRegion.m_StartHasBeenCalled = true;
            ExperienceModeManager experienceModeManager = GameManager.m_ExperienceModeManager;
            if (experienceModeManager.IsNullOrDestroyed())
            {
                this.LogErrorInstanced($"null ExperienceModeManager");
                return;
            }
            float chanceActive = mSpawnRegion.m_ChanceActive;
            chanceActive *= GameManager.InCustomMode()
                            ? GetCustomSpawnRegionChanceActiveScale()
                            : experienceModeManager.GetSpawnRegionChanceActiveScale();
            if (!Utils.RollChance(chanceActive))
            {
                mSpawnRegion.gameObject.SetActive(false);
                return;
            }
            if (Il2Cpp.SpawnRegion.m_SpawnDataProxyPool != null
                && Il2Cpp.SpawnRegion.m_SpawnDataProxyPool.Length != 0
                && Il2Cpp.SpawnRegion.m_SpawnDataProxyPool[0].IsNullOrDestroyed())
            {
                for (int i = 0, iMax = Il2Cpp.SpawnRegion.m_SpawnDataProxyPool.Length; i < iMax; i++)
                {
                    Il2Cpp.SpawnRegion.m_SpawnDataProxyPool[i] = new SpawnDataProxy();
                }
            }
        }


        public void UpdateFromManager()
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
                this.LogVerboseInstanced($"No den, no sleep duration");
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
                this.LogVerboseInstanced($"Null den, no sleep");
                return false;
            }
            float sleepChance = GameManager.m_TimeOfDay.IsDay()
                ? mSpawnRegion.m_Den.m_ChanceSleepAfterWaypointsLoopDay
                : mSpawnRegion.m_Den.m_ChanceSleepAfterWaypointsLoopNight;
            bool shouldSleep = Utils.RollChance(sleepChance);
            this.LogVerboseInstanced($"Rolling against chance to sleep of !_{sleepChance}_!: !_{shouldSleep}_!");
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
                        this.LogTraceInstanced($"No queued spawns for spawn region with guid {mModDataProxy.Guid} for mode {mode}. Wait for pending proxies under construction.");
                        return;
                    }
                    this.LogTraceInstanced($"No queued spawns for spawn region with guid {mModDataProxy.Guid} for mode {mode}. Queueing request for new proxy and spawn...");
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
                    this.LogWarningInstanced($"Proxy {proxy.Guid} unavailable");
                    return;
                }
                if (mPendingSpawns.Contains(proxy))
                {
                    this.LogWarningInstanced($"Attempting to double-spawn proxy, aborting!");
                    return;
                }
                for (int i = 0, iMax = mActiveSpawns.Count; i < iMax; i++)
                {
                    if (mActiveSpawns[i].ModDataProxy == proxy)
                    {
                        this.LogWarningInstanced($"Attempting to double-spawn proxy, aborting!");
                        return;
                    }
                }
                proxy.Available = false;
                mPendingSpawns.Enqueue(proxy);
            }
        }


        private void ProcessPendingSpawnQueue()
        {
            lock (mPendingSpawns)
            {
                while (mPendingSpawns.Count > 0)
                {
                    SpawnInternal(mPendingSpawns.Dequeue());
                }
            }
        }


        private void SpawnInternal(SpawnModDataProxy queuedProxy)
        {
            if (InstantiateSpawn(queuedProxy) != null)
            {
                mDataManager.ScheduleSpawnModDataProxyRequest(new ClaimAvailableSpawnRequest(queuedProxy.Guid, queuedProxy.Scene, null, false), queuedProxy.WildlifeMode);
            }
            else
            {
                this.LogTraceInstanced($"Proxy with guid {queuedProxy.Guid} set to AVAILABLE.");
                queuedProxy.Available = true;
            }
        }


        private CustomBaseAi InstantiateSpawn(SpawnModDataProxy modDataProxy)
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
            return customBaseAi;
        }


        private CustomBaseAi InstantiateSpawnInternal(SpawnModDataProxy modDataProxy)
        {
            GameObject spawnablePrefab = mSpawnRegion.m_SpawnablePrefab;
            if (mSpawnRegion.m_AuroraSpawnablePrefab != null && modDataProxy.WildlifeMode == WildlifeMode.Aurora)
            {
                this.LogVerboseInstanced($"Wildlife mode is aurora and aurora spawnable prefab available, overriding param prefab");
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
                newPackAnimal.gameObject.tag = mSpawnRegion.m_PackGroupId;
            }
            if (!mManager.TryWrapNewSpawn(newBaseAi, mSpawnRegion, out CustomBaseAi newCustomBaseAi, modDataProxy))
            {
                this.LogErrorInstanced($"Error wrapping new spawn!");
                return null;
            }
            return newCustomBaseAi;
        }


        protected bool PostProcessInstantiatedSpawn(CustomBaseAi customBaseAi)
        {
            BaseAi baseAi = customBaseAi.BaseAi;
            if (customBaseAi.IsNullOrDestroyed())
            {
                this.LogWarningInstanced($"InstantiateSpawnInternal returned null BaseAi, aborting");
                return false;
            }
            Transform transform = mSpawnRegion.transform;
            if (mSpawnRegion.m_WanderRegion != null)
            {
                this.LogVerboseInstanced($"Wander region found, setting move agent transform to wander region?");
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
                this.LogWarningInstanced($"Potential error: Could not get spawn position and rotation. Aborting");
                return;
            }

            // Need to delegate this upwards
            Type spawnType = typeof(void);
            ISubManager[] subManagers = mManager.Manager.SubManagerArray;
            for (int i = 0, iMax = subManagers.Length; i < iMax; i++)
            {
                this.LogVerboseInstanced($"Allowing submanager {subManagers[i]} to intercept spawn...");
                if (subManagers[i].ShouldInterceptSpawn(this))
                {
                    LogTrace($"Spawn intercept from submanager {subManagers[i]}! new type: {subManagers[i].SpawnType}");
                    spawnType = subManagers[i].SpawnType;
                    break;
                }
            }
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
                this.LogVerboseInstanced($"No submanager interceptions, attempting to randomly pick a valid spawn type...");
                if (async)
                {
                    mProxiesUnderConstruction++;
                    mManager.Manager.TypePicker.PickTypeAsync(spawnableAi, (type) =>
                    {
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


        private SpawnModDataProxy GenerateNewSpawnModDataProxy(Type variantSpawnType, WildlifeMode wildlifeMode, Vector3 position, Quaternion rotation)
        {
            if (variantSpawnType == null)
            {
                this.LogTraceInstanced($"Can't generate new spawn mod data proxy with null variant spawn type!");
                return null;
            }
            SpawnModDataProxy newProxy = new SpawnModDataProxy(Guid.NewGuid(), mManager.Manager.CurrentScene, position, rotation, mSpawnRegion.m_AiSubTypeSpawned, wildlifeMode, variantSpawnType);
            newProxy.ParentGuid = mModDataProxy.Guid;
            mDataManager.ScheduleRegisterSpawnModDataProxyRequest(newProxy, (proxy, result) =>
            {
                if (mManager.Manager.SubManagers.TryGetValue(variantSpawnType, out ISubManager subManager))
                {
                    subManager.PostProcessNewSpawnModDataProxy(newProxy);
                }
                if (newProxy.ForceSpawn)
                {
                    this.LogTraceInstanced($"FORCE spawning on creation!");
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
                this.LogVerboseInstanced($"{otherModeActivePopulation} active wildlife of opposite type, removing");
                RemoveActiveSpawns(otherModeActivePopulation, currentMode, true);
            }
            int targetDelta = targetPop - currentActivePopulation;
            if (targetDelta > 0)
            {
                if (mSpawnRegion.m_HasBeenDisabledByAurora)
                {
                    this.LogVerboseInstanced($"Disabled by aurora, aborting");
                    return;
                }
                if (!SpawnRegionCloseEnoughForSpawning())
                {
                    this.LogVerboseInstanced($"Not close enough for spawning, aborting");
                    return;
                }
                if (targetDelta <= 0)
                {
                    this.LogVerboseInstanced($"numToActivate (!_{targetDelta}_!) invalid, aborting");
                    return;
                }
                this.LogTraceInstanced($"{targetDelta} ({currentActivePopulation} vs {targetPop}) missing active wildlife of current type, adding");
                Spawn(currentMode);
            }
            else if (targetDelta < 0)
            {
                this.LogVerboseInstanced($"{-targetDelta} ({currentActivePopulation} vs {targetPop}) excess active wildlife of current type, removing");
                RemoveActiveSpawns(-targetDelta, currentMode, false);
            }
        }


        public  int CalculateTargetPopulation()
        {
            if (SpawningSuppressedByExperienceMode())
            {
                this.LogTraceInstanced($"Spawning suppressed by experience mode");
                return 0;
            }
            if (!mSpawnRegion.m_CanSpawnInBlizzard && GameManager.m_Weather.IsBlizzard())
            {
                this.LogTraceInstanced($"Cannot spawn in blizzard");
                return 0;
            }
            return CalculateTargetPopulationInternal();
        }


        private int CalculateTargetPopulationInternal()
        {
            int maxSimultaneousSpawns = GameManager.m_TimeOfDay.IsDay()
                ? GetMaxSimultaneousSpawnsDay()
                : GetMaxSimultaneousSpawnsNight();
            int adjustedMaxSimultaneousSpawns = maxSimultaneousSpawns - mSpawnRegion.m_NumTrapped - mSpawnRegion.m_NumRespawnsPending;
            if (adjustedMaxSimultaneousSpawns < 0)
            {
                return 0;
            }
            return Math.Min(adjustedMaxSimultaneousSpawns, mSpawnRegion.m_DifficultySettings[(int)mSpawnRegion.m_SpawnLevel].m_MaxSimultaneousSpawnsDay + AdditionalSimultaneousSpawnAllowance());
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
            return mSpawnRegion.m_DifficultySettings[(int)mSpawnRegion.m_SpawnLevel].m_MaxSimultaneousSpawnsDay;
        }


        public virtual int GetMaxSimultaneousSpawnsNight()
        {
            if (mSpawnRegion.m_DifficultySettings == null)
            {
                this.LogErrorInstanced($"Null mSpawnRegion.m_DifficultySettings");
                return 0;
            }
            return mSpawnRegion.m_DifficultySettings[(int)mSpawnRegion.m_SpawnLevel].m_MaxSimultaneousSpawnsNight;
        }


        public int GetNumActiveSpawns()
        {
            if (mSpawnRegion.gameObject.IsNullOrDestroyed())
            {
                this.LogTraceInstanced($"Null mSpawnRegion.gameObject; this is an ACTUAL error!");
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
                        this.LogVerboseInstanced($"Adjusting other wildlife mode and spawn mode matches called mode, wildlifeMode matched for despawn");
                        canDespawn = true;
                    }
                    if (!canDespawn && !HasSameWildlifeMode(spawn, wildlifeMode))
                    {
                        this.LogVerboseInstanced($"NOT adjusting other wildlife mode and spawn mode does NOT match called mode, wildlifeMode matched for despawn");
                        canDespawn = true;
                    }
                    if (canDespawn
                        && spawn.GetAiMode() != AiMode.Flee
                        && spawn.GetAiMode() != AiMode.Dead)
                    {
                        this.LogVerboseInstanced($"Can despawn && and spawn is not fleeing or dead, setting flee");
                        spawn.SetAiMode(AiMode.Flee);
                    }
                    Vector3 spawnPos = spawn.m_CachedTransform.position;
                    bool canDespawnDueToProximity = playerGhost;
                    if (canDespawnDueToProximity)
                    {
                        this.LogVerboseInstanced($"Ghost, proximity check passed");
                    }
                    if (!canDespawnDueToProximity
                        && Utils.DistanceToMainCamera(spawnPos) >= GameManager.GetSpawnRegionManager().m_DisallowDespawnBelowDistance
                        && (!Utils.PositionIsOnscreen(spawnPos) || Utils.DistanceToMainCamera(spawnPos) >= GameManager.GetSpawnRegionManager().m_AllowDespawnOnscreenDistance)
                        && !Utils.PositionIsInLOSOfPlayer(spawnPos)) //Why is this last one needed...?
                    {
                        this.LogVerboseInstanced($"Ai is not visible, proximity check passed");
                        canDespawnDueToProximity = true;
                    }
                    if (!canDespawnDueToProximity)
                    {
                        this.LogVerboseInstanced($"Proximity check failed, cannot despawn");
                        continue;
                    }
                    if (!canDespawn)
                    {
                        if (!spawn.m_CurrentTarget.IsNullOrDestroyed() && spawn.m_CurrentTarget.IsPlayer())
                        {
                            this.LogVerboseInstanced($"Failed to match wildlifeMode for forced removal and spawn is targetting player, cannot despawn");
                            continue;
                        }
                        if (spawn.m_CurrentMode == AiMode.Feeding)
                        {
                            this.LogVerboseInstanced($"Failed to match wildlifeMode for forced removal and spawn is eating, cannot despawn");
                            continue;
                        }
                        if (spawn.m_CurrentMode == AiMode.Sleep)
                        {
                            this.LogVerboseInstanced($"Failed to match wildlifeMode for forced removal and spawn is sleeping, cannot despawn");
                            continue;
                        }
                        if (spawn.IsBleedingOut())
                        {
                            this.LogVerboseInstanced($"Failed to match wildlifeMode for forced removal and spawn is bleeding, cannot despawn");
                            continue;
                        }
                        if (!spawn.NormalWolf.IsNullOrDestroyed() && spawn.m_CurrentMode == AiMode.WanderPaused)
                        {
                            this.LogVerboseInstanced($"Failed to match wildlifeMode for forced removal and spawn is normal wolf in AiMode.WanderPaused, cannot despawn");
                            continue;
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
                    break;
                case AiSubType.Bear:
                    spawnChance = customMode.m_BearSpawnChance;
                    break;
                case AiSubType.Stag:
                    spawnChance = customMode.m_DeerSpawnChance;
                    break;
                case AiSubType.Rabbit:
                    spawnChance = customMode.m_RabbitSpawnChance;
                    break;
                case AiSubType.Moose:
                    spawnChance = customMode.m_MooseSpawnChance;
                    break;
                case AiSubType.Cougar:
                    return 1.0f;
            }
            if (spawnChance == CustomTunableNLMHV.None)
            {
                return 0.0f;
            }
            if (mSpawnRegion.m_AiTypeSpawned == AiType.Ambient)
            {
                switch (spawnChance)
                {
                    case CustomTunableNLMHV.Low: return interloperExperienceMode.m_SpawnRegionChanceActiveScale;
                    case CustomTunableNLMHV.Medium: return stalkerExperienceMode.m_SpawnRegionChanceActiveScale;
                    case CustomTunableNLMHV.High: return voyagerExperienceMode.m_SpawnRegionChanceActiveScale;
                    case CustomTunableNLMHV.VeryHigh: return pilgrimExperienceMode.m_SpawnRegionChanceActiveScale;
                    default: return 1.0f;
                }
            }
            else
            {
                switch (spawnChance)
                {
                    case CustomTunableNLMHV.Low: return pilgrimExperienceMode.m_SpawnRegionChanceActiveScale;
                    case CustomTunableNLMHV.Medium: return voyagerExperienceMode.m_SpawnRegionChanceActiveScale;
                    case CustomTunableNLMHV.High: return interloperExperienceMode.m_SpawnRegionChanceActiveScale;
                    case CustomTunableNLMHV.VeryHigh: return stalkerExperienceMode.m_SpawnRegionChanceActiveScale;
                    default: return 1.0f;
                }
            }
        }


        public void MaybeReRollActive()
        {
            if (mSpawnRegion.m_HasBeenDisabledByAurora)
            {
                this.LogVerboseInstanced($"Disabled by aurora, aborting");
                return;
            }
            if (mSpawnRegion.m_AiSubTypeSpawned == AiSubType.Cougar)
            {
                this.LogVerboseInstanced($"Cougar override");
                return;
            }
            if (mSpawnRegion.m_WasForceDisabled)
            {
                this.LogVerboseInstanced($"Force disabled");
                return;
            }
            if (mSpawnRegion.m_HoursReRollActive <= 0.0001f)
            {
                this.LogVerboseInstanced($"Effectively zero mSpawnRegion.m_HoursReRollActive, aborting to prevent div by near zero");
                return;
            }
            if (GetCurrentTimelinePoint() - mSpawnRegion.m_ElapsedHoursAtLastActiveReRoll < mSpawnRegion.m_HoursReRollActive)
            {
                this.LogVerboseInstanced($"Not yet time");
                return;
            }
            if (!CanDoReRoll())
            {
                this.LogVerboseInstanced($"Ineligible for ReRoll");
                return;
            }
            mSpawnRegion.gameObject.SetActive(Utils.RollChance(GameManager.m_ExperienceModeManager.GetSpawnRegionChanceActiveScale()));
            mSpawnRegion.m_ElapsedHoursAtLastActiveReRoll = GetCurrentTimelinePoint();
        }


        public void SetActive(bool active)
        {
            if (mSpawnRegion.m_AiSubTypeSpawned == AiSubType.Cougar)
            {
                this.LogVerboseInstanced($"Cougar Override");
                return;
            }
            if (mSpawnRegion.m_WasForceDisabled && active)
            {
                this.LogVerboseInstanced($"WasForceDisabled, cannot activate");
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
                        this.LogWarningInstanced($"Null spawn");
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
                this.LogVerboseInstanced($"Found AreaMarkup");
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
                this.LogVerboseInstanced($"Found Random navmesh point");
                return true;
            }
            this.LogVerboseInstanced($"Couldnt get a valid position and rotation");
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
            this.LogTraceInstanced($"Reducing pending respawns: {mSpawnRegion.m_NumHoursBetweenRespawns + 1} -> {mSpawnRegion.m_NumHoursBetweenRespawns}");
        }
        


        private bool RespawnAllowed()
        {
            if (mSpawnRegion.m_NumRespawnsPending < 1)
            {
                this.LogVerboseInstanced($"No pending respawns, no respawn allowed");
                return false;
            }
            if (mSpawnRegion.m_ElapasedHoursNextRespawnAllowed >= GetCurrentTimelinePoint())
            {
                this.LogVerboseInstanced($"Not yet time, no respawn allowed");
                return false;
            }
            this.LogVerboseInstanced($"Respawn allowed");
            return true;
        }


        #endregion


        #region Trap

        private void MaybeReduceNumTrapped()
        {
            if (mSpawnRegion.m_HoursNextTrapReset > GetCurrentTimelinePoint())
            {
                return;
            }
            if (mSpawnRegion.m_NumTrapped > 0)
            {
                mSpawnRegion.m_NumTrapped--;
            }
            mSpawnRegion.m_HoursNextTrapReset += GetNumHoursBetweenRespawns();
        }

        #endregion


        #region Aurora Management

        private void MaybeResumeAfterAurora()
        {
            if (mSpawnRegion.m_WasActiveBeforeAurora)
            {
                mSpawnRegion.gameObject.SetActive(true);
            }
            mSpawnRegion.m_HasBeenDisabledByAurora = false;
            return;
        }


        private void MaybeSuspendForAurora()
        {
            if (mSpawnRegion.m_AiSubTypeSpawned == AiSubType.Cougar)
            {
                this.LogVerboseInstanced($"Cougar override");
                return;
            }
            mSpawnRegion.m_WasActiveBeforeAurora = mSpawnRegion.gameObject.activeInHierarchy;
            mSpawnRegion.m_HasBeenDisabledByAurora = true;
            mSpawnRegion.gameObject.SetActive(false);
        }


        public void OnAuroraEnabled(bool enabled)
        {
            if (mSpawnRegion.m_WildlifeMode == (enabled ? WildlifeMode.Aurora : WildlifeMode.Normal))
            {
                this.LogVerboseInstanced($"WildlifeMode match, ignoring");
                return;
            }

            if (enabled)
            {
                if (mSpawnRegion.m_AiTypeSpawned != AiType.Predator)
                {
                    this.LogVerboseInstanced($"Aurora enabled, maybe suspending ambient spawn region");
                    MaybeSuspendForAurora();
                }
            }
            else
            {
                if (mSpawnRegion.m_WasActiveBeforeAurora)
                {
                    this.LogVerboseInstanced($"Aurora enabled, activating previously active spawn region");
                    mSpawnRegion.gameObject.SetActive(true);
                }
                mSpawnRegion.m_HasBeenDisabledByAurora = false;
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
                LogError($"invalid spawn location. Set force spawn to bypass this check!");
                return false;
            }
            if (GetCurrentActivePopulation(VanillaSpawnRegion.m_WildlifeMode) >= CalculateTargetPopulationInternal())
            {
                LogError($"Population maxed. Set force spawn to bypass this check!");
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
                this.LogVerboseInstanced($"Player is too close, aborting. Set force spawn to bypass this check!");
                return false;
            }
            if (!AiUtils.IsNavmeshPosValid(modDataProxy.CurrentPosition, 0.5f, 1.0f))
            {
                this.LogWarningInstanced($"Invalid spawn position per AiUtils.IsNavmeshPosValid, aborting");
                return false;
            }
            if (mSpawnRegion.m_HasBeenDisabledByAurora)
            {
                this.LogVerboseInstanced($"Disabled by aurora, aborting");
                return false;
            }
            return true;
        }


        private bool PositionValidForSpawn(Vector3 position)
        {
            if (mManager.PointInsideNoSpawnRegion(position))
            {
                this.LogTraceInstanced($"Encountered NoSpawn region");
                return false;
            }
            if (GameManager.m_Weather.IsIndoorEnvironment())
            {
                this.LogVerboseInstanced($"Encountered indoor environment... why does this automatically return true???");
                return true;
            }
            if (!mManager.PreSpawning && SpawnPositionOnScreenTooClose(position))
            {
                this.LogTraceInstanced($"Spawn position on screen too close");
                return false;
            }
            if (!mManager.PreSpawning && SpawnPositionTooCloseToCamera(position))
            {
                this.LogTraceInstanced($"Spawn position too close to camera");
                return false;
            }
            return true;
        }


        private bool SpawningSuppressedByExperienceMode()
        {
            if (mSpawnRegion.m_SpawnLevel == 0)
            {
                this.LogVerboseInstanced($"Suppressed by zero spawn level");
                return true;
            }
            if (!ExperienceModeManager.s_CurrentGameMode.m_XPMode.m_NoPredatorsFirstDay)
            {
                this.LogVerboseInstanced($"No predator grace period, not suppressed");
                return false;
            }
            if (mSpawnRegion.m_AiTypeSpawned != AiType.Predator)
            {
                this.LogVerboseInstanced($"Non-predator spawn region, not suppressed");
                return false;
            }
            if (mSpawnRegion.m_ForcePredatorOverride)
            {
                this.LogVerboseInstanced($"Forced predator override, not suppressed");
                return false;
            }
            if (GetCurrentTimelinePoint() >= Il2Cpp.SpawnRegionManager.m_NoPredatorSpawningInVoyageurHours)
            {
                this.LogVerboseInstanced($"Past grace period, not suppressed");
                return false;
            }
            this.LogVerboseInstanced($"Predator spawn region suppressed during predator grace period");
            return true;
        }


        private bool SpawnPositionOnScreenTooClose(Vector3 spawnPos)
        {
            PlayerManager playerManager = GameManager.m_PlayerManager;
            if (playerManager.m_Ghost)
            {
                this.LogVerboseInstanced($"Ghost, not too close");
                return false;
            }
            if (Utils.PositionIsOnscreen(spawnPos)
                && Utils.DistanceToMainCamera(spawnPos) < GameManager.m_SpawnRegionManager.m_AllowSpawnOnscreenDistance
                && !mSpawnRegion.m_OverrideDistanceToCamera)
            {
                this.LogVerboseInstanced($"Position is on screen and dist to main camera within allowSpawnOnScreenDistance and spawnRegion.m_OverrideDistanceToCamera is false, too close for spawn");
                return true;
            }
            if (Utils.PositionIsInLOSOfPlayer(spawnPos) && !mSpawnRegion.m_OverrideCameraLineOfSight)
            {
                this.LogVerboseInstanced($"Position in LOS of player and no camera LOS override, too close to spawn");
                return true;
            }
            this.LogVerboseInstanced($"ScreenPos not too close to spawn!");
            return false;
        }


        private bool SpawnPositionTooCloseToCamera(Vector3 spawnPos)
        {
            PlayerManager playerManager = GameManager.m_PlayerManager;
            if (playerManager.m_Ghost)
            {
                this.LogVerboseInstanced($"Ghost, not too close");
                return false;
            }

            float closestDistToPlayer = GameManager.m_SpawnRegionManager.m_ClosestSpawnDistanceToPlayer;
            if (GameManager.m_Weather.IsIndoorEnvironment())
            {
                closestDistToPlayer *= 0.5f;
            }
            if (Vector3.Distance(GameManager.m_vpFPSCamera.transform.position, spawnPos) <= closestDistToPlayer)
            {
                this.LogVerboseInstanced($"Too close to spawn");
                return true;
            }
            this.LogVerboseInstanced($"NOT Too close to spawn");
            return false;
        }


        private bool SpawnRegionCloseEnoughForSpawning()
        {
            float dist = Utils.DistanceToMainCamera(mSpawnRegion.m_Center);
            bool closeEnough = mSpawnRegion.m_Radius + GameManager.m_SpawnRegionManager.m_SpawnRegionDisableDistance >= dist;
            this.LogVerboseInstanced($"Checking if spawn region at {mSpawnRegion.m_Center} with radius {mSpawnRegion.m_Radius} plus disable dist {GameManager.m_SpawnRegionManager.m_SpawnRegionDisableDistance} and distance to camera {dist} is close enough for spawning: {closeEnough}");
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
                    this.LogVerboseInstanced($"null spawnable prefab on spawn region, fetching...");
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
                this.LogVerboseInstanced($"Can't get spawnable ai script from spawn region, no pre-queueing spawns. Next...");
                return false;
            }
            return true;
        }

        #endregion
    }
}