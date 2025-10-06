
        

namespace ExpandedAiFramework
{
    public interface ISpawnManager : ISubManager
    {
        bool ShouldInterceptSpawn(CustomSpawnRegion region);
        void PostProcessNewSpawnModDataProxy(SpawnModDataProxy proxy); //Useful if you want to handle custom post-processing of new spawns ahead of time, for example during scene load this will be called during spawn pre-queuing and you can take the time to cause all the load hitches you want!
        Type SpawnType { get; }
    }
}

        
