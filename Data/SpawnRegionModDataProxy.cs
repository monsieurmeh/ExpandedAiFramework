using System.Xml.Linq;
using UnityEngine;


namespace ExpandedAiFramework
{
    //adding guid since I plan to allow people to stack these on spawn regions, which I will be using as dictionary keys to store these on deserialize scene
    [Serializable]
    public class SpawnRegionModDataProxy
    {
        public Guid Guid = Guid.Empty;
        public string Scene; //might be able to get rid of this?
        public Vector3 OriginalPosition;
        public Vector3 CurrentPosition;
        public AiType AiType; 
        public AiSubType AiSubType;
        public float LastDespawnTime;


        public SpawnRegionModDataProxy() { }


        public SpawnRegionModDataProxy(Guid guid, string scene, SpawnRegion spawnRegion)
        {
            Guid = guid;
            Scene = scene;
            CurrentPosition = spawnRegion.transform.position;
            OriginalPosition = CurrentPosition;
            AiType = spawnRegion.m_AiTypeSpawned;
            AiSubType = spawnRegion.m_AiSubTypeSpawned;
            LastDespawnTime = GetCurrentTimelinePoint();
        }


        public override string ToString()
        {
            return $"SpawnRegionModDataProxy with guid {Guid} at {CurrentPosition} [original: {OriginalPosition}] of type {AiType}.{AiSubType} in scene {Scene}";
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
