using UnityEngine;


namespace ExpandedAiFramework
{
    public class PreSpawnRequest : Request<SpawnModDataProxy>
    {
        protected CustomSpawnRegion mCustomSpawnRegion;
        protected ISerializedDataCrossReferenceProvider<SpawnRegionModDataProxy, SpawnModDataProxy> mSpawnModDataProxyProvider;

        public override string InstanceInfo { get { return $"{mCustomSpawnRegion.ModDataProxy.Guid}"; } }
        public override string TypeInfo { get { return $"PreSpawn"; } }

        public PreSpawnRequest(CustomSpawnRegion customSpawnRegion) : base((result) => { }, false)
        {
            mCustomSpawnRegion = customSpawnRegion;
            mCachedString += $"for {mCustomSpawnRegion.ModDataProxy.Guid}";
        }



        protected override bool Validate()
        {
            if (mCustomSpawnRegion == null)
            {
                this.LogTraceInstanced($"null custom spawn region");
                return false;
            }
            return true;
        }


        public override void Preprocess(ISubDataManager manager)
        {
            base.Preprocess(manager);
            if (manager is ISerializedDataCrossReferenceProvider<SpawnRegionModDataProxy, SpawnModDataProxy> crossReferenceProvider)
            {
                mSpawnModDataProxyProvider = crossReferenceProvider;
            }
        }


        protected override RequestResult PerformRequestInternal()
        {
            try
            {
                int preSpawnLimit = mCustomSpawnRegion.CalculateTargetPopulation() - mCustomSpawnRegion.GetCurrentActivePopulation(mCustomSpawnRegion.VanillaSpawnRegion.m_WildlifeMode);
                int preSpawnCount = 0;
                List<SpawnModDataProxy> spawnableProxies = new List<SpawnModDataProxy>();
                foreach (Guid guid in mSpawnModDataProxyProvider.GetCrossReferencedList<SpawnRegionModDataProxy, SpawnModDataProxy>(mCustomSpawnRegion.ModDataProxy.Guid))
                {
                    if (preSpawnCount >= preSpawnLimit)
                    {
                        break;
                    }
                    if (!mDataContainer.TryGetData(mCustomSpawnRegion.ModDataProxy.Scene, guid, out SpawnModDataProxy proxy))
                    {
                        continue;
                    }
                    if (proxy.ForceSpawn)
                    {
                        spawnableProxies.Insert(0, proxy);
                        preSpawnCount++;
                        continue;
                    }
                    if (mCustomSpawnRegion.VanillaSpawnRegion.m_Radius + GameManager.m_SpawnRegionManager.m_SpawnRegionDisableDistance >= Vector3.Distance(mCustomSpawnRegion.Manager.PlayerStartPos, proxy.CurrentPosition))
                    {
                        spawnableProxies.Add(proxy);
                        preSpawnCount++;
                        continue;
                    }
                }
                foreach (SpawnModDataProxy proxy in spawnableProxies)
                {
                    mCustomSpawnRegion.QueueImmediateSpawn(proxy);
                }
                spawnableProxies.Clear();
                return RequestResult.Succeeded;
            }
            catch (Exception e)
            {
                this.LogErrorInstanced(e.Message);
                return RequestResult.Failed;
            }
        }
    }
}
