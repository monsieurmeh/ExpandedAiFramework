using System.Xml.Linq;
using UnityEngine;
using MelonLoader.TinyJSON;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ExpandedAiFramework
{
    public class SpawnRegionModDataProxy : ModDataProxy
    {
        //Temporal Data
        [JsonIgnore] public bool Connected = false;
        [JsonIgnore] public bool PendingForceSpawns = false;

        //Mod Data
        public AiType AiType; 
        public AiSubType AiSubType;
        public WolfType WolfType;
        public float LastDespawnTime;
        public string Name;

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
        }


        public void Save(CustomSpawnRegion spawnRegion)
        {
            LastDespawnTime = GetCurrentTimelinePoint();
            if (spawnRegion.VanillaSpawnRegion == null)
            {
                return;
            }
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
            BaseAi baseAi = spawnRegion.VanillaSpawnRegion.m_SpawnablePrefab.GetComponent<BaseAi>();
            if (baseAi != null)
            {
                if (baseAi.m_AiSubType == AiSubType.Wolf)
                {
                    WolfType = baseAi.NormalWolf == null ? WolfType.Timberwolf : WolfType.Normal;
                }
                else if (baseAi.m_AiSubType == AiSubType.Rabbit)
                {
                    WolfType = baseAi.Ptarmigan == null ? WolfType.Timberwolf : WolfType.Normal;
                }
                else
                {
                    WolfType = WolfType.Normal;
                }
            }
        }
    }
}
