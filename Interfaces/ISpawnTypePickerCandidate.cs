using UnityEngine;


namespace ExpandedAiFramework
{ 
    public interface ISpawnTypePickerCandidate
    {
        JsonModSettings Settings { get; }
        int SpawnWeight();
        bool CanSpawn(BaseAi baseAi);
    }
}
