using ComplexLogger;
using UnityEngine;


namespace ExpandedAiFramework.CompanionWolfMod
{
    //effectively the companion wolf
    [Serializable]
    public class CompanionWolfData
    {
        public SpawnRegionModDataProxy SpawnRegionModDataProxy;
        public bool Connected = false;
        public bool Indoors = false;
        public bool Tamed = false;
        public float CurrentAffection = 100f;
        public float CurrentCalories = CompanionWolf.Settings.MaximumCalorieIntake;
        public float CurrentCondition = CompanionWolf.Settings.MaximumCondition;
        public float MaxCondition = CompanionWolf.Settings.MaximumCondition;
        public float SpawnDate;
        public float UntamedTimeoutTime;
        public float AffectionDecayTime;
        public float TamingAllowedTime;
        public float LastDespawnTime;
        public float Scale = 0.6f;

        public CompanionWolfData() { }


        public void Initialize(string scene, BaseAi ai, SpawnRegion spawnRegion)
        {
            if (!Connected)
            {
                Utility.LogDebug($"Connecting!");
                Connected = true;
                SpawnDate = Utility.GetCurrentTimelinePoint();
                UntamedTimeoutTime = Utility.GetCurrentTimelinePoint() + CompanionWolf.Settings.LingerDurationHours * Utility.HoursToSeconds;
                AffectionDecayTime = Utility.GetCurrentTimelinePoint() + CompanionWolf.Settings.AffectionDecayDelayHours * Utility.HoursToSeconds;
                TamingAllowedTime = Utility.GetCurrentTimelinePoint() + CompanionWolf.Settings.AffectionDaysRequirement * Utility.DaysToSeconds;
                LastDespawnTime = Utility.GetCurrentTimelinePoint();
                SpawnRegionModDataProxy = new SpawnRegionModDataProxy(scene, ai, spawnRegion);
            }
        }


        public void Disconnect()
        {
            Utility.LogDebug($"Disconnecting!!");
            SpawnRegionModDataProxy = null;
            Connected = false;
        }
    }
}
