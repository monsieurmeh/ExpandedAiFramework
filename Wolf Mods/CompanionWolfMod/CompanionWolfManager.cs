using UnityEngine;
using Il2CppInterop.Runtime;
using MelonLoader.TinyJSON;
using Il2CppTLD.AddressableAssets;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.AsyncOperations;
using static Il2Cpp.Panel_Debug;
using HarmonyLib;
using Il2Cpp;
using static Il2Cpp.UITweener;
using Il2CppTLD.PDID;
using Il2CppNewtonsoft.Json.Utilities;
using Il2CppNodeCanvas.Tasks.Actions;


namespace ExpandedAiFramework.CompanionWolfMod
{
    public class CompanionWolfManager : ISubManager
    {
        public const string WolfPrefabString = "WILDLIFE_Wolf";

        protected EAFManager mManager;
        protected GameObject mWolfPrefab;
        protected Transform mTamedSpawnTransform;
        protected CompanionWolf mInstance;
        protected CompanionWolfData mData;
        protected bool mInitialized = false;
        protected bool mShouldCheckForSpawnTamedCompanion = false;
        protected float mLastTriggeredCheckForSpawnTamedCompanionTime = 0.0f;

        public CompanionWolfData Data { get { return mData; } set { mData = value; } }
        public CompanionWolf Instance { get { return mInstance; } set { mInstance = value; } }
        public Type SpawnType { get { return typeof(CompanionWolf); } }
        public GameObject WolfPrefab { get { return mWolfPrefab; } set { mWolfPrefab = value; } }



        public void Initialize(EAFManager manager)
        {
            mManager = manager;
            mInitialized = true;
            Utility.LogDebug("CompanionWolfManager initialized!");
        }


        public bool ShouldInterceptSpawn(BaseAi baseAi, SpawnRegion region)
        {
            SpawnCompanion();
            if (mData == null)
            {
                Utility.LogDebug($"No data setup, will not intercept spawn. How the fuck did we get here before data loading anyways?");
                return false;
            }
            if (!mData.Connected)
            {
                Utility.LogDebug($"No connected instance, will not intercept spawn");
                return false;
            }
            if (mInstance != null)
            {
                Utility.LogDebug($"Active instance, will not intercept spawn");
                return false;
            }
            if (baseAi == null)
            {
                Utility.LogDebug($"Null baseAi, will not intercept spawn");
                return false;
            }
            if (region == null)
            {
                Utility.LogDebug($"Null SpawnRegion, will not intercept spawn");
                return false;
            }
            if (mData.SpawnRegionModDataProxy == null)
            {
                Utility.LogDebug($"Null proxy, will not intercept spawn");
                return false;
            }
            if (mData.SpawnRegionModDataProxy.Scene != GameManager.m_ActiveScene
                || Vector3.Distance(mData.SpawnRegionModDataProxy.Position, region.transform.position) > 0.001f
                || mData.SpawnRegionModDataProxy.AiType != baseAi.m_AiType
                || mData.SpawnRegionModDataProxy.AiSubType != baseAi.m_AiSubType)
            {
                Utility.LogDebug($"Proxy mismatch, will not intercept spawn");
                return false;
            }

            Utility.LogDebug($"Proxy match to connected CompanionWolf data found, overriding WeightedTypePicker and spawning companionwolf where it first spawned {Utility.GetCurrentTimelinePoint() - Data.SpawnDate} hours ago!");
            return true;
        }


        public void Shutdown()
        {
            mData = null;
        }


        public void OnStartNewGame()
        {
            mData = new CompanionWolfData();
            OnSaveGame();
        }


        public void OnLoadGame()
        {
            if (mData == null)
            {
                mData = new CompanionWolfData();
            }

            string json = mManager.ModData.Load("CompanionWolfMod");
            if (json != null)
            {
                Variant variant = JSON.Load(json);
                if (variant != null)
                {
                    Utility.LogDebug($"Successfully loaded previously saved CompanionWolfData!");
                    JSON.Populate(variant, mData);
                }
            }

            Utility.LogDebug($"Tamed: {mData.Tamed} | Calories: {mData.CurrentCalories} | Affection: {mData.CurrentAffection} | Outdoors: {GameManager.m_ActiveSceneSet.m_IsOutdoors}");
        }


        public void OnLoadScene() 
        { 
            if (mInstance != null) //this is too late, by now system has serialized the wolf for later. gotta stop it there
            {
                GameObject.Destroy(mInstance.gameObject.transform.parent.gameObject); //destroy the whole thing
            }
        }


        public void OnInitializedScene()
        {
            if (GameManager.m_ActiveSceneSet != null && GameManager.m_ActiveSceneSet.m_IsOutdoors && mData != null && mData.Tamed && mInstance == null)
            {
                mShouldCheckForSpawnTamedCompanion = true;
                mLastTriggeredCheckForSpawnTamedCompanionTime = Time.time;
            }
        }


        public void OnSaveGame()
        {
            string json = JSON.Dump(mData);
            if (json != null)
            {
                mManager.ModData.Save(json, "CompanionWolfMod");
            }
        }


        public void Update()
        {
            if (mShouldCheckForSpawnTamedCompanion && Time.time - mLastTriggeredCheckForSpawnTamedCompanionTime > 2.0f)
            {
                mShouldCheckForSpawnTamedCompanion = false;
                SpawnCompanion();
            }
            if (mInstance != null)
            {
                mInstance.UpdateStatusText();
            }
        }


        public void SpawnCompanion()
        {
            if (Data == null)
            {
                Utility.LogDebug("No data found, cannot spawn companion!");
                return;
            }
            if (!Data.Tamed)
            {
                Utility.LogDebug("Companion is not tamed, go find and tame one!");
                return;
            }
            if (mInstance != null)
            {
                Utility.LogDebug("Companion is already here!");
                return;
            }
            GameObject wolfContainer = new GameObject("CompanionWolfContainer");
            Vector3 playerPos = GameManager.m_PlayerManager.m_LastPlayerPosition;
            AiUtils.GetClosestNavmeshPos(out Vector3 validPos, playerPos, playerPos);
            GameObject newWolf = AssetHelper.SafeInstantiateAssetAsync(WolfPrefabString).WaitForCompletion();
            newWolf.transform.position = validPos;
            //GameObject newWolf = GameObject.Instantiate(newWolfAsset, validPos, Quaternion.identity);
            Utility.LogDebug("Successfully instantiated: " + newWolf.name);
            if (newWolf == null)
            {
                Utility.LogWarning("Couldn't instantiate new wolf prefab!");
                return;
            }
            newWolf.transform.position = validPos;
            BaseAi baseAi = newWolf.GetComponentInChildren<BaseAi>();
            if (baseAi == null)
            {
                Utility.LogError("Coult not find BaseAi script attached to wolf prefab!");
                return;
            }
            Utility.LogDebug($"Creating move agent...");
            baseAi.CreateMoveAgent(wolfContainer.transform);
            Utility.LogDebug($"Reparenting...");
            baseAi.ReparentBaseAi(wolfContainer.transform);
            //ObjectGuid.MaybeAttachObjectGuidAndRegister(newWolf, PdidTable.GenerateNewID());
            Utility.LogDebug($"Wrapping...");
            if (!mManager.TryInjectCustomAi(baseAi, Il2CppType.From(typeof(CompanionWolf)), null))
            {
                return;
            }
            Utility.LogDebug($"re-grabbing wrapper..");
            if (!mManager.CustomAis.TryGetValue(baseAi.GetHashCode(), out ICustomAi wrapper))
            {
                Utility.LogError("Did not find new wrapper for new base ai!");
                return;
            }
            Utility.LogDebug($"Grabbing Instance..");
            mInstance = wrapper as CompanionWolf;
            if (mInstance == null)
            {
                Utility.LogError("Instantiated companion wolf but script is not correct!");
                return;
            }
            wrapper.BaseAi.m_MoveAgent.transform.position = validPos;
            wrapper.BaseAi.m_MoveAgent.Warp(validPos, 5.0f, true, -1);
            mShouldCheckForSpawnTamedCompanion = false;
            BaseAiManager.Remove(wrapper.BaseAi);
            Utility.LogDebug($"Companion wolf loaded!");
        }
    }         
}


