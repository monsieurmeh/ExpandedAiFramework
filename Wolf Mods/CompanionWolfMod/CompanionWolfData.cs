using ComplexLogger;
using Il2CppRewired;
using UnityEngine;


namespace ExpandedAiFramework.CompanionWolfMod
{
    //effectively the companion wolf
    [Serializable]
    public class CompanionWolfData
    {
        public TypeSpecificSpawnRegionModDataProxy SpawnRegionModDataProxy;
        public bool Connected = false;
        public bool Tamed = false;
        public float CurrentAffection = 0.0f;
        public float CurrentCalories = 0.0f;
        public float CurrentCondition = CompanionWolf.CompanionWolfSettings.MaximumCondition;
        public float MaxCondition = CompanionWolf.CompanionWolfSettings.MaximumCondition;
        public float SpawnDate;
        public float UntamedTimeoutTime;
        public float AffectionDecayTime;
        public float AbleToBeTamedTime;
        public float LastDespawnTime;
        public float Scale = 0.6f;

        public CompanionWolfData() { }


        public void Initialize(string scene, BaseAi ai, SpawnRegion spawnRegion)
        {
            if (!Connected) //important safety check, if not connected then we have a "new spawn" to generate. this will do so! We should ensure it gets registered with the new spawn manager so the submanager doesnt have to
            {
                Initialize(spawnRegion != null ? new TypeSpecificSpawnRegionModDataProxy(new Guid(), scene, ai, spawnRegion, typeof(CompanionWolf)) : null);
            }
        }


        public void Initialize(TypeSpecificSpawnRegionModDataProxy proxy)
        {
            Utility.LogVerbose($"Connecting!");
            Connected = true;
            SpawnDate = Utility.GetCurrentTimelinePoint();
            UntamedTimeoutTime = Utility.GetCurrentTimelinePoint() + CompanionWolf.CompanionWolfSettings.LingerDurationHours;
            AffectionDecayTime = Utility.GetCurrentTimelinePoint() + CompanionWolf.CompanionWolfSettings.AffectionDecayDelayHours;
            AbleToBeTamedTime = Utility.GetCurrentTimelinePoint() + CompanionWolf.CompanionWolfSettings.AffectionDaysRequirement * 24;
            LastDespawnTime = Utility.GetCurrentTimelinePoint();
            SpawnRegionModDataProxy = proxy;
            
        }


        public void Disconnect()
        {
            Utility.LogVerbose($"Disconnecting!!");
            SpawnRegionModDataProxy = null;
            Connected = false;
        }
    }
}
