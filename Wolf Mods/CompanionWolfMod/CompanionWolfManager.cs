
using UnityEngine;
using System.Text;
using MelonLoader.Utils;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Attributes;
using ComplexLogger;
using UnityEngine.AI;
using ModData;
using MelonLoader.TinyJSON;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ExpandedAiFramework.CompanionWolfMod
{
    public class CompanionWolfManager : ISubManager
    {
        private const string WolfPrefabString = "WILDLIFE_Wolf";

        protected GameObject mPrefab;
        protected EAFManager mManager;
        protected CompanionWolf mInstance;
        protected CompanionWolfData mData;
        protected bool mInitialized = false;

        public CompanionWolfData Data { get { return mData; } set { mData = value; } }
        public CompanionWolf Instance { get { return mInstance; } set { mInstance = value; } }
        public Type SpawnType { get { return typeof(CompanionWolf); } }



        public void Initialize(EAFManager manager)
        {
            mManager = manager;
            mInitialized = true;
            Utility.LogDebug("CompanionWolfManager initialized!");
        }


        public bool ShouldInterceptSpawn(BaseAi baseAi, SpawnRegion region)
        {
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
            OnSave();
        }


        public void OnLoad()
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
                    return;
                }
            }
        }


        public void OnSave()
        {
            string json = JSON.Dump(mData);
            if (json != null)
            {
                mManager.ModData.Save(json, "CompanionWolfMod");
            }
        }


        public void Update()
        {
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
            AsyncOperationHandle<GameObject> companionObjectTask = UnityEngine.AddressableAssets.Addressables.InstantiateAsync(WolfPrefabString);
            Action<AsyncOperationHandle<GameObject>> onCompleted = new Action<AsyncOperationHandle<GameObject>>((handle) => 
            {   
                if (handle.IsDone)
                {
                    Utility.LogDebug($"Companion wolf loaded!");
                    Vector3 playerPos = GameManager.m_PlayerManager.m_LastPlayerPosition;
                    AiUtils.GetClosestNavmeshPos(out Vector3 validPos, playerPos, playerPos);
                    BaseAi baseAi = handle.Result.GetComponentInChildren<BaseAi>();
                    if (baseAi == null)
                    {
                        Utility.LogError("Coult not find BaseAi script attached to wolf prefab!");
                        return;
                    }
                    mManager.TryInjectCustomAi(baseAi, null)


                }
            });
            companionObjectTask.add_Completed(onCompleted);
        }
    }
}


