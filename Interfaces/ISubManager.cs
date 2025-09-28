

namespace ExpandedAiFramework
{
    public interface ISubManager
    {
        void Initialize(EAFManager manager);
        void Shutdown();
        void OnStartNewGame();
        void OnLoadScene(string sceneName);
        void OnInitializedScene(string sceneName);
        void OnSaveGame();
        void OnLoadGame();
        void UpdateFromManager();
        void OnQuitToMainMenu();
        bool ShouldInterceptSpawn(CustomSpawnRegion region);
        void PostProcessNewSpawnModDataProxy(SpawnModDataProxy proxy); //Useful if you want to handle custom post-processing of new spawns ahead of time, for example during scene load this will be called during spawn pre-queuing and you can take the time to cause all the load hitches you want!
        Type SpawnType { get; }
    }
}
