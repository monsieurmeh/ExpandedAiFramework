using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ExpandedAiFramework
{
    internal interface ICustomSpawnRegion
    {
        SpawnRegion SpawnRegion { get; }
        SpawnRegionModDataProxy ModDataProxy { get; }
        void Initialize(SpawnRegion spawnRegion, SpawnRegionModDataProxy modDataProxy, TimeOfDay timeOfDay);
        void Despawn(float despawnTime);
        bool TryQueueSpawn(SpawnModDataProxy proxy);
        //bool ShouldInterceptSpawn(BaseAi baseAi);
        //For now I dont think I'm going to incorporate this.
        //Adding an extra layer for actual custom spawn region functionality is a huge task
        //for now I just need a flexible and future-proof way to store an instance of a custom spawn region wrapper
        //Anyone wanting to do stuff to a spawn region can grab it from this wrapper's base implemention in EAF
    }
}
