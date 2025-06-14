using Harmony;
using HarmonyLib;
using Il2Cpp;
using Il2CppRewired.Utils;
using Il2CppSWS;
using Il2CppTLD.AI;
using Il2CppTLD.Gameplay;
using Il2CppTLD.Gameplay.Tunable;
using Il2CppTLD.GameplayTags;
using Il2CppTLD.PDID;
using Il2CppTLD.Serialization;
using System;
using UnityEngine;
using UnityEngine.AI;
using static Il2Cpp.UITweener;



namespace ExpandedAiFramework
{
    public class CustomBaseSpawnRegion : ILogInfoProvider
    {
        protected SpawnRegion mSpawnRegion;
        protected TimeOfDay mTimeOfDay;
        protected SpawnRegionManager mManager;
        protected SpawnRegionModDataProxy mModDataProxy;

        public SpawnRegion VanillaSpawnRegion { get { return mSpawnRegion; } }
        public SpawnRegionModDataProxy ModDataProxy { get { return mModDataProxy; } }
        public virtual string InstanceInfo { get { return !VanillaSpawnRegion.IsNullOrDestroyed() ? VanillaSpawnRegion.GetHashCode().ToString() : "NULL"; } }
        public virtual string TypeInfo { get { return GetType().Name; } }


        public CustomBaseSpawnRegion(SpawnRegion spawnRegion, SpawnRegionModDataProxy dataProxy, TimeOfDay timeOfDay)
        {
            Initialize(spawnRegion, dataProxy, timeOfDay);
        }


        public virtual void Initialize(SpawnRegion spawnRegion, SpawnRegionModDataProxy dataProxy, TimeOfDay timeOfDay)
        {
            if (spawnRegion.IsNullOrDestroyed())
            {
                this.LogErrorInstanced($"Null SpawnRegion");
                return;
            }
            mSpawnRegion = spawnRegion;
            mModDataProxy = dataProxy;
            mTimeOfDay = timeOfDay;
            mManager = Manager.SpawnRegionManager;
            if (!spawnRegion.m_Registered)
            {
                GameManager.m_SpawnRegionManager.Add(mSpawnRegion);
                mSpawnRegion.m_Registered = true;
            }
        }


        public void Despawn(float time)
        {
            mModDataProxy.LastDespawnTime = time;
            mModDataProxy.CurrentPosition = mSpawnRegion.transform.position;
        }


        #region Attempts at vanilla overrides

        private void AddActiveSpawns(int numToActivate, WildlifeMode wildlifeMode)
        {
            if (mSpawnRegion.m_HasBeenDisabledByAurora)
            {
                this.LogTraceInstanced($"Disabled by aurora, aborting");
                return;
            }
            if (!SpawnRegionCloseEnoughForSpawning())
            {
                int forceSpawnCount = 0;
                foreach (Guid spawnGuid in mManager.Manager.DataManager.GetCachedSpawnModDataProxiesByParentGuid(mModDataProxy.Guid))
                {
                    this.LogTraceInstanced($"Trying to fetch spawn mod data proxy for queued spawn guid {spawnGuid} as part of region {mModDataProxy.Guid}");
                    if (!mManager.Manager.DataManager.TryGetActiveSpawnModDataProxy(spawnGuid, out SpawnModDataProxy proxy))
                    {
                        this.LogTraceInstanced($"Cannot fetch proxy for queued spawn guid {spawnGuid}");
                        continue;
                    }
                    if (!proxy.ForceSpawn)
                    {
                        this.LogTraceInstanced($"Proxy with guid {spawnGuid} for type {proxy.VariantSpawnTypeString} is not set to force spawn");
                        continue;
                    }
                    forceSpawnCount++;
                    SpawnDataProxy forceQueuedSpawn = new SpawnDataProxy();
                    forceQueuedSpawn.m_Guid = spawnGuid.ToString();
                    QueueSerializedRespawnPending(forceQueuedSpawn);
                    if (forceSpawnCount >= numToActivate)
                    {
                        break;
                    }
                }
                if (forceSpawnCount > 0)
                {
                    this.LogTraceInstanced($"Force queued {forceSpawnCount} spawns");
                }
                else
                {
                    this.LogTraceInstanced($"Too far for non-forced spawning, aborting");
                }
                return;
            }
            if (numToActivate <= 0)
            {
                this.LogTraceInstanced($"numToActivate (!_{numToActivate}_!) invalid, aborting");
                return;
            }
            Spawn(wildlifeMode);
        }


        private void AdjustActiveSpawnRegionPopulation()
        {
            WildlifeMode currentMode = mSpawnRegion.m_WildlifeMode;
            WildlifeMode oppositeMode = currentMode == WildlifeMode.Normal ? WildlifeMode.Aurora : WildlifeMode.Normal;
            int targetPop = CalculateTargetPopulation();
            int currentActivePopulation = GetCurrentActivePopulation(currentMode);
            int otherModeActivePopulation = GetCurrentActivePopulation(oppositeMode);
            if (otherModeActivePopulation > 0)
            {
                this.LogTraceInstanced($"{otherModeActivePopulation} active wildlife of opposite type, removing");
                RemoveActiveSpawns(otherModeActivePopulation, currentMode, true);
            }
            int targetDelta = targetPop - currentActivePopulation;
            if (targetDelta > 0)
            {
                this.LogTraceInstanced($"{targetDelta} ({currentActivePopulation} vs {targetPop}) missing active wildlife of current type, adding");
                AddActiveSpawns(targetDelta, currentMode);
            }
            else if (targetDelta < 0)
            {
                this.LogTraceInstanced($"{-targetDelta} ({currentActivePopulation} vs {targetPop}) excess active wildlife of current type, removing");
                RemoveActiveSpawns(-targetDelta, currentMode, false);
            }
        }


        public BaseAi AttemptInstantiateAndPlaceSpawnFromSave(WildlifeMode wildlifeMode, PendingSerializedRespawnInfo pendingSerializedRespawnInfo)
        {
            if (pendingSerializedRespawnInfo == null)
            {
                this.LogWarningInstanced($"null PendingSerializedRespawnInfo!");
                return null;
            }
            if (pendingSerializedRespawnInfo.m_SaveData == null)
            {
                this.LogWarningInstanced($"null PendingSerializedRespawnInfo.m_SaveData!");
                return null;
            }
            if (!mManager.Manager.DataManager.TryGetActiveSpawnModDataProxy(new Guid(pendingSerializedRespawnInfo.m_SaveData.m_Guid), out SpawnModDataProxy modDataProxy))
            {
                this.LogTraceInstanced($"No existing SpawnModDataProxy for guid {pendingSerializedRespawnInfo.m_SaveData.m_Guid}, new will be generated during wrap");
            }
            if (!PositionValidForSpawn(pendingSerializedRespawnInfo.m_SaveData.m_Position, modDataProxy))
            {
                this.LogWarningInstanced($"invalid spawn location!");
                return null;
            }
            PlayerManager playerManager = GameManager.m_PlayerManager;
            if (playerManager == null)
            {
                this.LogErrorInstanced($"null PlayerManager");
                return null;
            }
            playerManager.GetTeleportTransformAfterSceneLoad(out Vector3 position, out Quaternion rotation);
            float distanceToPlayer = Vector3.Distance(position, pendingSerializedRespawnInfo.m_SaveData.m_Position);
            Il2Cpp.SpawnRegionManager spawnRegionManager = GameManager.m_SpawnRegionManager;
            if (spawnRegionManager == null)
            {
                this.LogErrorInstanced($"null Il2Cpp.SpawnRegionManager");
                return null;
            }
            float minSpawnDist = spawnRegionManager.m_ClosestSpawnDistanceToPlayerAfterSceneTransition;
            ExperienceModeManager experienceModeManager = GameManager.m_ExperienceModeManager;
            if (experienceModeManager == null)
            {
                this.LogErrorInstanced($"null ExperienceModEmanager");
                return null;
            }
            ExperienceMode currentExperienceMode = experienceModeManager.GetCurrentExperienceMode();
            float closestSpawnDistanceAfterTransitionScale = 1.0f;
            if (currentExperienceMode != null)
            {
                closestSpawnDistanceAfterTransitionScale = currentExperienceMode.m_ClosestSpawnDistanceAfterTransitionScale;
            }
            if (distanceToPlayer < minSpawnDist * closestSpawnDistanceAfterTransitionScale)
            {
                this.LogVerboseInstanced($"Player is too close, aborting");
                return null;
            }
            return InstantiateSpawnFromSaveData(pendingSerializedRespawnInfo.m_SaveData, wildlifeMode);
        }


        private int CalculateTargetPopulation()
        {
            if (SpawningSuppressedByExperienceMode())
            {
                return 0;
            }
            if (!SpawnRegionCloseEnoughForSpawning())
            {
                this.LogVerboseInstanced($"Too far for spawning, returning active population to prevent changes");
                return GetCurrentActivePopulation(mSpawnRegion.m_WildlifeMode);
            }
            if (!mSpawnRegion.m_CanSpawnInBlizzard && GameManager.m_Weather.IsBlizzard())
            {
                this.LogVerboseInstanced($"Cannot spawn in blizzard");
                return 0;
            }
            int maxSimultaneousSpawns = GameManager.m_TimeOfDay.IsDay()
                ? mSpawnRegion.m_DifficultySettings[(int)mSpawnRegion.m_SpawnLevel].m_MaxSimultaneousSpawnsDay
                : mSpawnRegion.m_DifficultySettings[(int)mSpawnRegion.m_SpawnLevel].m_MaxSimultaneousSpawnsNight;
            int adjustedMaxSimultaneousSpawns = maxSimultaneousSpawns - mSpawnRegion.m_NumTrapped - mSpawnRegion.m_NumRespawnsPending;
            if (adjustedMaxSimultaneousSpawns < 0)
            {
                return 0;
            }
            if (mSpawnRegion.m_DifficultySettings[(int)mSpawnRegion.m_SpawnLevel].m_MaxSimultaneousSpawnsDay < adjustedMaxSimultaneousSpawns)
            {
                return mSpawnRegion.m_DifficultySettings[(int)mSpawnRegion.m_SpawnLevel].m_MaxSimultaneousSpawnsDay;
            }
            return adjustedMaxSimultaneousSpawns;
        }


        private bool CanDoReRoll()
        {
            if (mSpawnRegion.m_ControlledByRandomSpawner)
            {
                return false;
            }
            if (!HasDeferredDeserializeCompleted())
            {
                return false;
            }    
            return GetNumActiveSpawns() == 0;
        }


        public bool CanTrap()
        {
            return GameManager.m_TimeOfDay.IsDay()
                ? mSpawnRegion.m_NumTrapped < mSpawnRegion.m_DifficultySettings[(int)mSpawnRegion.m_SpawnLevel].m_MaxSimultaneousSpawnsDay
                : mSpawnRegion.m_NumTrapped < mSpawnRegion.m_DifficultySettings[(int)mSpawnRegion.m_SpawnLevel].m_MaxSimultaneousSpawnsNight;
        }


        public void Deserialize(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                {
                    this.LogErrorInstanced($"Null or empty text, cannot deserialize");
                    return;
                }
                Start();
                SpawnRegionDataProxy proxy = Utils.DeserializeObject<SpawnRegionDataProxy>(text);
                mSpawnRegion.gameObject.SetActive(true);
                mSpawnRegion.m_ElapsedHoursAtLastActiveReRoll = proxy.m_ElapsedHoursAtLastActiveReRoll;
                mSpawnRegion.m_NumRespawnsPending = proxy.m_NumRespawnsPending;
                mSpawnRegion.m_ElapasedHoursNextRespawnAllowed = proxy.m_ElapasedHoursNextRespawnAllowed;
                mSpawnRegion.m_NumTrapped = proxy.m_NumTrapped;
                mSpawnRegion.m_HoursNextTrapReset = proxy.m_HoursNextTrapReset;
                mSpawnRegion.m_SpawnGuidCounter = proxy.m_SpawnGuidCounter;
                mSpawnRegion.m_CurrentWaypointPathIndex = proxy.m_CurrentWaypointPathIndex;
                mSpawnRegion.m_WildlifeMode = proxy.m_WildlifeMode;
                mSpawnRegion.m_HasBeenDisabledByAurora = proxy.m_HasBeenDisabledByAurora;
                mSpawnRegion.m_WasActiveBeforeAurora = proxy.m_WasActiveBeforeAurora;
                SetBoundingSphereBasedOnWaypoints(proxy.m_CurrentWaypointPathIndex);
                mSpawnRegion.m_CooldownTimerHours = Il2Cpp.SpawnRegion.m_SpawnRegionDataProxy.m_CooldownTimerHours;
                mSpawnRegion.m_DeferredSpawnWildlifeMode = proxy.m_WildlifeMode;
                if (GetCurrentTimelinePoint() - proxy.m_HoursPlayed < GameManager.m_SpawnRegionManager.m_RandomizeRestoredSpawnsAfterHoursInside)
                {
                    foreach (SpawnDataProxy spawnProxy in proxy.m_ActiveSpawns)
                    {
                        mSpawnRegion.m_DeferredSpawnsWithSavedPosition.Add(spawnProxy);
                    }
                    mSpawnRegion.m_DeferredSpawnWildlifeMode = proxy.m_WildlifeMode;
                }
                else
                {
                    MaybeReRollActive();
                    if (!mSpawnRegion.gameObject.activeInHierarchy)
                    {
                        UpdateDeferredDeserialize();
                        return;
                    }
                    foreach (SpawnDataProxy spawnProxy in proxy.m_ActiveSpawns)
                    {
                        mSpawnRegion.m_DeferredSpawnsWithRandomPosition.Add(spawnProxy);
                    }
                }
                UpdateDeferredDeserialize();
            }
            catch (Exception e)
            {
                this.LogErrorInstanced($"Deserialize error SpawnRegion with hash code {VanillaSpawnRegion.GetHashCode()}: {e}");
            }
        }


        public GameObject GetClosestActiveSpawn(Vector3 pos)
        {
            float closestDist = float.MaxValue;
            GameObject closestObj = null;
            for (int i = 0, iMax = mSpawnRegion.m_Spawns.Count; i < iMax; i++)
            {
                if (mSpawnRegion.m_Spawns[i].IsNullOrDestroyed())
                {
                    continue;
                }
                if (!mSpawnRegion.m_Spawns[i].gameObject.activeSelf)
                {
                    continue;
                }
                float currentDist = SquaredDistance(pos, mSpawnRegion.m_Spawns[i].transform.position);
                if (currentDist < closestDist)
                {
                    closestDist = currentDist;
                    closestObj = mSpawnRegion.m_Spawns[i].gameObject;
                }
            }
            return closestObj;
        }


        private int GetCurrentActivePopulation(WildlifeMode wildlifeMode)
        {
            int count = 0;
            for (int i = 0, iMax = mSpawnRegion.m_Spawns.Count; i < iMax; i++)
            {
                if (mSpawnRegion.m_Spawns[i].m_WildlifeMode != wildlifeMode)
                {
                    continue;
                }
                if (!mSpawnRegion.m_Spawns[i].gameObject.activeSelf)
                {
                    continue;
                }
                count++;
            }
            return count;
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


        private int GetMaxSimultaneousSpawnsDay()
        {
            if (mSpawnRegion.m_DifficultySettings == null)
            {
                this.LogErrorInstanced($"Null mSpawnRegion.m_DifficultySettings");
                return 0;
            }
            return mSpawnRegion.m_DifficultySettings[(int)mSpawnRegion.m_SpawnLevel].m_MaxSimultaneousSpawnsDay;
        }


        private int GetMaxSimultaneousSpawnsNight()
        {
            if (mSpawnRegion.m_DifficultySettings == null)
            {
                this.LogVerboseInstanced($"Null mSpawnRegion.m_DifficultySettings");
                return 0;
            }
            return mSpawnRegion.m_DifficultySettings[(int)mSpawnRegion.m_SpawnLevel].m_MaxSimultaneousSpawnsNight;
        }


        public int GetNumActiveSpawns()
        {
            if (mSpawnRegion.gameObject.IsNullOrDestroyed())
            {
                this.LogErrorInstanced($"Null mSpawnRegion.gameObject");
                return 0;
            }    
            if (!mSpawnRegion.gameObject.activeSelf)
            {
                this.LogErrorInstanced($"Inactive mSpawnRegion.gameObject");
                return 0;
            }
            if (mSpawnRegion.m_Spawns.IsNullOrDestroyed())
            {
                this.LogErrorInstanced($"Null mSpawnRegion.m_Spawns");
                return 0;
            }
            return GetCurrentActivePopulation(mSpawnRegion.m_WildlifeMode);
        }


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


        public WanderRegion GetWanderRegion(Vector3 pos)
        {
            foreach(WanderRegion wanderRegion in mSpawnRegion.gameObject.GetComponentsInChildren<WanderRegion>())
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



        private bool HasDeferredDeserializeCompleted()
        {
            if (mSpawnRegion.m_DeferredSpawnsWithRandomPosition == null)
            {
                this.LogErrorInstanced($"Null mSpawnRegion.m_DeferredSpawnsWithRandomPosition");
                return false;
            }
            if (mSpawnRegion.m_DeferredSpawnsWithRandomPosition.Count != 0)
            {
                this.LogVerboseInstanced($"Remaining deferred spawns with random position");
                return false;
            }
            if (mSpawnRegion.m_DeferredSpawnsWithSavedPosition == null)
            {
                this.LogErrorInstanced($"Null mSpawnRegion.m_DeferredSpawnsWithSavedPosition");
                return false;
            }
            if (mSpawnRegion.m_DeferredSpawnsWithSavedPosition.Count != 0)
            {
                this.LogVerboseInstanced($"Remaining deferred spawns with saved position");
                return false;
            }
            return true;
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


        private bool HasSerializedRespawnPending()
        {
            if (mSpawnRegion.m_PendingSerializedRespawnInfoQueue.IsNullOrDestroyed())
            {
                this.LogErrorInstanced($"Null mSpawnRegion.m_PendingSerializedRespawnInfoQueue");
                return false;
            }
            return mSpawnRegion.m_PendingSerializedRespawnInfoQueue.Count != 0;
        }


        private BaseAi InstantiateAndPlaceSpawn(WildlifeMode wildlifeMode)
        {
            if (mSpawnRegion.m_AiSubTypeSpawned == AiSubType.Cougar && GetCurrentTimelinePoint() < mSpawnRegion.m_CooldownTimerHours)
            {
                this.LogVerboseInstanced($"Cougar timer has not expired, aborting");
                return null;
            }
            AssetReferenceAnimalPrefab animalReferencePrefab = null;
            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;
            if (mSpawnRegion.m_SpawnablePrefab == null)
            {
                this.LogVerboseInstanced($"Null spawnable prefab on spawn region, fetching...");
                animalReferencePrefab = mSpawnRegion.m_SpawnRegionAnimalTableSO.PickSpawnAnimal(wildlifeMode);
                mSpawnRegion.m_SpawnablePrefab = animalReferencePrefab.GetOrLoadAsset();
                animalReferencePrefab.ReleaseAsset();
            }
            if (!TryGetSpawnPositionAndRotation(ref spawnPosition, ref spawnRotation))
            {
                this.LogWarningInstanced($"Potential error: Could not get spawn position and rotation. Aborting");
                return null;
            }
            if (!PositionValidForSpawn(spawnPosition, null))
            {
                this.LogVerboseInstanced($"Invalid spawn placement. Aborting");
                return null;
            }
            AiMode aiMode = AiMode.FollowWaypoints;
            if (mSpawnRegion.m_PathManagers == null || mSpawnRegion.m_PathManagers.Count == 0)
            {
                this.LogVerboseInstanced($"Path manager null or zero count, setting mode to wander");
                aiMode = AiMode.Wander;
            }
            this.LogVerboseInstanced($"success!");
            return InstantiateSpawn(mSpawnRegion.m_SpawnablePrefab, animalReferencePrefab, spawnPosition, spawnRotation, aiMode, wildlifeMode);
        }


        private BaseAi InstantiateSpawn(GameObject spawnablePrefab, AssetReferenceAnimalPrefab assetRef, Vector3 spawnPos, Quaternion spawnRot, AiMode aiMode, WildlifeMode wildlifeMode)
        {
            CustomBaseAi newCustombaseAi = InstantiateSpawnInternal(spawnablePrefab, wildlifeMode, spawnPos, spawnRot);
            BaseAi baseAi = newCustombaseAi.BaseAi;
            if (baseAi == null)
            {
                this.LogVerboseInstanced($"Null BaseAi received from InstantiateSpawnInternal, cascading");
                return null;
            }
            baseAi.transform.position = spawnPos;
            baseAi.transform.rotation = spawnRot;
            Transform transform = mSpawnRegion.transform;
            if (mSpawnRegion.m_WanderRegion != null)
            {
                this.LogVerboseInstanced($"Wander region found, setting move agent transform to wander region?");
                transform = mSpawnRegion.m_WanderRegion.transform;
            }
            if (BaseAiManager.CreateMoveAgent(transform, baseAi, spawnPos))
            {
                baseAi.ReparentBaseAi(transform, true);
            }
            baseAi.m_SpawnPos = baseAi.transform.position;
            baseAi.SetAiMode(aiMode);
            baseAi.m_StartMode = aiMode;
            AiDifficultySetting setting = GameManager.m_AiDifficultySettings.GetSetting(mSpawnRegion.m_AiDifficulty, mSpawnRegion.m_AiSubTypeSpawned);
            baseAi.m_AiDifficultySetting = setting;
            ObjectGuid.MaybeAttachObjectGuidAndRegister(baseAi.gameObject, newCustombaseAi.ModDataProxy.Guid.ToString());
            mSpawnRegion.m_Spawns.Add(baseAi);
            mSpawnRegion.m_SpawnsPrefabReferences.Add(assetRef);
            this.LogVerboseInstanced($"success!");
            return baseAi;
        }


        private BaseAi InstantiateSpawnFromSaveData(SpawnDataProxy spawnData, WildlifeMode wildlifeMode)
        {
            if (spawnData.IsNullOrDestroyed())
            {
                this.LogErrorInstanced($"Null spawnData, aborting");
                return null;
            }
            if (!AiUtils.IsNavmeshPosValid(spawnData.m_Position, 0.5f, 1.0f))
            {
                this.LogWarningInstanced($"Invalid spawn position per AiUtils.IsNavmeshPosValid, aborting");
                return null;
            }
            AssetReferenceAnimalPrefab assetRef = null;
            GameObject spawnablePrefab = mSpawnRegion.m_SpawnablePrefab;
            if (spawnablePrefab.IsNullOrDestroyed())
            {
                this.LogVerboseInstanced($"Null spawnable prefab on spawn region, fetching...");
                assetRef = mSpawnRegion.m_SpawnRegionAnimalTableSO.PickSpawnAnimal(wildlifeMode);
                mSpawnRegion.m_SpawnablePrefab = assetRef.GetOrLoadAsset();
                assetRef.ReleaseAsset();

            }
            if (!mManager.Manager.DataManager.TryGetActiveSpawnModDataProxy(new Guid(spawnData.m_Guid), out SpawnModDataProxy existingModDataProxy))
            {
                this.LogTraceInstanced($"No existing pre-queued mod data proxy, new instance will be generated during wrapping");
            }
            BaseAi baseAi = InstantiateSpawnInternal(spawnablePrefab, wildlifeMode, spawnData.m_Position, spawnData.m_Rotation, existingModDataProxy).BaseAi;
            if (baseAi.IsNullOrDestroyed())
            {
                this.LogWarningInstanced($"InstantiateSpawnInternal returned null BaseAi, aborting");
                return null;
            }
            if (baseAi.transform == null)
            {
                this.LogErrorInstanced($"BaseAi has null transform, aborting");
                return null;
            }
            //This should be handled by InstantiateSpawnInternal!
            //baseAi.transform.position = spawnData.m_Position;
            //baseAi.transform.rotation = spawnData.m_Rotation;
            Transform transform = mSpawnRegion.transform;
            if (mSpawnRegion.m_WanderRegion != null)
            {
                this.LogVerboseInstanced($"Wander region found, setting move agent transform to wander region?");
                transform = mSpawnRegion.m_WanderRegion.transform;
            }
            if (BaseAiManager.CreateMoveAgent(transform, baseAi, spawnData.m_Position))
            {
                baseAi.ReparentBaseAi(transform, true);
            }
            baseAi.SetSpawnRegionParent(mSpawnRegion);
            AiDifficultySettings aiDifficultySettings = GameManager.m_AiDifficultySettings;
            if (aiDifficultySettings.IsNullOrDestroyed())
            {
                this.LogErrorInstanced($"Null AiDifficultySettings, aborting");
                return null;
            }
            baseAi.m_AiDifficultySetting = aiDifficultySettings.GetSetting(mSpawnRegion.m_AiDifficulty, baseAi.m_AiSubType);
            if (spawnData.m_Guid == null || spawnData.m_Guid.Length == 0)
            {
                this.LogTraceInstanced($"Generating new PDID");
                spawnData.m_Guid = PdidTable.GenerateNewID();
            }
            ObjectGuid.MaybeAttachObjectGuidAndRegister(baseAi.gameObject, spawnData.m_Guid);
            baseAi.Deserialize(spawnData.m_BaseAiSerialized);
            mSpawnRegion.m_Spawns.Add(baseAi);
            mSpawnRegion.m_SpawnsPrefabReferences.Add(assetRef);
            return baseAi;
        }


        private CustomBaseAi InstantiateSpawnInternal(GameObject spawnablePrefab, WildlifeMode wildlifeMode, Vector3 spawnPos, Quaternion spawnRot, SpawnModDataProxy modDataProxy = null)
        {
            if (mSpawnRegion.m_HasBeenDisabledByAurora)
            {
                this.LogVerboseInstanced($"Disabled by aurora, aborting");
                return null;
            }
            if (mSpawnRegion.m_AuroraSpawnablePrefab != null && wildlifeMode == WildlifeMode.Aurora)
            {
                this.LogVerboseInstanced($"Wildlife mode is aurora and aurora spawnable prefab available, overriding param prefab");
                spawnablePrefab = mSpawnRegion.m_AuroraSpawnablePrefab;
            }
            GameObject newInstance = GameObject.Instantiate(spawnablePrefab, spawnPos, spawnRot);
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
            if (!mManager.TryWrapNewSpawn(newBaseAi, VanillaSpawnRegion, out CustomBaseAi newCustomBaseAi, modDataProxy))
            {
                this.LogErrorInstanced($"Error wrapping new spawn!");
                return null;
            }
            return newCustomBaseAi;
        }


        private bool IsPredator() => mSpawnRegion.m_AiTypeSpawned == AiType.Predator;


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


        private void MaybeRegisterWithManager()
        {
            if (mSpawnRegion.m_Registered)
            {
                return;
            }
            if (GameManager.m_SpawnRegionManager == null)
            {
                this.LogErrorInstanced($"Null SpawnRegionManager");
                return;
            }
            GameManager.m_SpawnRegionManager.Add(mSpawnRegion);
            mSpawnRegion.m_Registered = true;
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


        private void MaybeResumeAfterAurora()
        {
            if (mSpawnRegion.m_WasActiveBeforeAurora)
            {
                mSpawnRegion.gameObject.SetActive(true);
            }
            mSpawnRegion.m_HasBeenDisabledByAurora = false;
            return;
        }


        private BaseAi MaybeSpawnPendingSerializedRespawn(WildlifeMode wildlifeMode)
        {
            if (mSpawnRegion.m_PendingSerializedRespawnInfoQueue.Count == 0)
            {
                this.LogVerboseInstanced($"No pending serialized respawn info, aborting");
                return null;
            }
            PendingSerializedRespawnInfo pendingSerializedRespawnInfo = mSpawnRegion.m_PendingSerializedRespawnInfoQueue.Dequeue();
            BaseAi respawnedAi = AttemptInstantiateAndPlaceSpawnFromSave(wildlifeMode, pendingSerializedRespawnInfo);
            if (respawnedAi != null)
            {
                this.LogVerboseInstanced($"Successfully respawned AI from pending serialized respawn info!");
                return respawnedAi;
            }
            pendingSerializedRespawnInfo.m_TrySpawnCount += 1;
            if (pendingSerializedRespawnInfo.m_TrySpawnCount < 0x3d)
            {
                this.LogVerboseInstanced($"Failed to respawn Ai from pending serialized respawn info {pendingSerializedRespawnInfo.m_TrySpawnCount} times, re-queueing");
                mSpawnRegion.m_PendingSerializedRespawnInfoQueue.Enqueue(pendingSerializedRespawnInfo);
            }
            else
            {
                this.LogWarningInstanced($"Failed to respawn Ai from pending serialized respawn info {pendingSerializedRespawnInfo.m_TrySpawnCount} times, disposing");
            }
            return null;
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

        //not yet hooked up, looks lilke
        private void OnDestroy()
        {
            GameManager.m_SpawnRegionManager.m_SpawnRegions.Remove(mSpawnRegion);
        }


        private bool PositionValidForSpawn(Vector3 spawnPosition, SpawnModDataProxy modDataProxy)
        {
            if (GameManager.m_SpawnRegionManager.PointInsideNoSpawnRegion(spawnPosition))
            {
                this.LogVerboseInstanced($"Encountered NoSpawn region");
                return false;
            }
            if (GameManager.m_Weather.IsIndoorEnvironment())
            {
                this.LogVerboseInstanced($"Encountered indoor environment... why does this automatically return true???");
                return true;
            }
            if (modDataProxy != null && modDataProxy.ForceSpawn)
            {
                this.LogTraceInstanced($"Force spawn enabled for spawn guid {modDataProxy.Guid}");
                return true;
            }
            if (SpawnPositionOnScreenTooClose(spawnPosition))
            {
                this.LogVerboseInstanced($"Spawn position on screen too close");
                return false;
            }
            if (SpawnPositionTooCloseToCamera(spawnPosition))
            {
                this.LogVerboseInstanced($"Spawn position too close to camera");
                return false;
            }
            return true;
        }


        private void QueueSerializedRespawnPending(SpawnDataProxy saveData)
        {
            PendingSerializedRespawnInfo respawnInfo = new PendingSerializedRespawnInfo();
            respawnInfo.m_SaveData = saveData;
            respawnInfo.m_TrySpawnCount = 0;
            mSpawnRegion.m_PendingSerializedRespawnInfoQueue.Enqueue(respawnInfo);
        }


        private void RemoveActiveSpawns(int numToDeactivate, WildlifeMode wildlifeMode, bool isAdjustingOtherWildlifeMode)
        {
            bool playerGhost = GameManager.m_PlayerManager.m_Ghost;
            for (int i = 0, iMax = mSpawnRegion.m_Spawns.Count; i < iMax && numToDeactivate > 0; i++)
            {
                BaseAi spawn = mSpawnRegion.m_Spawns[i];
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


        public void RemoveFromSpawnRegion(BaseAi baseAi)
        {
            if (mSpawnRegion.m_Spawns.IsNullOrDestroyed())
            {
                this.LogErrorInstanced($"Null m_Spawns");
                return;
            }
            if (mSpawnRegion.m_Spawns.Contains(baseAi))
            {
                this.LogVerboseInstanced($"BaseAI not found in m_Spawns");
                return;
            }
            int removeIndex = mSpawnRegion.m_Spawns.IndexOf(baseAi);
            if (mSpawnRegion.m_Spawns.Count <= removeIndex || mSpawnRegion.m_SpawnsPrefabReferences.Count <= removeIndex)
            {
                this.LogErrorInstanced($"Remove index out of range");
                return;
            }
            mSpawnRegion.m_Spawns.RemoveAt(removeIndex);
            mSpawnRegion.m_SpawnsPrefabReferences.RemoveAt(removeIndex);
            mSpawnRegion.m_NumRespawnsPending++;
            mSpawnRegion.m_ElapasedHoursNextRespawnAllowed = GetCurrentTimelinePoint() + GetNumHoursBetweenRespawns();
        }
        

        private void Respawn(WildlifeMode wildlifeMode)
        {
            if (!Spawn(wildlifeMode))
            {
                this.LogVerboseInstanced($"Spawn failed, aborting");
                return;
            }
            mSpawnRegion.m_NumRespawnsPending--;
            mSpawnRegion.m_ElapasedHoursNextRespawnAllowed = GetCurrentTimelinePoint() + GetNumHoursBetweenRespawns();
            this.LogVerboseInstanced($"Respawn successful; pending respawns: !_{mSpawnRegion.m_NumRespawnsPending}_! next respawn time: !_{mSpawnRegion.m_ElapasedHoursNextRespawnAllowed}_! hours");
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


        //Note to self: system re-uses a static SpawnRegionModDataProxy for serialization rather than creating one per region.
        //Since it's just a transfer container, makes some sense for garbage collector optimization
        public string Serialize()
        {
            try
            {
                SpawnRegionDataProxy regionProxy = Il2Cpp.SpawnRegion.m_SpawnRegionDataProxy;
                regionProxy.m_HoursPlayed = GetCurrentTimelinePoint();
                regionProxy.m_ElapsedHoursAtLastActiveReRoll = mSpawnRegion.m_ElapsedHoursAtLastActiveReRoll;
                regionProxy.m_IsActive = mSpawnRegion.gameObject.activeSelf;
                regionProxy.m_NumRespawnsPending = mSpawnRegion.m_NumRespawnsPending;
                if (mSpawnRegion.m_OnlyOneChance)
                {
                    mSpawnRegion.m_ElapasedHoursNextRespawnAllowed = float.MaxValue;
                }
                regionProxy.m_ElapasedHoursNextRespawnAllowed = mSpawnRegion.m_OnlyOneChance ? float.MaxValue : mSpawnRegion.m_ElapasedHoursNextRespawnAllowed;
                regionProxy.m_NumTrapped = mSpawnRegion.m_NumTrapped;
                regionProxy.m_HoursNextTrapReset = mSpawnRegion.m_HoursNextTrapReset;
                regionProxy.m_ActiveSpawns.Clear();
                regionProxy.m_SpawnGuidCounter = mSpawnRegion.m_SpawnGuidCounter;
                regionProxy.m_CurrentWaypointPathIndex = mSpawnRegion.m_CurrentWaypointPathIndex;
                regionProxy.m_WildlifeMode = mSpawnRegion.m_WildlifeMode;
                regionProxy.m_HasBeenDisabledByAurora = mSpawnRegion.m_HasBeenDisabledByAurora;
                regionProxy.m_WasActiveBeforeAurora = mSpawnRegion.m_WasActiveBeforeAurora;
                regionProxy.m_CooldownTimerHours = mSpawnRegion.m_CooldownTimerHours;
                for (int i = 0, iMax = mSpawnRegion.m_Spawns.Count; i < iMax; i++)
                {
                    try
                    {
                        if (mSpawnRegion.m_Spawns[i].IsNullOrDestroyed())
                        {
                            this.LogWarningInstanced($"Null BaseAI, skipping");
                            continue;
                        }
                        if (!mSpawnRegion.m_Spawns[i].gameObject.activeSelf)
                        {
                            this.LogVerboseInstanced($"Inactive BaseAi, skipping");
                            continue;
                        }
                        SpawnDataProxy spawnProxy = i < 0x80 ? Il2Cpp.SpawnRegion.m_SpawnDataProxyPool[i] : new SpawnDataProxy();
                        spawnProxy.m_Position = mSpawnRegion.m_Spawns[i].transform.position;
                        spawnProxy.m_Rotation = mSpawnRegion.m_Spawns[i].transform.rotation;
                        spawnProxy.m_Guid = ObjectGuid.GetGuidFromGameObject(mSpawnRegion.m_Spawns[i].gameObject);
                        //spawnProxy.m_AssetReferenceGUID = mSpawnRegion.m_SpawnsPrefabReferences[i].AssetGUID;
                        spawnProxy.m_BaseAiSerialized = mSpawnRegion.m_Spawns[i].Serialize();
                        regionProxy.m_ActiveSpawns.Add(spawnProxy);
                    }
                    catch (Exception e)
                    {
                        this.LogErrorInstanced($"Serialization failure on spawn #{i} on Spawn Region with hash code {VanillaSpawnRegion.GetHashCode()}: {e}");
                    }
                }
                return SerializationUtils.SerializeObject(regionProxy);
            }
            catch (Exception e)
            {
                this.LogErrorInstanced($"Serialization error on spawn region with hash code {VanillaSpawnRegion.GetHashCode()}: {e}");
                return string.Empty;
            }
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
                for (int i = 0, iMax = mSpawnRegion.m_Spawns.Count; i < iMax; i++)
                {
                    spawn = mSpawnRegion.m_Spawns[i];
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


        private void SetBoundingSphereBasedOnWaypoints(int waypointIndex)
        {
            if (waypointIndex < 0 || waypointIndex >= mSpawnRegion.m_PathManagers.Length)
            {
                this.LogErrorInstanced($"Invalid waypoint index !_{waypointIndex}_! (waypoints available: !_{mSpawnRegion.m_PathManagers.Length}_!)");
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


        private void SetRespawnCooldownTimer(float cooldownHours)
        {
            mSpawnRegion.m_CooldownTimerHours = GetCurrentTimelinePoint() + cooldownHours;
            this.LogVerboseInstanced($"Setting cooldown time to !_{mSpawnRegion.m_CooldownTimerHours}_!");
        }


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


        private bool Spawn(WildlifeMode wildlifeMode)
        {
            if (mSpawnRegion.m_HasBeenDisabledByAurora)
            {
                this.LogVerboseInstanced($"Spawn region disabled by aurora, skipping");
                return false;
            }
            if (GameManager.m_IsPaused)
            {
                this.LogVerboseInstanced($"Paused");
                return false;
            }
            if (mSpawnRegion.m_Spawns == null)
            {
                this.LogVerboseInstanced($"Spawn region with null spawn list!");
                return false;
            }
            int skipCount = 0;
            foreach (BaseAi spawn in mSpawnRegion.m_Spawns)
            {
                if (spawn == null)
                {
                    this.LogVerboseInstanced($"Null spawn in m_Spawns, skipping");
                    continue;
                }
                if (spawn.gameObject == null)
                {
                    this.LogVerboseInstanced($"Null game object in m_Spawns, skipping");
                    continue;
                }
                if (spawn.gameObject.activeSelf)
                {
                    this.LogVerboseInstanced($"Spawn is active in m_Spawns, skipping");
                    continue;
                }
                if (spawn.m_WildlifeMode != mSpawnRegion.m_WildlifeMode)
                {
                    this.LogVerboseInstanced($"Spawn wildlife mode <<<{spawn.m_WildlifeMode}>>> does not match region wildlife mode <<<{mSpawnRegion.m_WildlifeMode}>>>, skipping");
                    continue;
                }
                Vector3 position = spawn.transform.position;
                bool forceSpawn = mManager.Manager.AiManager.CustomAis.TryGetValue(spawn.GetHashCode(), out CustomBaseAi customBaseAi) && customBaseAi.ModDataProxy.ForceSpawn;
                if (forceSpawn)
                {
                    this.LogVerboseInstanced($"Force spawn detected");
                }
                if (!forceSpawn && (SpawnPositionOnScreenTooClose(position) || SpawnPositionTooCloseToCamera(position)))
                {
                    skipCount++;
                    this.LogVerboseInstanced($"Spawn position on screen or too close to camera, skipping");
                    continue;
                }
                spawn.gameObject.SetActive(true);
                spawn.SetAiMode(spawn.m_DefaultMode); //Should patch through via harmony to CustomBaseAi version
                return true; 
            }
            if (skipCount != 0)
            {
                this.LogVerboseInstanced($"Skipped at least one inactive instantiated spawn without activating any, aborting");
                return false;
            }
            if (mSpawnRegion.m_PendingSerializedRespawnInfoQueue.Count >= 1)
            {
                return MaybeSpawnPendingSerializedRespawn(wildlifeMode) != null;
            }
            else
            {
                if (mManager.Manager.DataManager.GetCachedSpawnModDataProxiesByParentGuid(mModDataProxy.Guid).Count <= 0)
                {
                    LogTrace($"Waiting for pre-queues...");
                    return false;
                }
                return InstantiateAndPlaceSpawn(wildlifeMode) != null;
            }
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
            return mSpawnRegion.m_Radius + GameManager.m_SpawnRegionManager.m_SpawnRegionDisableDistance >= Utils.DistanceToMainCamera(mSpawnRegion.m_Center);
        }

        
        private void SpawnWithRandomPositions(Il2CppSystem.Collections.Generic.List<SpawnDataProxy> spawns, WildlifeMode wildlifeMode)
        {
            if (mSpawnRegion.m_DifficultySettings == null)
            {
                this.LogErrorInstanced($"Null DifficultySettings");
                return;
            }
            if (mSpawnRegion.m_DifficultySettings.Length <= (int)mSpawnRegion.m_SpawnLevel)
            {
                this.LogErrorInstanced($"Not enough entries in DifficultySettings for spawn level");
                return;
            }
            SpawnRegion.DifficultyProperties difficultyProperty = mSpawnRegion.m_DifficultySettings[(int)mSpawnRegion.m_SpawnLevel];
            if (difficultyProperty.IsNullOrDestroyed())
            {
                this.LogErrorInstanced($"Null DifficultyProperty");
                return;
            }
            int spawnCount = 0;
            foreach (SpawnDataProxy spawn in spawns)
            {
                if (spawn.IsNullOrDestroyed())
                {
                    this.LogWarningInstanced($"Null SpawnDataProxy");
                    continue;
                }
                if (spawnCount < difficultyProperty.m_MaxSimultaneousSpawnsDay)
                {
                    if (spawn.m_AiMode == AiMode.Sleep || spawn.m_AiMode == AiMode.Feeding)
                    {
                        BaseAi newSpawn = InstantiateSpawnFromSaveData(spawn, wildlifeMode);
                        if (!newSpawn.IsNullOrDestroyed())
                        {
                            spawnCount++;
                            continue;
                        }
                    }
                    QueueSerializedRespawnPending(spawn);
                }
                if (spawnCount >= difficultyProperty.m_MaxSimultaneousSpawnsDay)
                {
                    this.LogVerboseInstanced($"Maximum spawns reached");
                    return;
                }
            }
        }


        private void SpawnWithSavedPositions(Il2CppSystem.Collections.Generic.List<SpawnDataProxy> spawns, WildlifeMode wildlifeMode)
        {
            foreach (SpawnDataProxy spawn in spawns)
            {
                if (spawn.IsNullOrDestroyed())
                {
                    this.LogWarningInstanced($"Null SpawnDataProxy");
                    continue;
                }
                QueueSerializedRespawnPending(spawn);
            }
        }


        public void Start()
        {
            if (mSpawnRegion.m_StartHasBeenCalled)
            {
                return;
            }
            mSpawnRegion.m_StartHasBeenCalled = true;
            mSpawnRegion.m_AiTypeSpawned = AiType.Ambient;
            if (mSpawnRegion.m_SpawnablePrefab.IsNullOrDestroyed())
            {
                this.LogVerboseInstanced($"null spawnable prefab on spawn region, fetching...");
                AssetReferenceAnimalPrefab animalReferencePrefab = mSpawnRegion.m_SpawnRegionAnimalTableSO.PickSpawnAnimal(WildlifeMode.Normal);
                mSpawnRegion.m_SpawnablePrefab = animalReferencePrefab.GetOrLoadAsset();
                animalReferencePrefab.ReleaseAsset();
            }
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
            mSpawnRegion.m_Spawns.Clear();
            mSpawnRegion.m_SpawnsPrefabReferences.Clear();
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
            mManager.QueueNewSpawns(this);
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


        public void UpdateDeferredDeserialize()
        {
            if (mSpawnRegion.m_DeferredSpawnsWithRandomPosition.Count > 0)
            {
                this.LogVerboseInstanced($"Found deferred spawns with random position, spawning immediately");
                SpawnWithRandomPositions(mSpawnRegion.m_DeferredSpawnsWithRandomPosition, mSpawnRegion.m_DeferredSpawnWildlifeMode);
                mSpawnRegion.m_DeferredSpawnsWithRandomPosition.Clear();
            }
            if (mSpawnRegion.m_DeferredSpawnsWithSavedPosition.Count > 0)
            {
                this.LogVerboseInstanced($"Found deferred spawns with saved position, Queueing");
                for (int i = 0, iMax = mSpawnRegion.m_DeferredSpawnsWithSavedPosition.Count; i < iMax; i++)
                {
                    QueueSerializedRespawnPending(mSpawnRegion.m_DeferredSpawnsWithSavedPosition[i]);
                }
                mSpawnRegion.m_DeferredSpawnsWithSavedPosition.Clear();
            }
        }


        public void UpdateDeferredDeserializeFromManager()
        {
            if (!HasDeferredDeserializeCompleted())
            {
                UpdateDeferredDeserialize();
            }
        }


        public void UpdateFromManager()
        {
            if (!HasDeferredDeserializeCompleted())
            {
                this.LogVerboseInstanced($"Awaiting HasDeferredDeserializeCompleted");
                return;
            }
            if (mSpawnRegion.m_HasBeenDisabledByAurora)
            {
                mSpawnRegion.gameObject.SetActive(false);
            }
            AdjustActiveSpawnRegionPopulation(); 
            MaybeReduceNumTrapped();
        }

        #endregion
    }
}