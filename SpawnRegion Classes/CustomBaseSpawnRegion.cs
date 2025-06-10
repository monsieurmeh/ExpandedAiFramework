using Harmony;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.Runtime;
using Il2CppNodeCanvas.Tasks.Actions;
using Il2CppRewired.Utils;
using Il2CppSystem.Runtime.InteropServices;
using Il2CppTLD.AI;
using Il2CppTLD.Gameplay;
using Il2CppTLD.Gameplay.Tunable;
using Il2CppTLD.PDID;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static Il2Cpp.UITweener;
using static MelonLoader.Modules.MelonModule;


namespace ExpandedAiFramework
{
    //right now this is just a spawn region wrapper holding a proxy and itself serving as an index.
    //Eventually might grow. Doing most stuff with managers right now, messy but works at least.
    //[RegisterTypeInIl2Cpp]
    public class CustomBaseSpawnRegion //: MonoBehaviour
    {
        //public CustomBaseSpawnRegion(IntPtr intPtr) : base(intPtr) { }

        protected SpawnRegion mSpawnRegion;
        protected TimeOfDay mTimeOfDay;
        protected EAFManager mManager;
        protected SpawnRegionModDataProxy mModDataProxy;

        public SpawnRegion SpawnRegion { get { return mSpawnRegion; } }
        //public Component Self { get { return this; } }
        public SpawnRegionModDataProxy ModDataProxy { get { return mModDataProxy; } }


        public CustomBaseSpawnRegion(SpawnRegion spawnRegion, SpawnRegionModDataProxy dataProxy, TimeOfDay timeOfDay)
        {
            Initialize(spawnRegion, dataProxy, timeOfDay);
        }

        //One day I might uproot this, but it's not tiny (~500 lines decompiled) and unless we want to start adjusting spawn rate *mechanics* (not just numeric values) we can probably leave it alone. Just need to hook into it for registration
        //public void OverrideStart()
        //{
        //if (!OverrideStartCustom())
        //{
        // return;
        //}
        //mSpawnRegion.Start();
        //}


        //protected virtual bool OverrideStartCustom() => true;


        public virtual void Initialize(SpawnRegion spawnRegion, SpawnRegionModDataProxy dataProxy, TimeOfDay timeOfDay)
        {
            mSpawnRegion = spawnRegion;
            mModDataProxy = dataProxy;
            mTimeOfDay = timeOfDay;
            mManager = Manager;// manager;
        }


        public void Despawn(float time)
        {
            mModDataProxy.LastDespawnTime = time;
            mModDataProxy.CurrentPosition = mSpawnRegion.transform.position;
        }


        #region Attempts at vanilla overrides

        public void AddActiveSpawns(int numToActivate, WildlifeMode wildlifeMode)
        {
            if (mSpawnRegion.m_HasBeenDisabledByAurora)
            {
                LogTrace($"Disabled by aurora, aborting");
                return;
            }
            if (!SpawnRegionCloseEnoughForSpawning())
            {
                LogTrace($"Too far for spawning, aborting");
                return;
            }
            if (numToActivate <= 0)
            {
                LogTrace($"numToActivate (!_{numToActivate}_!) invalid, aborting");
                return;
            }
            LogTrace($"AddActiveSpawns -> Spawn");
            Spawn(wildlifeMode);
        }


        public void AdjustActiveSpawnRegionPopulation()
        {
            int targetPopulation = CalculateTargetPopulation();
            WildlifeMode currentMode = mSpawnRegion.m_WildlifeMode;
            WildlifeMode oppositeMode = currentMode == WildlifeMode.Normal ? WildlifeMode.Aurora : WildlifeMode.Normal;

            int oppositeActive = GetCurrentActivePopulation(oppositeMode);
            if (oppositeActive > 0)
            {
                LogTrace($"!_{oppositeActive}_! active wildlife of opposite type, removing");
                RemoveActiveSpawns(oppositeActive, oppositeMode, true);
            }

            int currentActive = GetCurrentActivePopulation(currentMode);
            int deficit = targetPopulation - currentActive;

            if (deficit < 0)
            {
                LogTrace($"!_{-deficit}_! excess active wildlife of current type, removing");
                RemoveActiveSpawns(-deficit, currentMode, false);
                return;
            }

            if (deficit > 0 &&
                !mSpawnRegion.m_HasBeenDisabledByAurora &&
                SpawnRegionCloseEnoughForSpawning())
            {
                LogTrace($"!_{deficit}_! unspawned active wildlife of current type, spawning");
                Spawn(currentMode);
            }
        }


        public BaseAi AttemptInstantiateAndPlaceSpawnFromSave(WildlifeMode wildlifeMode, PendingSerializedRespawnInfo pendingSerializedRespawnInfo)
        {
            if (pendingSerializedRespawnInfo == null)
            {
                LogWarning($"null PendingSerializedRespawnInfo!");
                return null;
            }
            if (pendingSerializedRespawnInfo.m_SaveData == null)
            {
                LogWarning($"null PendingSerializedRespawnInfo.m_SaveData!");
                return null;
            }
            if (!PositionValidForSpawn(pendingSerializedRespawnInfo.m_SaveData.m_Position))
            {
                LogWarning($"invalid spawn location!");
                return null;
            }
            PlayerManager playerManager = GameManager.m_PlayerManager;
            if (playerManager == null)
            {
                LogError($"null PlayerManager");
                return null;
            }
            playerManager.GetTeleportTransformAfterSceneLoad(out Vector3 position, out Quaternion rotation);
            float distanceToPlayer = Vector3.Distance(position, pendingSerializedRespawnInfo.m_SaveData.m_Position);
            Il2Cpp.SpawnRegionManager spawnRegionManager = GameManager.m_SpawnRegionManager;
            if (spawnRegionManager == null)
            {
                LogError($"null Il2Cpp.SpawnRegionManager");
                return null;
            }
            float minSpawnDist = spawnRegionManager.m_ClosestSpawnDistanceToPlayerAfterSceneTransition;
            ExperienceModeManager experienceModeManager = GameManager.m_ExperienceModeManager;
            if (experienceModeManager == null)
            {
                LogError($"null ExperienceModEmanager");
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
                LogTrace($"Player is too close, aborting");
                return null;
            }
            return InstantiateSpawnFromSaveData(pendingSerializedRespawnInfo.m_SaveData, wildlifeMode);
        }


        public int CalculateTargetPopulation()
        {
            if (mSpawnRegion.m_SpawnLevel == 0)
            {
                LogTrace($"SpawnLevel is zero, aborting");
                return 0;
            }

            if (mSpawnRegion.m_AiTypeSpawned == 0 && !mSpawnRegion.m_ForcePredatorOverride)
            {
                if (GetCurrentTimelinePoint() < Il2Cpp.SpawnRegionManager.m_NoPredatorSpawningInVoyageurHours)
                {
                    LogTrace($"Voyager mode, no predator spawning, aborting");
                    return 0;
                }
            }
            if (!SpawnRegionCloseEnoughForSpawning())
            {
                LogTrace($"Too far for spawning, returning active population to prevent changes");
                return GetCurrentActivePopulation(mSpawnRegion.m_WildlifeMode);
            }
            if (!mSpawnRegion.m_CanSpawnInBlizzard && GameManager.m_Weather.IsBlizzard())
            {
                LogTrace($"Cannot spawn in blizzard");
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


        public bool CanDoReRoll()
        {
            if (mSpawnRegion.m_ControlledByRandomSpawner)
            {
                return false;
            }
            if (mSpawnRegion.m_DeferredSpawnsWithRandomPosition.Count > 0)
            {
                return false;
            }
            if (mSpawnRegion.m_DeferredSpawnsWithSavedPosition.Count > 0)
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
            if (string.IsNullOrEmpty(text))
            {
                LogError($"Null or empty text, cannot deserialize");
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
            mSpawnRegion.m_CooldownTimerHours = SpawnRegion.m_SpawnRegionDataProxy.m_CooldownTimerHours;
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
                //Is this section needed? maybe not...
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


        public int GetCurrentActivePopulation(WildlifeMode wildlifeMode)
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


        public float GetCustomSpawnRegionChanceActiveScale()
        {
            CustomExperienceMode customMode = GameManager.GetCustomMode();
            if (customMode.IsNullOrDestroyed())
            {
                LogError($"No custom mode found");
                return 1.0f;
            }
            CustomExperienceModeTunableLookupTable lookupTable = customMode.m_LookupTable;
            if (lookupTable.IsNullOrDestroyed())
            {
                LogError($"No custom mode lookup table found");
                return 1.0f;
            }
            Il2CppSystem.Collections.Generic.List<ExperienceMode> experienceModes = lookupTable.m_BaseExperienceModes;
            if (experienceModes.IsNullOrDestroyed())
            {
                LogError($"No base experience mode table found");
                return 1.0f;
            }
            if (experienceModes.Count < 4)
            {
                LogError($"Cannot fetch all base experience modes");
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
                LogTrace($"No den, no sleep duration");
                return 0.0f;
            }
            return GameManager.m_TimeOfDay.IsDay()
                ? UnityEngine.Random.Range(mSpawnRegion.m_Den.m_MinSleepHoursDay, mSpawnRegion.m_Den.m_MaxSleepHoursDay)
                : UnityEngine.Random.Range(mSpawnRegion.m_Den.m_MinSleepHoursNight, mSpawnRegion.m_Den.m_MaxSleepHoursNight);
        }


        public int GetMaxSimultaneousSpawnsDay()
        {
            if (mSpawnRegion.m_DifficultySettings == null)
            {
                LogTrace($"Null mSpawnRegion.m_DifficultySettings");
                return 0;
            }
            return mSpawnRegion.m_DifficultySettings[(int)mSpawnRegion.m_SpawnLevel].m_MaxSimultaneousSpawnsDay;
        }


        public int GetMaxSimultaneousSpawnsNight()
        {
            if (mSpawnRegion.m_DifficultySettings == null)
            {
                LogTrace($"Null mSpawnRegion.m_DifficultySettings");
                return 0;
            }
            return mSpawnRegion.m_DifficultySettings[(int)mSpawnRegion.m_SpawnLevel].m_MaxSimultaneousSpawnsNight;
        }


        public int GetNumActiveSpawns()
        {
            if (mSpawnRegion.gameObject.IsNullOrDestroyed())
            {
                LogError($"Null mSpawnRegion.gameObject");
                return 0;
            }    
            if (!mSpawnRegion.gameObject.activeSelf)
            {
                LogError($"Inactive mSpawnRegion.gameObject");
                return 0;
            }
            if (mSpawnRegion.m_Spawns.IsNullOrDestroyed())
            {
                LogError($"Null mSpawnRegion.m_Spawns");
                return 0;
            }
            int count = 0;
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
                count++;
            }
            return count;
        }



        public float GetNumHoursBetweenRespawns()
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


        //Not going to override this one unless we have problems, we arent introducing new prefabs yet (ever?)
        public static string GetPrefabNameFromInstanceName(string instanceName)
        {
            return string.Empty;
        }

        public string GetSpawnablePrefabName()
        {
            if (string.IsNullOrEmpty(mSpawnRegion.m_SpawnablePrefabName))
            {
                if (mSpawnRegion.m_SpawnablePrefab.IsNullOrDestroyed())
                {
                    LogTrace($"null spawnable prefab on spawn region, fetching...");
                    AssetReferenceAnimalPrefab animalReferencePrefab = mSpawnRegion.m_SpawnRegionAnimalTableSO.PickSpawnAnimal(WildlifeMode.Normal);
                    mSpawnRegion.m_SpawnablePrefab = animalReferencePrefab.GetOrLoadAsset();
                    animalReferencePrefab.ReleaseAsset();
                }
                if (mSpawnRegion.m_SpawnablePrefab.IsNullOrDestroyed())
                {
                    LogError($"Could not fetch spawnable prefab");
                    return string.Empty;
                }
            }
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




        public bool Spawn(WildlifeMode wildlifeMode)
        {
            if (mSpawnRegion.m_HasBeenDisabledByAurora)
            {
                LogTrace($"[{nameof(CustomBaseSpawnRegion)}.{nameof(Spawn)}] Spawn region disabled by aurora, skipping");
                return false;
            }
            // Some stuff here about NavMeshSurface, but I dont even see that class available via TLD. Seems they didnt want spawnign to occur unless navmesh was available?
            // Possible future bug fix for us I guess.
            // if (no navmesh) return false;
            if (mSpawnRegion.m_Spawns == null)
            {
                LogError($"Spawn region with null spawn list!");
                return false;
            }
            int skipCount = 0;
            foreach (BaseAi spawn in mSpawnRegion.m_Spawns)
            {
                if (spawn == null)
                {
                    LogTrace($"Null spawn in m_Spawns, skipping");
                    continue;
                }
                if (spawn.gameObject == null)
                {
                    LogTrace($"Null game object in m_Spawns, skipping");
                    continue;
                }
                if (spawn.gameObject.activeSelf)
                {
                    LogTrace($"Spawn is active in m_Spawns, skipping");
                    continue;
                }
                if (spawn.m_WildlifeMode != mSpawnRegion.m_WildlifeMode)
                {
                    LogTrace($"Spawn wildlife mode <<<{spawn.m_WildlifeMode}>>> does not match region wildlife mode <<<{mSpawnRegion.m_WildlifeMode}>>>, skipping");
                    continue;
                }

                Vector3 position = spawn.transform.position;

                if (SpawnPositionOnScreenTooClose(position) ||
                    SpawnPositionTooCloseToCamera(position))
                {
                    LogTrace($"Spawn position on screen or too close to camera, skipping");
                    skipCount++;
                    continue;
                }

                spawn.gameObject.SetActive(true);
                spawn.SetAiMode(spawn.m_DefaultMode); //Should patch through via harmony to CustomBaseAi version
                return true; 
            }
            if (skipCount != 0)
            {
                LogTrace($"Skipped at least one instantiated spawn without activating any, aborting");
                return false;
            }
            BaseAi newAi = mSpawnRegion.m_PendingSerializedRespawnInfoQueue.Count < 1 ? InstantiateAndPlaceSpawn(wildlifeMode) : MaybeSpawnPendingSerializedRespawn(wildlifeMode);
            return newAi != null;
        }


        protected bool SpawnPositionOnScreenTooClose(Vector3 position)
        {
            return false;
        }


        protected bool SpawnPositionTooCloseToCamera(Vector3 position)
        {
            return false;
        }


        public bool TryGetSpawnPositionAndRotation(ref Vector3 position, ref Quaternion rotation)
        {

            return true;
        }


        public bool PositionValidForSpawn(Vector3 spawnPosition)
        {
            return true;
        }





        public BaseAi InstantiateSpawnInternal(GameObject spawnablePrefab, WildlifeMode wildlifeMode, Vector3 spawnPos, Quaternion spawnRot)
        {
            if (mSpawnRegion.m_HasBeenDisabledByAurora)
            {
                LogTrace($"[{nameof(CustomBaseSpawnRegion)}.{nameof(InstantiateSpawnInternal)}] Disabled by aurora, aborting");
                return null;
            }
            if (mSpawnRegion.m_AuroraSpawnablePrefab != null && wildlifeMode == WildlifeMode.Aurora)
            {
                LogTrace($"[{nameof(CustomBaseSpawnRegion)}.{nameof(InstantiateSpawnInternal)}] Wildlife mode is aurora and aurora spawnable prefab available, overriding param prefab");
                spawnablePrefab = mSpawnRegion.m_AuroraSpawnablePrefab; 
            }
            if (!UnityEngine.AI.NavMesh.SamplePosition(new Vector3(spawnPos.x, spawnPos.y + 0.2f, spawnPos.z), out UnityEngine.AI.NavMeshHit hitLoc, float.MaxValue, 0x3f800000))
            {
                LogError($"[{nameof(CustomBaseSpawnRegion)}.{nameof(InstantiateSpawnInternal)}] Could not get valid navmesh result!");
                return null;
            }
            GameObject newInstance = GameObject.Instantiate(spawnablePrefab, spawnPos, spawnRot);
            if (!newInstance.TryGetComponent<BaseAi>(out BaseAi newBaseAi))
            {
                LogError($"[{nameof(CustomBaseSpawnRegion)}.{nameof(InstantiateSpawnInternal)}] Cannot extract BaseAi component from newly instantiated BaseAi spawnable prefab!");
                return null;
            }
            newInstance.name = spawnablePrefab.name + $"_{mSpawnRegion.m_AutoCloneIndex}";
            mSpawnRegion.m_AutoCloneIndex++;
            if (newInstance.TryGetComponent<PackAnimal>(out PackAnimal newPackAnimal))
            {
                newPackAnimal.gameObject.tag = mSpawnRegion.m_PackGroupId;
            }
            LogTrace($"[{nameof(CustomBaseSpawnRegion)}.{nameof(InstantiateSpawnInternal)}] Success!");
            return newBaseAi;
        }


        public BaseAi InstantiateSpawn(GameObject spawnablePrefab, AssetReferenceAnimalPrefab assetRef, Vector3 spawnPos, Quaternion spawnRot, AiMode aiMode, WildlifeMode wildlifeMode)
        {
            BaseAi baseAi = InstantiateSpawnInternal(spawnablePrefab, wildlifeMode, spawnPos, spawnRot);
            if (baseAi == null)
            {
                LogTrace($"[{nameof(CustomBaseSpawnRegion)}.{nameof(InstantiateSpawn)}] Null BaseAi received from InstantiateSpawnInternal, cascading");
                return null;
            }
            baseAi.transform.position = spawnPos;
            baseAi.transform.rotation = spawnRot;
            Transform transform = mSpawnRegion.transform;
            if (mSpawnRegion.m_WanderRegion != null)
            {
                LogTrace($"[{nameof(CustomBaseSpawnRegion)}.{nameof(InstantiateSpawn)}] Wander region found, setting move agent transform to wander region?");
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
            ObjectGuid.MaybeAttachObjectGuidAndRegister(baseAi.gameObject, PdidTable.GenerateNewID());
            mSpawnRegion.m_Spawns.Add(baseAi);
            mSpawnRegion.m_SpawnsPrefabReferences.Add(assetRef);
            LogTrace($"[{nameof(CustomBaseSpawnRegion)}.{nameof(InstantiateSpawn)}] success!");
            return baseAi;            
        }


        public BaseAi InstantiateAndPlaceSpawn(WildlifeMode wildlifeMode)
        {
            if (mSpawnRegion.m_AiSubTypeSpawned == AiSubType.Cougar && GetCurrentTimelinePoint() < mSpawnRegion.m_CooldownTimerHours)
            {
                LogTrace($"[{nameof(CustomBaseSpawnRegion)}.{nameof(InstantiateAndPlaceSpawn)}] Cougar timer has not expired, aborting");
                return null;
            }
            AssetReferenceAnimalPrefab animalReferencePrefab = null;
            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;
            if (mSpawnRegion.m_SpawnablePrefab == null)
            {
                LogTrace($"[{nameof(InstantiateAndPlaceSpawn)}.{nameof(InstantiateAndPlaceSpawn)}] Null spawnable prefab on spawn region, fetching...");
                animalReferencePrefab = mSpawnRegion.m_SpawnRegionAnimalTableSO.PickSpawnAnimal(wildlifeMode);
                mSpawnRegion.m_SpawnablePrefab = animalReferencePrefab.GetOrLoadAsset();
                animalReferencePrefab.ReleaseAsset();
            }
            if (!TryGetSpawnPositionAndRotation(ref spawnPosition, ref spawnRotation))
            {
                LogTrace($"[{nameof(CustomBaseSpawnRegion)}.{nameof(InstantiateAndPlaceSpawn)}] Potential error: Could not get spawn position and rotation. Aborting");
                return null;
            }
            if (!PositionValidForSpawn(spawnPosition))
            {
                LogTrace($"[{nameof(CustomBaseSpawnRegion)}.{nameof(InstantiateAndPlaceSpawn)}] Potential error: Invalid spawn placement. Aborting");
                return null;
            }
            AiMode aiMode = AiMode.FollowWaypoints;
            if (mSpawnRegion.m_PathManagers == null || mSpawnRegion.m_PathManagers.Count == 0)
            {
                LogTrace($"[{nameof(CustomBaseSpawnRegion)}.{nameof(InstantiateAndPlaceSpawn)}] Path manager null or zero count, setting mode to wander");
                aiMode = AiMode.Wander;
            }
            LogTrace($"[{nameof(CustomBaseSpawnRegion)}.{nameof(InstantiateAndPlaceSpawn)}] success!");
            return InstantiateSpawn(mSpawnRegion.m_SpawnablePrefab, animalReferencePrefab, spawnPosition, spawnRotation, aiMode, wildlifeMode);
        }


        public BaseAi MaybeSpawnPendingSerializedRespawn(WildlifeMode wildlifeMode)
        {
            if (mSpawnRegion.m_PendingSerializedRespawnInfoQueue.Count == 0)
            {
                LogTrace($"[{nameof(CustomBaseSpawnRegion)}.{nameof(MaybeSpawnPendingSerializedRespawn)}] No pending serialized respawn info, aborting");
                return null;
            }
            PendingSerializedRespawnInfo pendingSerializedRespawnInfo = mSpawnRegion.m_PendingSerializedRespawnInfoQueue.Dequeue();
            BaseAi respawnedAi = AttemptInstantiateAndPlaceSpawnFromSave(wildlifeMode, pendingSerializedRespawnInfo);
            if (respawnedAi != null)
            {
                LogTrace($"[{nameof(CustomBaseSpawnRegion)}.{nameof(MaybeSpawnPendingSerializedRespawn)}] Successfully respawned AI from pending serialized respawn info!");
                return respawnedAi;
            }
            pendingSerializedRespawnInfo.m_TrySpawnCount += 1;
            if (pendingSerializedRespawnInfo.m_TrySpawnCount < 0x3d)
            {
                LogTrace($"Failed to respawn Ai from pending serialized respawn info {pendingSerializedRespawnInfo.m_TrySpawnCount} times, re-queueing");
                mSpawnRegion.m_PendingSerializedRespawnInfoQueue.Enqueue(pendingSerializedRespawnInfo);
            }
            else
            {
                LogWarning($"Failed to respawn Ai from pending serialized respawn info {pendingSerializedRespawnInfo.m_TrySpawnCount} times, disposing");
            }
            return null;
        }

        public BaseAi InstantiateSpawnFromSaveData(SpawnDataProxy spawnData, WildlifeMode wildlifeMode)
        {
            if (spawnData.IsNullOrDestroyed())
            {
                LogError($"Null spawnData, aborting");
                return null;
            }
            if (!AiUtils.IsNavmeshPosValid(spawnData.m_Position, 0.5f, 1.0f))
            {
                LogWarning($"Invalid spawn position, aborting");
                return null;
            }
            AssetReferenceAnimalPrefab assetRef = null;
            GameObject spawnablePrefab = mSpawnRegion.m_SpawnablePrefab;
            if (spawnablePrefab.IsNullOrDestroyed())
            {
                LogTrace($"Null spawnable prefab on spawn region, fetching...");
                assetRef = mSpawnRegion.m_SpawnRegionAnimalTableSO.PickSpawnAnimal(wildlifeMode);
                mSpawnRegion.m_SpawnablePrefab = assetRef.GetOrLoadAsset();
                assetRef.ReleaseAsset();
                
            }
            BaseAi baseAi = InstantiateSpawnInternal(spawnablePrefab, wildlifeMode, spawnData.m_Position, spawnData.m_Rotation);
            if (baseAi.IsNullOrDestroyed())
            {
                LogWarning($"InstantiateSpawnInternal returned null BaseAi, aborting");
                return null;
            }
            if (baseAi.transform == null)
            {
                LogError($"BaseAi has null transform, aborting");
                return null;
            }
            baseAi.transform.position = spawnData.m_Position;
            baseAi.transform.rotation = spawnData.m_Rotation;
            Transform transform = mSpawnRegion.transform;
            if (mSpawnRegion.m_WanderRegion != null)
            {
                LogTrace($"Wander region found, setting move agent transform to wander region?");
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
                LogError($"Null AiDifficultySettings, aborting");
                return null;
            }
            AiDifficultySetting aiDifficultySetting = aiDifficultySettings.GetSetting(mSpawnRegion.m_AiDifficulty, baseAi.m_AiSubType);
            if (aiDifficultySetting.IsNullOrDestroyed())
            {
                LogError($"ull AiDifficultySetting, aborting");
                return null;
            }
            if (spawnData.m_Guid == null || spawnData.m_Guid.Length == 0)
            {
                LogTrace($"Generating new PDID");
                spawnData.m_Guid = PdidTable.GenerateNewID();
            }
            ObjectGuid.MaybeAttachObjectGuidAndRegister(baseAi.gameObject, spawnData.m_Guid);
            baseAi.Deserialize(spawnData.m_BaseAiSerialized);
            mSpawnRegion.m_Spawns.Add(baseAi);
            mSpawnRegion.m_SpawnsPrefabReferences.Add(assetRef);
            return baseAi;
        }

        #endregion
    }
}
