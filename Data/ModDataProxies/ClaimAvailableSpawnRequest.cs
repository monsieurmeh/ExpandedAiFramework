

namespace ExpandedAiFramework
{
    public class ClaimAvailableSpawnRequest : DataRequest<SpawnModDataProxy>
    {
        protected Guid mGuid;
        protected string mScene;
        protected ISerializedDataCrossReferenceProvider<SpawnRegionModDataProxy, SpawnModDataProxy> mSpawnModDataProxyProvider;


        public override string InstanceInfo { get { return $"{mGuid} in {mScene}"; } }
        public override string TypeInfo { get { return $"GetNextAvailableSpawnModDataProxy"; } }

        public ClaimAvailableSpawnRequest(Guid guid, string scene, Action<SpawnModDataProxy, RequestResult> callback) : base(callback, false)
        {
            mGuid = guid;
            mScene = scene;
        }


        protected override bool Validate()
        {
            if (mDataContainer == null)
            {
                this.LogTraceInstanced($"null mDataContainer");
                return false;
            }
            if (string.IsNullOrEmpty(mScene))
            {
                this.LogTraceInstanced($"empty or null scene");
                return false;
            }
            if (mGuid == Guid.Empty)
            {
                this.LogTraceInstanced($"Empty guid");
                return false;
            }
            return true;
        }


        protected override RequestResult PerformRequestInternal()
        {
            if (TryClaimProxy())
            {
                return RequestResult.Succeeded;
            }
             return RequestResult.Failed;
        }


        private bool TryClaimProxy()
        {
            List <Guid> availableProxies = mSpawnModDataProxyProvider.GetCrossReferencedList<SpawnRegionModDataProxy, SpawnModDataProxy>(mGuid);
            if (availableProxies.Contains(mGuid))
            {
                availableProxies.RemoveAt(availableProxies.IndexOf(mGuid));
                return true;
            }
            return false;
        }
    }
}
