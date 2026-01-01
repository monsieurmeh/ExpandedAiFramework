
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
            LogTrace($"Connecting!", LogCategoryFlags.Ai);
            Connected = true;
            SpawnDate = GetCurrentTimelinePoint();
            UntamedTimeoutTime = GetCurrentTimelinePoint() + CompanionWolf.CompanionWolfSettings.LingerDurationHours;
            AffectionDecayTime = GetCurrentTimelinePoint() + CompanionWolf.CompanionWolfSettings.AffectionDecayDelayHours;
            AbleToBeTamedTime = GetCurrentTimelinePoint() + CompanionWolf.CompanionWolfSettings.AffectionDaysRequirement * 24;
            LastDespawnTime = GetCurrentTimelinePoint();
        }


        public void Disconnect()
        {
            LogTrace($"Disconnecting!!", LogCategoryFlags.Ai);
            Connected = false;
        }
    }
}
