using UnityEngine;


namespace ExpandedAiFramework
{ 
    public interface ISpawnTypePickerCandidate
    {
        int SpawnWeight();
        bool CanSpawn(BaseAi baseAi);
        bool ForceSpawningEnabled();
    }
}
