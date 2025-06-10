using Harmony;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.Runtime;
using Il2CppNodeCanvas.Tasks.Actions;
using Il2CppRewired.Utils;
using Il2CppTLD.AI;
using Il2CppTLD.Gameplay;
using Il2CppTLD.PDID;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static Il2Cpp.UITweener;


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

        bool Spawn(WildlifeMode wildlifeMode)
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
