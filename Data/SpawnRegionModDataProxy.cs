using System.Xml.Linq;
using UnityEngine;


namespace ExpandedAiFramework
{
    [Serializable]
    public class SpawnRegionModDataProxy : ModDataProxyBase
    {
        //Temporal Data
        [NonSerialized] public bool Connected = false;
        [NonSerialized] public bool PendingForceSpawns = false;

        //Mod Data
        public Vector3 CurrentPosition;
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

        public SpawnRegionModDataProxy() { }


        //Only constructs mod data; it is on managers to serialize in vanilla data later
        public SpawnRegionModDataProxy(Guid guid, string scene, SpawnRegion spawnRegion)
        {
            Guid = guid;
            Scene = scene;
            CurrentPosition = spawnRegion.transform.position;
            AiType = spawnRegion.m_AiTypeSpawned;
            AiSubType = spawnRegion.m_AiSubTypeSpawned;
            LastDespawnTime = GetCurrentTimelinePoint();
        }


        public void Save(CustomBaseSpawnRegion spawnRegion)
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


        public override string ToString()
        {
            return $"SpawnRegionModDataProxy with guid {Guid} at {CurrentPosition} of type {AiType}.{AiSubType} in scene {Scene}";
        }


        public override int GetHashCode() => Guid.GetHashCode();

        public override bool Equals(object obj) => this.Equals(obj as SpawnRegionModDataProxy);

        public bool Equals(SpawnRegionModDataProxy proxy)
        {
            if (proxy is null)
            {
                return false;
            }

            return Guid == proxy.Guid;
        }

        public static bool operator ==(SpawnRegionModDataProxy lhs, SpawnRegionModDataProxy rhs)
        {
            if (lhs is null)
            {
                if (rhs is null)
                {
                    return true;
                }
                return false;
            }
            return lhs.Equals(rhs);
        }
        public static bool operator !=(SpawnRegionModDataProxy lhs, SpawnRegionModDataProxy rhs) => !(lhs == rhs);
    }
}
