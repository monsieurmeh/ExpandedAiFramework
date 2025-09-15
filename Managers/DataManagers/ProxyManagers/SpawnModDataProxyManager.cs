﻿

namespace ExpandedAiFramework
{
    public class SpawnModDataProxyManager : ProxyManagerBase<SpawnModDataProxy>,
                                            ISerializedDataCrossReferenceProvider<SpawnRegionModDataProxy, SpawnModDataProxy>
    {
        private object mForceSpawnLock = new object();
        private int mForceSpawnCount = 0;
        private Dictionary<Guid, List<Guid>> mQueuedSpawnModDataProxiesByParentGuid = new Dictionary<Guid, List<Guid>>();

        public SpawnModDataProxyManager(DataManager manager, DispatchManager dispatcher, string dataLocation) : base(manager, dispatcher, dataLocation) { }

        protected override void Clear()
        {
            ClearQueuedSpawnModDataProxiesByParentGuid();
            base.Clear();
        }


        protected override void Refresh(string scene)
        {
            lock (mForceSpawnLock)
            {
                mForceSpawnCount = 0;
            }
            ClearQueuedSpawnModDataProxiesByParentGuid();
            base.Refresh(scene);
        }


        private void ClearQueuedSpawnModDataProxiesByParentGuid()
        {
            foreach (List<Guid> list in mQueuedSpawnModDataProxiesByParentGuid.Values)
            {
                list.Clear();
            }
            mQueuedSpawnModDataProxiesByParentGuid.Clear();
        }


        protected override void RefreshData(SpawnModDataProxy proxy)
        {
            proxy.Available = true;
            if (mManager.Manager.AiManager.SpawnSettingsDict.TryGetValue(proxy.VariantSpawnType, out ISpawnTypePickerCandidate settings))
            {
                proxy.ForceSpawn = settings.ForceSpawningEnabled() && CanForceSpawn();
                if (proxy.ForceSpawn)
                {
                    IncrementForceSpawnCount();
                }
            }
            base.RefreshData(proxy);
        }


        protected override bool PostProcessDataAfterLoad(SpawnModDataProxy proxy)
        {
            if (!proxy.InitializeType())
            {
                this.LogErrorInstanced($"Type initialize error: {proxy}");
                return false;
            }
            //Base method checks IsDataValid, which will fail if we run OnRegister first
            if (!base.PostProcessDataAfterLoad(proxy))
            {
                return false;
            }
            OnRegister(proxy);
            return true;
        }


        protected override void OnRegister(SpawnModDataProxy proxy)
        {
            List<Guid> queuedGuids = GetQueuedSpawnModDataProxiesByParentGuid(proxy.ParentGuid);
            if (!queuedGuids.Contains(proxy.Guid))
            {
                this.LogTraceInstanced($"Queueing SpawnModDataProxy {proxy.Guid} against parent guid {proxy.ParentGuid}");
                queuedGuids.Add(proxy.Guid);
            }
        }


        protected override bool IsDataValid(SpawnModDataProxy proxy)
        {
            if (proxy.ParentGuid == Guid.Empty)
            {
                this.LogTraceInstanced($"Empty parent guid: {proxy}");
                return false;
            }
            if (proxy.Disconnected)
            {
                this.LogTraceInstanced($"Disconnected: {proxy}");
                return false;
            }
            return base.IsDataValid(proxy);
        }


        protected List<Guid> GetQueuedSpawnModDataProxiesByParentGuid(Guid guid)
        {
            if (!mQueuedSpawnModDataProxiesByParentGuid.TryGetValue(guid, out List<Guid> proxyGuidsByParentGuid))
            {
                proxyGuidsByParentGuid = new List<Guid>();
                mQueuedSpawnModDataProxiesByParentGuid.Add(guid, proxyGuidsByParentGuid);
            }
            return proxyGuidsByParentGuid;
        }


        //locks force spawn, should be OK
        public bool CanForceSpawn()
        {
            lock (mForceSpawnLock)
            {
                bool canForceSpawn = mForceSpawnCount < mManager.Manager.Settings.MaxForceSpawns;
                LogVerbose($"ForceSpawnCount: {mForceSpawnCount} | CanForceSpawn: {canForceSpawn}");
                return canForceSpawn;
            }
        }


        //locks force spawn, should be OK
        public void IncrementForceSpawnCount()
        {
            lock (mForceSpawnLock)
            {
                mForceSpawnCount++;
                LogTrace($"Incrementing force spawn count: {mForceSpawnCount - 1} -> {mForceSpawnCount}");
            }
        }


        //explicitly NOT public because I don't want this getting picked up unless it's requested!
        List<Guid> ISerializedDataCrossReferenceProvider<SpawnRegionModDataProxy, SpawnModDataProxy>.GetCrossReferencedList<T0, T1>(Guid guid) => GetQueuedSpawnModDataProxiesByParentGuid(guid);
    }
}
