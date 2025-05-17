using ComplexLogger;
using Il2CppRewired;
using UnityEngine;


namespace ExpandedAiFramework.CompanionWolfMod
{
    //effectively the companion wolf
    [Serializable]
    public class CompanionWolfData
    {
        public SpawnRegionModDataProxy SpawnRegionModDataProxy;
        public bool Connected = false;
        public bool Tamed = false;
        public float CurrentAffection = 0.0f;
        public float CurrentCalories = 0.0f;
        public float CurrentCondition = CompanionWolf.Settings.MaximumCondition;
        public float MaxCondition = CompanionWolf.Settings.MaximumCondition;
        public float SpawnDate;
        public float UntamedTimeoutTime;
        public float AffectionDecayTime;
        public float AbleToBeTamedTime;
        public float LastDespawnTime;
        public float Scale = 0.6f;

        public CompanionWolfData() { }


        public void Initialize(string scene, BaseAi ai, SpawnRegion spawnRegion)
        {
            if (!Connected)
            {
                Initialize(spawnRegion != null ? new SpawnRegionModDataProxy(scene, ai, spawnRegion) : null);
            }
        }


        public void Initialize(SpawnRegionModDataProxy proxy)
        {
            Utility.LogDebug($"Connecting!");
            Connected = true;
            SpawnDate = Utility.GetCurrentTimelinePoint();
            UntamedTimeoutTime = Utility.GetCurrentTimelinePoint() + CompanionWolf.Settings.LingerDurationHours;
            AffectionDecayTime = Utility.GetCurrentTimelinePoint() + CompanionWolf.Settings.AffectionDecayDelayHours;
            AbleToBeTamedTime = Utility.GetCurrentTimelinePoint() + CompanionWolf.Settings.AffectionDaysRequirement * 24;
            LastDespawnTime = Utility.GetCurrentTimelinePoint();
            SpawnRegionModDataProxy = proxy;
            
        }


        public void Disconnect()
        {
            Utility.LogDebug($"Disconnecting!!");
            SpawnRegionModDataProxy = null;
            Connected = false;
        }
    }
}
