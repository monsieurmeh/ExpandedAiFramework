using ComplexLogger;
using Il2CppRewired;
using UnityEngine;


namespace ExpandedAiFramework.CompanionWolfMod
{
    [Serializable]
    public class CompanionWolfData
    {
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
        public string LastScene = string.Empty;
        public Vector3 LastPosition = Vector3.zero;

        public CompanionWolfData() { }


        public void TryConnect()
        {
            if (!Connected) //important safety check, if not connected then we have a "new spawn" to generate. this will do so! We should ensure it gets registered with the new spawn manager so the submanager doesnt have to
            {
                Connect();
            }
        }


        public void Connect()
        {
            Utility.LogVerbose($"Connecting!");
            Connected = true;
            SpawnDate = Utility.GetCurrentTimelinePoint();
            UntamedTimeoutTime = Utility.GetCurrentTimelinePoint() + CompanionWolf.CompanionWolfSettings.LingerDurationHours;
            AffectionDecayTime = Utility.GetCurrentTimelinePoint() + CompanionWolf.CompanionWolfSettings.AffectionDecayDelayHours;
            AbleToBeTamedTime = Utility.GetCurrentTimelinePoint() + CompanionWolf.CompanionWolfSettings.AffectionDaysRequirement * 24;
            LastDespawnTime = Utility.GetCurrentTimelinePoint();
        }


        public void Disconnect()
        {
            Utility.LogVerbose($"Disconnecting!!");
            Connected = false;
        }
    }
}
