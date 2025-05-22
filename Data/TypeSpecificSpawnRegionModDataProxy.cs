using System.Xml.Linq;
using UnityEngine;


namespace ExpandedAiFramework
{
    [Serializable]
    public class TypeSpecificSpawnRegionModDataProxy : SpawnRegionModDataProxy
    {
        public Type SpawnType; 
        public TypeSpecificSpawnRegionModDataProxy() { }


        public TypeSpecificSpawnRegionModDataProxy(Guid guid, string scene, BaseAi ai, SpawnRegion spawnRegion, Type spawnType) : base(guid, scene, ai, spawnRegion)
        {
            SpawnType = spawnType;
        }


        public virtual void Respawn(BaseAi baseAi)
        {
            
        }

        public override string ToString()
        {
            return $"TypeSpecificSpawnRegionModDataProxy with guid {Guid} and spawn variant type {SpawnType} at {CurrentPosition} of type {AiType}.{AiSubType} in scene {Scene}";
        }
    }
}
