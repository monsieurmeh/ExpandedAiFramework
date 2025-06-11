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
using Il2CppTLD.Serialization;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static Il2Cpp.GridUI;
using static Il2Cpp.UITweener;
using static Il2CppNewtonsoft.Json.Converters.DiscriminatedUnionConverter;
using static Il2CppSystem.Linq.Expressions.Interpreter.CastInstruction.CastInstructionNoT;
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
                    //UpdateDeferredDeserialize();
                    return;
                }
                foreach (SpawnDataProxy spawnProxy in proxy.m_ActiveSpawns)
                {
                    mSpawnRegion.m_DeferredSpawnsWithRandomPosition.Add(spawnProxy);
                }
            }
            //UpdateDeferredDeserialize();
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
                LogError($"Null mSpawnRegion.m_DifficultySettings");
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
                LogError($"Null mSpawnRegion.m_PathManagers");
                return null;
            }
            if (mSpawnRegion.m_PathManagers.Length == 0)
            {
                LogError($"Empty mSpawnRegion.m_PathManagers");
                return null;
            }
            if (mSpawnRegion.m_CurrentWaypointPathIndex >= mSpawnRegion.m_PathManagers.Length)
            {
                LogError($"mSpawnRegion.m_CurrentWaypointIndex >= mSpawnRegion.m_PathManagers.Length");
                return null;
            }
            if (mSpawnRegion.m_PathManagers[mSpawnRegion.m_CurrentWaypointPathIndex].IsNullOrDestroyed())
            {
                LogError($"Path manager for current waypoint index is null");
                return null;
            }
            foreach (WanderRegion wanderRegion in mSpawnRegion.m_PathManagers[mSpawnRegion.m_CurrentWaypointPathIndex].gameObject.GetComponentsInChildren<WanderRegion>())
            {
                if (wanderRegion.IsNullOrDestroyed())
                {
                    LogError($"Null wander region found in pathmanager");
                    return null;
                }
            }
            return mSpawnRegion.m_PathManagers[mSpawnRegion.m_CurrentWaypointPathIndex].GetPathPoints();
        }


        public bool HasDeferredDeserializeCompleted()
        {
            if (mSpawnRegion.m_DeferredSpawnsWithRandomPosition == null)
            {
                LogError($"Null mSpawnRegion.m_DeferredSpawnsWithRandomPosition");
                return false;
            }
            if (mSpawnRegion.m_DeferredSpawnsWithRandomPosition.Count != 0)
            {
                return false;
            }
            if (mSpawnRegion.m_DeferredSpawnsWithSavedPosition == null)
            {
                LogError($"Null mSpawnRegion.m_DeferredSpawnsWithSavedPosition");
                return false;
            }
            if (mSpawnRegion.m_DeferredSpawnsWithSavedPosition.Count != 0)
            {
                return false;
            }
            return true;
        }


        public bool HasSameWildlifeMode(BaseAi baseAi, WildlifeMode wildlifeMode)
        {
            if (baseAi.IsNullOrDestroyed())
            {
                LogError($"Null baseAi");
                return false;
            }
            return baseAi.m_WildlifeMode == mSpawnRegion.m_WildlifeMode;
        }


        public bool HasSerializedRespawnPending()
        {
            if (mSpawnRegion.m_PendingSerializedRespawnInfoQueue.IsNullOrDestroyed())
            {
                LogError($"Null mSpawnRegion.m_PendingSerializedRespawnInfoQueue");
                return false;
            }
            return mSpawnRegion.m_PendingSerializedRespawnInfoQueue.Count != 0;
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
            if (false)//!TryGetSpawnPositionAndRotation(ref spawnPosition, ref spawnRotation))
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


        public bool IsPredator() => mSpawnRegion.m_AiTypeSpawned == AiType.Predator;


        public void MaybeReduceNumTrapped()
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


        public void MaybeRegisterWithManager()
        {
            if (mSpawnRegion.m_Registered)
            {
                return;
            }
            if (GameManager.m_SpawnRegionManager == null)
            {
                LogError($"Null SpawnRegionManager");
                return;
            }
            GameManager.m_SpawnRegionManager.Add(mSpawnRegion);
            mSpawnRegion.m_Registered = true;
        }


        public void MaybeReRollActive()
        {
            if (mSpawnRegion.m_HasBeenDisabledByAurora)
            {
                LogTrace($"Disabled by aurora, aborting");
                return;
            }
            if (mSpawnRegion.m_AiSubTypeSpawned == AiSubType.Cougar)
            {
                LogTrace($"Cougar override");
                return;
            }
            if (mSpawnRegion.m_WasForceDisabled)
            {
                LogTrace($"Force disabled");
                return;
            }
            if (mSpawnRegion.m_HoursReRollActive <= 0.0001f)
            {
                LogError($"Effectively zero mSpawnRegion.m_HoursReRollActive, aborting to prevent div by near zero");
                return;
            }
            if (GetCurrentTimelinePoint() - mSpawnRegion.m_ElapsedHoursAtLastActiveReRoll < mSpawnRegion.m_HoursReRollActive)
            {
                LogTrace($"Not yet time");
                return;
            }
            if (mSpawnRegion.m_ControlledByRandomSpawner)
            {
                LogTrace($"Controlled by random spawner");
                return;
            }
            if (mSpawnRegion.m_DeferredSpawnsWithRandomPosition.IsNullOrDestroyed())
            {
                LogError($"Null mSpawnRegion.m_DeferredSpawnsWithRandomPosition, aborting");
                return;
            }
            if (mSpawnRegion.m_DeferredSpawnsWithRandomPosition.Count == 0)
            {
                LogTrace($"mSpawnRegion.m_DeferredSpawnsWithRandomPosition.Count != 0, cannot reroll");
                return;
            }
            if (mSpawnRegion.m_DeferredSpawnsWithSavedPosition.IsNullOrDestroyed())
            {
                LogError($"Null mSpawnRegion.m_DeferredSpawnsWithSavedPosition, aborting");
                return;
            }
            if (mSpawnRegion.m_DeferredSpawnsWithSavedPosition.Count == 0)
            {
                LogTrace($"mSpawnRegion.m_DeferredSpawnsWithSavedPosition.Count != 0, cannot reroll");
                return;
            }
            if (GetNumActiveSpawns() != 0)
            {
                LogTrace($"Active spawns, cannot reroll");
                return;
            }
            mSpawnRegion.gameObject.SetActive(Utils.RollChance(GameManager.m_ExperienceModeManager.GetSpawnRegionChanceActiveScale()));
            mSpawnRegion.m_ElapsedHoursAtLastActiveReRoll = GetCurrentTimelinePoint();
        }


        public void MaybeResumeAfterAurora()
        {
            if (mSpawnRegion.m_WasActiveBeforeAurora)
            {
                mSpawnRegion.gameObject.SetActive(true);
            }
            mSpawnRegion.m_HasBeenDisabledByAurora = false;
            return;
        }


        public BaseAi MaybeSpawnPendingSerializedRespawn(WildlifeMode wildlifeMode)
        {
            if (mSpawnRegion.m_PendingSerializedRespawnInfoQueue.Count == 0)
            {
                LogTrace($"No pending serialized respawn info, aborting");
                return null;
            }
            PendingSerializedRespawnInfo pendingSerializedRespawnInfo = mSpawnRegion.m_PendingSerializedRespawnInfoQueue.Dequeue();
            BaseAi respawnedAi = AttemptInstantiateAndPlaceSpawnFromSave(wildlifeMode, pendingSerializedRespawnInfo);
            if (respawnedAi != null)
            {
                LogTrace($"Successfully respawned AI from pending serialized respawn info!");
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


        public void MaybeSuspendForAurora()
        {
            if (mSpawnRegion.m_AiSubTypeSpawned == AiSubType.Cougar)
            {
                LogTrace($"Cougar override");
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
                LogTrace($"WildlifeMode match, ignoring");
                return;
            }

            if (enabled)
            {
                if (mSpawnRegion.m_AiTypeSpawned != AiType.Predator)
                {
                    LogTrace($"Aurora enabled, maybe suspending ambient spawn region");
                    MaybeSuspendForAurora();
                }
            }
            else
            {
                if (mSpawnRegion.m_WasActiveBeforeAurora)
                {
                    LogTrace($"Aurora enabled, activating previously active spawn region");
                    mSpawnRegion.gameObject.SetActive(true);
                }
                mSpawnRegion.m_HasBeenDisabledByAurora = false;
            }
            RemoveActiveSpawns(GetCurrentActivePopulation(mSpawnRegion.m_WildlifeMode), mSpawnRegion.m_WildlifeMode, true);
            mSpawnRegion.m_WildlifeMode = enabled ? WildlifeMode.Aurora : WildlifeMode.Normal;
        }


        public void OnDestroy()
        {
            GameManager.m_SpawnRegionManager.m_SpawnRegions.Remove(mSpawnRegion);
        }


        public bool PositionValidForSpawn(Vector3 spawnPosition)
        {
            if (!GameManager.m_SpawnRegionManager.PointInsideNoSpawnRegion(spawnPosition))
            {
                LogTrace($"Encountered NoSpawn region");
                return false;
            }
            if (GameManager.m_Weather.IsIndoorEnvironment())
            {
                LogTrace($"Encountered indoor environment... why does this automatically return true???");
                return true;
            }
            if (SpawnPositionOnScreenTooClose(spawnPosition))
            {
                LogTrace($"Spawn position on screen too close");
                return false;
            }
            if (SpawnPositionTooCloseToCamera(spawnPosition))
            {
                LogTrace($"Spawn position too close to camera");
                return false;
            }
            return true;
        }


        public void QueueSerializedRespawnPending(SpawnDataProxy saveData)
        {
            PendingSerializedRespawnInfo respawnInfo = new PendingSerializedRespawnInfo();
            respawnInfo.m_SaveData = saveData;
            respawnInfo.m_TrySpawnCount = 0;
            mSpawnRegion.m_PendingSerializedRespawnInfoQueue.Enqueue(respawnInfo);
        }


        public void RemoveActiveSpawns(int numToDeactivate, WildlifeMode wildlifeMode, bool isAdjustingOtherWildlifeMode)
        {
            bool playerGhost = GameManager.m_PlayerManager.m_Ghost;
            for (int i = 0, iMax = mSpawnRegion.m_Spawns.Count; i < iMax || numToDeactivate == 0; i++)
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
                    LogTrace($"Adjusting other wildlife mode and spawn mode matches called mode, wildlifeMode matched for despawn");
                    canDespawn = true;
                }
                if (!canDespawn && !HasSameWildlifeMode(spawn, wildlifeMode))
                {
                    LogTrace($"NOT adjusting other wildlife mode and spawn mode does NOT match called mode, wildlifeMode matched for despawn");
                    canDespawn = true;
                }
                if (canDespawn
                    && spawn.GetAiMode() != AiMode.Flee
                    && spawn.GetAiMode() != AiMode.Dead)
                {
                    LogTrace($"Can despawn && and spawn is not fleeing or dead, setting flee");
                    spawn.SetAiMode(AiMode.Flee);
                }
                Vector3 spawnPos = spawn.m_CachedTransform.position;
                bool canDespawnDueToProximity = playerGhost;
                if (canDespawnDueToProximity)
                {
                    LogTrace($"Ghost, proximity check passed");
                }
                if (!canDespawnDueToProximity
                    && Utils.DistanceToMainCamera(spawnPos) >= GameManager.GetSpawnRegionManager().m_DisallowDespawnBelowDistance
                    && (!Utils.PositionIsOnscreen(spawnPos) || Utils.DistanceToMainCamera(spawnPos) >= GameManager.GetSpawnRegionManager().m_AllowDespawnOnscreenDistance)
                    && !Utils.PositionIsInLOSOfPlayer(spawnPos)) //Why is this last one needed...?
                {
                    LogTrace($"Ai is not visible, proximity check passed");
                    canDespawnDueToProximity = true;
                }
                if (!canDespawnDueToProximity)
                {
                    LogTrace($"Proximity check failed, cannot despawn");
                    continue;
                }
                if (!canDespawn)
                {
                    if (!spawn.m_CurrentTarget.IsNullOrDestroyed() && spawn.m_CurrentTarget.IsPlayer())
                    {
                        LogTrace($"Failed to match wildlifeMode for forced removal and spawn is targetting player, cannot despawn");
                        continue;
                    }
                    if (spawn.m_CurrentMode == AiMode.Feeding)
                    {
                        LogTrace($"Failed to match wildlifeMode for forced removal and spawn is eating, cannot despawn");
                        continue;
                    }
                    if (spawn.m_CurrentMode == AiMode.Sleep)
                    {
                        LogTrace($"Failed to match wildlifeMode for forced removal and spawn is sleeping, cannot despawn");
                        continue;
                    }
                    if (spawn.IsBleedingOut())
                    {
                        LogTrace($"Failed to match wildlifeMode for forced removal and spawn is bleeding, cannot despawn");
                        continue;
                    }
                    if (!spawn.NormalWolf.IsNullOrDestroyed() && spawn.m_CurrentMode == AiMode.WanderPaused)
                    {
                        LogTrace($"Failed to match wildlifeMode for forced removal and spawn is normal wolf in AiMode.WanderPaused, cannot despawn");
                        continue;
                    }
                    spawn.Despawn();
                    numToDeactivate--;
                }
            }
        }
        

        public void Respawn(WildlifeMode wildlifeMode)
        {
            if (!Spawn(wildlifeMode))
            {
                LogTrace($"Spawn failed, aborting");
                return;
            }
            mSpawnRegion.m_NumRespawnsPending--;
            mSpawnRegion.m_ElapasedHoursNextRespawnAllowed = GetCurrentTimelinePoint() + GetNumHoursBetweenRespawns();
            LogTrace($"Respawn successful; pending respawns: !_{mSpawnRegion.m_NumRespawnsPending}_! next respawn time: !_{mSpawnRegion.m_ElapasedHoursNextRespawnAllowed}_! hours");
        }


        public bool RespawnAllowed()
        {
            if (mSpawnRegion.m_NumRespawnsPending < 1)
            {
                LogTrace($"No pending respawns, no respawn allowed");
                return false;
            }
            if (mSpawnRegion.m_ElapasedHoursNextRespawnAllowed >= GetCurrentTimelinePoint())
            {
                LogTrace($"Not yet time, no respawn allowed");
                return false;
            }
            LogTrace($"Respawn allowed");
            return true;
        }


        //Note to self: system re-uses a static SpawnRegionModDataProxy for serialization rather than creating one per region.
        //Since it's just a transfer container, makes some sense for garbage collector optimization
        public string Serialize()
        {
            SpawnRegionDataProxy regionProxy = SpawnRegion.m_SpawnRegionDataProxy;
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
            regionProxy.m_CurrentWaypointPathIndex =  mSpawnRegion.m_CurrentWaypointPathIndex; 
            regionProxy.m_WildlifeMode =  mSpawnRegion.m_WildlifeMode;
            regionProxy.m_HasBeenDisabledByAurora = mSpawnRegion. m_HasBeenDisabledByAurora;
            regionProxy.m_WasActiveBeforeAurora = mSpawnRegion.m_WasActiveBeforeAurora;   
            regionProxy.m_CooldownTimerHours = mSpawnRegion. m_CooldownTimerHours;
            for (int i = 0, iMax = mSpawnRegion.m_Spawns.Count; i < iMax; i++)
            {
                if (mSpawnRegion.m_Spawns[i].IsNullOrDestroyed())
                {
                    LogWarning($"Null BaseAI, skipping");
                    continue;
                }
                if (!mSpawnRegion.m_Spawns[i].gameObject.activeSelf)
                {
                    LogTrace($"Inactive BaseAi, skipping");
                    continue;
                }
                SpawnDataProxy spawnProxy = i < 0x80 ? SpawnRegion.m_SpawnDataProxyPool[i] : new SpawnDataProxy();
                spawnProxy.m_Position = mSpawnRegion.m_Spawns[i].transform.position;
                spawnProxy.m_Rotation = mSpawnRegion.m_Spawns[i].transform.rotation;
                spawnProxy.m_Guid = ObjectGuid.GetGuidFromGameObject(mSpawnRegion.m_Spawns[i].gameObject);

                spawnProxy.m_AssetReferenceGUID = mSpawnRegion.m_SpawnsPrefabReferences[i].AssetGUID;
                spawnProxy.m_BaseAiSerialized = mSpawnRegion.m_Spawns[i].Serialize();
                regionProxy.m_ActiveSpawns.Add(spawnProxy);
            }
            return SerializationUtils.SerializeObject(regionProxy);
        }


        public void SetActive(bool active)
        {
            if (mSpawnRegion.m_AiSubTypeSpawned == AiSubType.Cougar)
            {
                LogTrace($"Cougar Override");
                return;
            }
            if (SpawnRegion.m_WasForceDisabled && active)
            {
                LogTrace($"WasForceDisabled, cannot activate");
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
                        LogWarning($"Null spawn");
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


        public void SetBoundingSphereBasedOnWaypoints(int waypointIndex)
        {
            if (waypointIndex < 0 || waypointIndex >= mSpawnRegion.m_PathManagers.Length)
            {
                LogError($"Invalid waypoint index !_{waypointIndex}_! (waypoints available: !_{mSpawnRegion.m_PathManagers.Length}_!)");
                return;
            }
            Vector3[] pathPoints = mSpawnRegion.m_PathManagers[mSpawnRegion.m_CurrentWaypointPathIndex].GetPathPoints();
            BoundingSphereFromPoints.Calculate(pathPoints, pathPoints.Length, 0, 0);
            mSpawnRegion.m_Radius = BoundingSphereFromPoints.m_Radius;
            mSpawnRegion.m_Center = BoundingSphereFromPoints.m_Center;
            LogTrace($"Set bounding sphere to center !_{mSpawnRegion.m_Center}_! and radius !_{mSpawnRegion.m_Radius}_!");
        }


        public void SetRandomWaypointCircuit()
        {
            if (mSpawnRegion.m_PathManagers.Count <= 0)
            {
                LogTrace($"No pathmanagers, cannot set waypoint circuit");
                return;
            }
            mSpawnRegion.m_CurrentWaypointPathIndex = UnityEngine.Random.Range(0, mSpawnRegion.m_PathManagers.Count);
            LogTrace($"mSpawnRegion.m_CurrentWaypointIndex set to !_{mSpawnRegion.m_CurrentWaypointPathIndex}_!");
        }


        public void SetRespawnCooldownTimer(float cooldownHours)
        {
            mSpawnRegion.m_CooldownTimerHours = GetCurrentTimelinePoint() + cooldownHours;
            LogTrace($"Setting cooldown time to !_{mSpawnRegion.m_CooldownTimerHours}_!");
        }


        private bool ShouldSleepInDenAfterWaypointLoop()
        {
            if (mSpawnRegion.m_Den.IsNullOrDestroyed())
            {
                LogTrace($"Null den, no sleep");
                return false;
            }
            float sleepChance = GameManager.m_TimeOfDay.IsDay()
                ? mSpawnRegion.m_Den.m_ChanceSleepAfterWaypointsLoopDay
                : mSpawnRegion.m_Den.m_ChanceSleepAfterWaypointsLoopNight;
            bool shouldSleep = Utils.RollChance(sleepChance);
            LogTrace($"Rolling against chance to sleep of !_{sleepChance}_!: !_{shouldSleep}_!");
            return shouldSleep;
        }


        public bool Spawn(WildlifeMode wildlifeMode)
        {
            if (mSpawnRegion.m_HasBeenDisabledByAurora)
            {
                LogTrace($"[{nameof(CustomBaseSpawnRegion)}.{nameof(Spawn)}] Spawn region disabled by aurora, skipping");
                return false;
            }
            // Some stuff here about NavMeshSurface, but I dont even see that class available via TLD. Seems they didnt want spawning to occur unless navmesh was available?
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


        public bool SpawningSuppressedByExperienceMode()
        { 
            if (mSpawnRegion.m_SpawnLevel == 0)
            {
                LogTrace($"Suppressed by zero spawn level");
                return true;
            }
            if (!ExperienceModeManager.s_CurrentGameMode.m_XPMode.m_NoPredatorsFirstDay)
            {
                LogTrace($"No predator grace period, not suppressed");
                return false;
            }
            if (mSpawnRegion.m_AiTypeSpawned != AiType.Predator)
            {
                LogTrace($"Non-predator spawn region, not suppressed");
                return false;
            }
            if (mSpawnRegion.m_ForcePredatorOverride)
            {
                LogTrace($"Forced predator override, not suppressed");
                return false;
            }
            if (GetCurrentTimelinePoint() >= Il2Cpp.SpawnRegionManager.m_NoPredatorSpawningInVoyageurHours)
            {
                LogTrace($"Past grace period, not suppressed");
                return false;
            }
            LogTrace($"Predator spawn region suppressed during predator grace period");
            return true;
        }


        private bool SpawnPositionOnScreenTooClose(Vector3 spawnPos)
        {
            PlayerManager playerManager = GameManager.m_PlayerManager;
            if (playerManager.m_Ghost)
            {
                LogTrace($"Ghost, not too close");
                return false;
            }
            if (Utils.PositionIsOnscreen(spawnPos) 
                && Utils.DistanceToMainCamera(spawnPos) < GameManager.m_SpawnRegionManager.m_AllowSpawnOnscreenDistance
                && !mSpawnRegion.m_OverrideDistanceToCamera)
            {
                LogTrace($"Position is on screen and dist to main camera within allowSpawnOnScreenDistance and spawnRegion.m_OverrideDistanceToCamera is false, too close for spawn");
                return true;
            }
            if (Utils.PositionIsInLOSOfPlayer(spawnPos) && !mSpawnRegion.m_OverrideCameraLineOfSight)
            {
                LogTrace($"Position in LOS of player and no camera LOS override, too close to spawn");
                return true;
            }
            LogTrace($"ScreenPos not too close to spawn!");
            return false;
        }


        public bool SpawnPositionTooCloseToCamera(Vector3 spawnPos)
        {
            PlayerManager playerManager = GameManager.m_PlayerManager;
            if (playerManager.m_Ghost)
            {
                LogTrace($"Ghost, not too close");
                return false;
            }

            float closestDistToPlayer = GameManager.m_SpawnRegionManager.m_ClosestSpawnDistanceToPlayer;
            if (GameManager.m_Weather.IsIndoorEnvironment())
            {
                closestDistToPlayer *= 0.5f;
            }
            if (Vector3.Distance(GameManager.m_vpFPSCamera.transform.position, spawnPos) <= closestDistToPlayer)
            {
                LogTrace($"Too close to spawn");
                return true;
            }
            LogTrace($"NOT Too close to spawn");
            return false;
        }


        public bool SpawnRegionCloseEnoughForSpawning()
        {
            return mSpawnRegion.m_Radius + GameManager.m_SpawnRegionManager.m_SpawnRegionDisableDistance >= Utils.DistanceToMainCamera(mSpawnRegion.m_Center);
        }


        
        public void SpawnWithRandomPositions(Il2CppSystem.Collections.Generic.List<SpawnDataProxy> spawns, WildlifeMode wildlifeMode)
        {
            if (mSpawnRegion.m_DifficultySettings == null)
            {
                LogError($"Null DifficultySettings");
                return;
            }
            if (mSpawnRegion.m_DifficultySettings.Length <= (int)mSpawnRegion.m_SpawnLevel)
            {
                LogError($"Not enough entries in DifficultySettings for spawn level");
                return;
            }
            SpawnRegion.DifficultyProperties difficultyProperty = mSpawnRegion.m_DifficultySettings[(int)mSpawnRegion.m_SpawnLevel];
            if (difficultyProperty.IsNullOrDestroyed())
            {
                LogError($"Null DifficultyProperty");
                return;
            }
            int spawnCount = 0;
            foreach (SpawnDataProxy spawn in spawns)
            {
                if (spawn.IsNullOrDestroyed())
                {
                    LogWarning($"Null SpawnDataProxy");
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
                    LogTrace($"Maximum spawns reached");
                    return;
                }
            }
        }



        public void SpawnWithSavedPositions(Il2CppSystem.Collections.Generic.List<SpawnDataProxy> spawns, WildlifeMode wildlifeMode)
        {
            foreach (SpawnDataProxy spawn in spawns)
            {
                if (spawn.IsNullOrDestroyed())
                {
                    LogWarning($"Null SpawnDataProxy");
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
                LogTrace($"null spawnable prefab on spawn region, fetching...");
                AssetReferenceAnimalPrefab animalReferencePrefab = mSpawnRegion.m_SpawnRegionAnimalTableSO.PickSpawnAnimal(WildlifeMode.Normal);
                mSpawnRegion.m_SpawnablePrefab = animalReferencePrefab.GetOrLoadAsset();
                animalReferencePrefab.ReleaseAsset();
            }
            if (mSpawnRegion.m_SpawnablePrefab.IsNullOrDestroyed())
            {
                LogError($"Could not fetch spawnable prefab!");
                return;
            }

            if (!mSpawnRegion.m_SpawnablePrefab.TryGetComponent<BaseAi>(out BaseAi baseAi))
            {
                LogError($"Could not fetch baseai script from spawnable prefab");
                return;
            }
            if (GetSpawnablePrefabName() == string.Empty)
            {
                LogError($"Could not set spawnable prefab name!");
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
            //WIP: continue tomorrow
        }


        public static void VanillaLog(string output) => LogTrace(output, "FromVanilla");
        public static void VanillaLogWarning(string otuput) => LogWarning(otuput, true, "FromVanilla");

        #endregion
    }
}
