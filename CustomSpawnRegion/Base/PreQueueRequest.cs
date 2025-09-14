using ModData;
using UnityEngine;


namespace ExpandedAiFramework
{
    public class PreQueueRequest : Request<SpawnModDataProxy>
    {
        protected CustomSpawnRegion mCustomSpawnRegion;
        protected WildlifeMode mWildlifeMode;
        protected bool mCloseEnoughForPreSpawning; //should we immediately process new spawn mod data proxy generation, schedule it as a new request for later? depends on if we're close enough for prespawning! If so, that kidna needs to finish first ;)
        protected ISerializedDataCrossReferenceProvider<SpawnRegionModDataProxy, SpawnModDataProxy> mSpawnModDataProxyProvider;

        public override string InstanceInfo { get { return $"{mCustomSpawnRegion.ModDataProxy.Guid}"; } }
        public override string TypeInfo { get { return $"PreQueue"; } }

        public PreQueueRequest(CustomSpawnRegion customSpawnRegion, WildlifeMode wildlifeMode, bool closeEnoughForPreSpawning) : base((result) => { }, false, false)
        {
            mCustomSpawnRegion = customSpawnRegion;
            mWildlifeMode = wildlifeMode;
            mCloseEnoughForPreSpawning = closeEnoughForPreSpawning;
        }



        protected override bool Validate()
        {
            if (mCustomSpawnRegion == null)
            {
                this.LogTraceInstanced($"null custom spawn region");
                return false;
            }
            if (mSpawnModDataProxyProvider == null)
            {
                this.LogTraceInstanced($"null spawn mod data proxy provider");
                return false;
            }
            return base.Validate();
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
                int preQueueCount = Math.Max(mCustomSpawnRegion.GetMaxSimultaneousSpawnsDay(), mCustomSpawnRegion.GetMaxSimultaneousSpawnsNight());
                List<Guid> normalSpawns = mSpawnModDataProxyProvider.GetCrossReferencedList<SpawnRegionModDataProxy, SpawnModDataProxy>(mCustomSpawnRegion.ModDataProxy.Guid);
                for (int i = normalSpawns.Count; i < preQueueCount; i++)
                {
                    this.LogTraceInstanced($"Pre-queueing normal spawn #{i}");
                    mCustomSpawnRegion.GenerateNewRandomSpawnModDataProxy((s) =>
                    {
                        mDataProvider.TryRegister(s);
                    }, mWildlifeMode, !mCloseEnoughForPreSpawning);
                }
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
