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
            LogTrace("AmbushWolfManager initialized!"); 
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
        public Type SpawnType { get { return typeof(AmbushWolf); } }

        public void PostProcessNewSpawnModDataProxy(SpawnModDataProxy proxy)
        {
            LogTrace($"proxy with guid <<<{proxy.Guid}>>> has custom data: {proxy.CustomData != null} with length: {proxy.CustomData?.Length ?? 0}", LogCategoryFlags.AiManager);

            if (proxy.CustomData == null || proxy.CustomData.Length == 0)
            {
                mManager.DataManager.ScheduleMapDataRequest<HidingSpot>(new GetNearestMapDataRequest<HidingSpot>(proxy.CurrentPosition, proxy.Scene, (spot, result) =>
                {
                    if (result != RequestResult.Succeeded)
                    {
                        EAFManager.LogWithStackTrace($"FAILED TO GET BY GUID!");
                        return;
                    }
                    ClaimHidingSpot(proxy, spot);
                }, false, null, 3));
            }
            else
            {
                mManager.DataManager.ScheduleMapDataRequest<HidingSpot>(new GetDataByGuidRequest<HidingSpot>(new Guid(proxy.CustomData[0]), proxy.Scene, (spot, result) =>
                {
                    if (result != RequestResult.Succeeded)
                    {
                        EAFManager.LogWithStackTrace($"FAILED TO GET NEAREST!!!");
                        return;
                    }
                    ClaimHidingSpot(proxy, spot);
                }, false));
            }
        }


        private void ClaimHidingSpot(SpawnModDataProxy proxy, HidingSpot spot)
        {
            proxy.AsyncProcessing = false;
            if (spot != null)
            {
                LogTrace($"Attaching wanderpath with guid <<<{spot.Guid}>>> to proxy with guid <<<{proxy.Guid}>>>", LogCategoryFlags.AiManager);
                proxy.CustomData = [spot.Guid.ToString()];
                spot.Claim();
            }
        }
    }
}


