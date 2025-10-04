using Il2CppTLD.AI;
using UnityEngine;
using Il2CppAK;

namespace ExpandedAiFramework
{
    public interface ICougarManager : ISubManager
    {
        VanillaCougarManager VanillaCougarManager { get; }
        bool OverrideCustomSpawnRegionType(SpawnRegion spawnRegion, SpawnRegionModDataProxy proxy, TimeOfDay timeOfDay, out CustomSpawnRegion customSpawnRegion)
        {
            customSpawnRegion = new CustomSpawnRegion(spawnRegion, proxy, timeOfDay);
            return false;
        }

        void OverrideStart() {}
    }
}
