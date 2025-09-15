

namespace ExpandedAiFramework
{
    public class ClaimAvailableSpawnRequest : DataRequest<SpawnModDataProxy>
    {
        protected Guid mGuid;
        protected string mScene;
        protected ISerializedDataCrossReferenceProvider<SpawnRegionModDataProxy, SpawnModDataProxy> mSpawnModDataProxyProvider;


        public override string InstanceInfo { get { return $"{mGuid} in {mScene}"; } }
        public override string TypeInfo { get { return $"GetNextAvailableSpawnModDataProxy"; } }

        public ClaimAvailableSpawnRequest(Guid guid, string scene, Action<SpawnModDataProxy, RequestResult> callback, bool callbackIsThreadSafe) : base(callback, true, callbackIsThreadSafe)
        {
            mGuid = guid;
            mScene = scene;
        }

        public override void Preprocess(ISubDataManager manager)
        {
            base.Preprocess(manager);
            if (manager is ISerializedDataCrossReferenceProvider<SpawnRegionModDataProxy, SpawnModDataProxy> crossReferenceProvider)
            {
                mSpawnModDataProxyProvider = crossReferenceProvider;
            }
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
            if (mSpawnModDataProxyProvider == null)
            {
                this.LogTraceInstanced("$Null SpawnModDataProxyProvider");
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
