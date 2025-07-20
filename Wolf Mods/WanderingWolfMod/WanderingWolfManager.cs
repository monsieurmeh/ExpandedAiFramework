using static ExpandedAiFramework.Utility;


namespace ExpandedAiFramework.WanderingWolfMod
{
    public class WanderingWolfManager : ISubManager
    {
        protected EAFManager mManager;
        public void Initialize(EAFManager manager)
        {
            mManager = manager;
            LogDebug("WanderingWolfManager initialized!");
        }
        public bool ShouldInterceptSpawn(CustomSpawnRegion region) => false;
        public void Shutdown() { }
        public void OnStartNewGame() { }
        public void OnLoadGame() { }
        public void OnLoadScene(string sceneName) { }
        public void OnInitializedScene(string sceneName) { }
        public void OnSaveGame() { }
        public void OnQuitToMainMenu() { }
        public void Update() { }
        public Type SpawnType { get { return typeof(WanderingWolf); } }
        public void PostProcessNewSpawnModDataProxy(SpawnModDataProxy proxy)
        {
            LogVerbose($"proxy with guid <<<{proxy.Guid}>>> has custom data: {proxy.CustomData != null} with length: {proxy.CustomData?.Length ?? 0}");
            proxy.AsyncProcessing = true;
            if (proxy.CustomData == null || proxy.CustomData.Length == 0)
            {
                mManager.DataManager.ScheduleMapDataRequest<WanderPath>(new GetNearestMapDataRequest<WanderPath>(proxy.CurrentPosition, proxy.Scene, (path, result) =>
                {
                    if (result != RequestResult.Succeeded)
                    {
                        EAFManager.LogWithStackTrace($"FAILED TO GET BY GUID!");
                        return;
                    }
                    ClaimWanderPath(proxy, path);
                }, 3));
            }
            else
            {
                mManager.DataManager.ScheduleMapDataRequest<WanderPath>(new GetDataByGuidRequest<WanderPath>(new Guid(proxy.CustomData[0]), proxy.Scene, (path, result) =>
                {
                    if (result != RequestResult.Succeeded)
                    {
                        EAFManager.LogWithStackTrace($"FAILED TO GET NEAREST!!!");
                        return;
                    }
                    ClaimWanderPath(proxy, path);
                }));
            }
        }


        private void ClaimWanderPath(SpawnModDataProxy proxy, WanderPath path)
        {
            proxy.AsyncProcessing = false;
            if (path != null)
            {
                LogVerbose($"Attaching wanderpath with guid <<<{path.Guid}>>> to proxy with guid <<<{proxy.Guid}>>>");
                proxy.CustomData = [path.Guid.ToString()];
                path.Claim();
            }
        }
    }
}


