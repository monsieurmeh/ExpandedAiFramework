using ExpandedAiFramework.AmbushWolfMod;
using static ExpandedAiFramework.Utility;


namespace ExpandedAiFramework.WanderingWolfMod
{
    public class AmbushWolfManager : ISubManager
    {
        protected EAFManager mManager;

        public void Initialize(EAFManager manager) 
        { 
            mManager = manager;  
            LogDebug("AmbushWolfManager initialized!"); 
        }
        public bool ShouldInterceptSpawn(BaseAi baseAi, SpawnRegion region) => false;
        public void Shutdown() { }
        public void OnStartNewGame() { }
        public void OnLoadGame() { }
        public void OnLoadScene(string sceneName) { }
        public void OnInitializedScene(string sceneName) { }
        public void OnSaveGame() { }
        public void OnQuitToMainMenu() { }
        public void Update() { }
        public void PostProcessNewSpawnModDataProxy(SpawnModDataProxy proxy)
        {
            LogTrace($"proxy with guid <<<{proxy.Guid}>>> has custom data: {proxy.CustomData != null} with length: {proxy.CustomData?.Length ?? 0}");
            if (proxy.CustomData == null || proxy.CustomData.Length == 0 || !mManager.DataManager.AvailableHidingSpots.ContainsKey(new Guid(proxy.CustomData[0])))
            {
                LogTrace($"No custom data, short custom data or no available hiding spot using packed GUID for proxy with guid <<<{proxy.Guid}>>>, dispatching request for new");
                proxy.AsyncProcessing = true;
                mManager.DataManager.GetNearestHidingSpotAsync(proxy.CurrentPosition, new Action<HidingSpot>((spot) =>
                {
                    proxy.AsyncProcessing = false;
                    if (spot != null)
                    {
                        LogDebug($"Attaching spot with guid <<<{spot.Guid}>>> to proxy with guid <<<{proxy.Guid}>>>");
                        proxy.CustomData = [spot.Guid.ToString()];
                        spot.Claim();
                    }
                }), 3);
            }
        }
        public Type SpawnType { get { return typeof(AmbushWolf); } }
    }
}


