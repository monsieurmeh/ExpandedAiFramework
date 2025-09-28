using static ExpandedAiFramework.Utility;


namespace ExpandedAiFramework.TrackingWolfMod
{
    public class TrackingWolfManager : ISubManager
    {
        protected EAFManager mManager;
        public void Initialize(EAFManager manager)
        {
            mManager = manager;
            LogTrace("TrackingWolfManager initialized!");
        }
        public bool ShouldInterceptSpawn(CustomSpawnRegion region) => false;
        public void Shutdown() { }
        public void OnStartNewGame() { }
        public void OnLoadGame() { }
        public void OnLoadScene(string sceneName) { }
        public void OnInitializedScene(string sceneName) { }
        public void OnSaveGame() { }
        public void OnQuitToMainMenu() { }
        public void UpdateFromManager() { }
        public void PostProcessNewSpawnModDataProxy(SpawnModDataProxy proxy)
        {
            if (TrackingWolf.TrackingWolfSettings.ForceSpawn)
            {
                proxy.ForceSpawn = true;
            }
        }
        public Type SpawnType { get { return typeof(TrackingWolf); } }
    }
}


