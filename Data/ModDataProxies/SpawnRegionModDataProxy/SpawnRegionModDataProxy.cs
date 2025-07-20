using System.Xml.Linq;
using UnityEngine;
using MelonLoader.TinyJSON;

namespace ExpandedAiFramework
{
    public class SpawnRegionModDataProxy : ModDataProxy
    {
        //Temporal Data
        [Exclude] public bool Connected = false;
        [Exclude] public bool PendingForceSpawns = false;

        //Mod Data
        public AiType AiType; 
        public AiSubType AiSubType;
        public float LastDespawnTime;

        //Vanilla Data
        public float HoursPlayed;
        public float ElapsedHoursAtLastActiveReRoll;
        public bool IsActive;
        public int NumRespawnsPending;
        public float ElapasedHoursNextRespawnAllowed;
        public int NumTrapped;
        public float HoursNextTrapReset;
        public int CurrentWaypointPathIndex;
        public WildlifeMode WildlifeMode;
        public bool HasBeenDisabledByAurora;
        public bool WasActiveBeforeAurora;
        public float CooldownTimerHours;

        public SpawnRegionModDataProxy() : base() { }


        public SpawnRegionModDataProxy(Guid guid, string scene, SpawnRegion spawnRegion) : base(guid, scene, spawnRegion.transform.position)
        {
            AiType = spawnRegion.m_AiTypeSpawned;
            AiSubType = spawnRegion.m_AiSubTypeSpawned;
            LastDespawnTime = GetCurrentTimelinePoint();
            BuildCachedStringSegment();
        }


        public override void UpdateCachedString()
        {
            base.UpdateCachedString();
            BuildCachedStringSegment();
        }


        private void BuildCachedStringSegment()
        {
            mCachedString += $" of type {AiType}.{AiSubType}";
        }


        public void Save(CustomSpawnRegion spawnRegion)
        {
            LastDespawnTime = GetCurrentTimelinePoint();
            ElapsedHoursAtLastActiveReRoll = spawnRegion.VanillaSpawnRegion.m_ElapsedHoursAtLastActiveReRoll;
            NumRespawnsPending = spawnRegion.VanillaSpawnRegion.m_NumRespawnsPending;
            ElapasedHoursNextRespawnAllowed = spawnRegion.VanillaSpawnRegion.m_ElapasedHoursNextRespawnAllowed;
            NumTrapped = spawnRegion.VanillaSpawnRegion.m_NumTrapped;
            HoursNextTrapReset = spawnRegion.VanillaSpawnRegion.m_HoursNextTrapReset;
            CurrentWaypointPathIndex = spawnRegion.VanillaSpawnRegion.m_CurrentWaypointPathIndex;
            WildlifeMode = spawnRegion.VanillaSpawnRegion.m_WildlifeMode;
            HasBeenDisabledByAurora = spawnRegion.VanillaSpawnRegion.m_HasBeenDisabledByAurora;
            WasActiveBeforeAurora = spawnRegion.VanillaSpawnRegion.m_WasActiveBeforeAurora;
            CooldownTimerHours = spawnRegion.VanillaSpawnRegion.m_CooldownTimerHours;
            CurrentPosition = spawnRegion.VanillaSpawnRegion.m_Center;
        }
    }
}
