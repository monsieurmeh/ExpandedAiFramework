

namespace ExpandedAiFramework
{
    public class GetNextAvailableSpawnRequest : DataRequest<SpawnModDataProxy>
    {
        protected Guid mGuid;
        protected string mScene;
        protected bool mRequireForceSpawn;
        protected int mAttemptCount = 0;
        protected int mAttemptLimit = 0;
        protected ISerializedDataCrossReferenceProvider<SpawnRegionModDataProxy, SpawnModDataProxy> mSpawnModDataProxyProvider;

        public override string InstanceInfo { get { return $"{mGuid} in {mScene}"; } }
        public override string TypeInfo { get { return $"GetNextAvailableSpawnModDataProxy"; } }

        public GetNextAvailableSpawnRequest(Guid guid, string scene, bool requireForceSpawn, Action<SpawnModDataProxy, RequestResult> callback, bool callbackIsThreadSafe, int allowedAttempts = 1) : base(callback, true, callbackIsThreadSafe)
        {
            mGuid = guid;
            mScene = scene;
            mRequireForceSpawn = requireForceSpawn;
            mAttemptLimit = allowedAttempts;
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
                this.LogTraceInstanced($"null mDataContainer", LogCategoryFlags.Request);
                return false;
            }
            if (string.IsNullOrEmpty(mScene))
            {
                this.LogTraceInstanced($"empty or null scene", LogCategoryFlags.Request);
                return false;
            }
            if (mGuid == Guid.Empty)
            {
                this.LogTraceInstanced($"Empty guid", LogCategoryFlags.Request);
                return false;
            }
            if (mSpawnModDataProxyProvider== null)
            {
                this.LogTraceInstanced($"null data proxy provider", LogCategoryFlags.Request);
                return false;
            }
            return true;
        }


        protected override RequestResult PerformRequestInternal()
        {
            if (TryGetNextAvailableSpawnModDataProxy())
            {
                return RequestResult.Succeeded;
            }
            mAttemptCount++;
            if (mAttemptCount >= mAttemptLimit)
            {
                return RequestResult.Failed;
            }
            return RequestResult.Requeue;            
        }
        
        private bool TryGetNextAvailableSpawnModDataProxy()
        {
            bool foundValidProxy = false;
            List<Guid> availableProxies = mSpawnModDataProxyProvider.GetCrossReferencedList<SpawnRegionModDataProxy, SpawnModDataProxy>(mGuid);
            for (int i = 0, iMax = availableProxies.Count; i < iMax; i++)
            {
                if (!mDataContainer.TryGetData(mScene, availableProxies[i], out mPayload))
                {
                    this.LogErrorInstanced($"Couldnt match existing matched spawn mod data proxy guid {availableProxies[i]} to intended parent proxy guid {mGuid}!");
                    continue;
                }
                if (!mPayload.Available)
                {
                    this.LogTraceInstanced($"Proxy with guid {mPayload.Guid} unavailable.", LogCategoryFlags.Request);
                    continue;
                }
                if (mRequireForceSpawn && !mPayload.ForceSpawn)
                {
                    this.LogTraceInstanced($"Proxy with guid {mPayload.Guid} is not force spawn, skipping", LogCategoryFlags.Request);
                    continue;
                }
                foundValidProxy = true;
                break;
            }
            if (!foundValidProxy)
            {
                if (!mRequireForceSpawn)
                {
                    this.LogWarningInstanced($"Could not get proxy!");
                }
                return false;
            }
            return true;
        }
    }
}
