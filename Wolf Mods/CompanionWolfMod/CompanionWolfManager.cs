
using UnityEngine;
using System.Text;
using MelonLoader.Utils;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Attributes;
using ComplexLogger;
using UnityEngine.AI;
using ModData;
using MelonLoader.TinyJSON;

namespace ExpandedAiFramework.CompanionWolfMod
{
    public class CompanionWolfManager : ISubManager
    {
        protected GameObject mPrefab;
        protected EAFManager mManager;
        protected CompanionWolf mInstance;
        protected CompanionWolfData mData;
        protected bool mInitialized = false;

        public CompanionWolfData Data { get { return mData; } set { mData = value; } }
        public Type SpawnType { get { return typeof(CompanionWolf); } }



        public void Initialize(EAFManager manager)
        {
            mManager = manager;
            mInitialized = true;
            Utility.LogVerbose("CompanionWolfManager initialized!");
        }


        public bool ShouldInterceptSpawn(BaseAi baseAi, SpawnRegion region)
        {
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
            Utility.LogDebug($"Proxy match to connected CompanionWolf data found, overriding WeightedTypePicker and spawning companionwolf where it first spawned {Utility.GetCurrentTimelinePoint() - Data.SpawnDate} hours ago!");
            return true;
        }


        public void Shutdown() { }


        public void OnLoad()
        {
            if (mData == null)
            {
                mData = new CompanionWolfData();
            }
            Utility.LogDebug($"Before load at {Utility.GetCurrentTimelinePoint()} hrs, data is null: {Data == null}; Data.tamed = {Data?.Tamed ?? false}; Data.LastDespawnTime: {Data?.LastDespawnTime ?? 0.0f}");
            string json = mManager.ModData.Load("CompanionWolfMod");
            if (json != null)
            {
                JSON.Populate(JSON.Load(json), mData);
            }
            Utility.LogDebug($"After load at {Utility.GetCurrentTimelinePoint()} hrs, data is null: {Data == null}; Data.tamed = {Data?.Tamed ?? false}; Data.LastSeen: {Data?.LastDespawnTime ?? 0.0f}");
        }


        public void OnSave()
        {
            Utility.LogDebug($"Before save at {Utility.GetCurrentTimelinePoint()} hrs, data is null: {Data == null}; Data.tamed = {Data?.Tamed ?? false}; Data.LastDespawnTime: {Data?.LastDespawnTime ?? 0.0f}");
            string json = JSON.Dump(mData);
            if (json != null)
            {
                Utility.LogDebug($"Successful convert to JSON, saving...");
                mManager.ModData.Save(json, "CompanionWolfMod");
            }
        }


        public void Update()
        {


        }
    }
}


