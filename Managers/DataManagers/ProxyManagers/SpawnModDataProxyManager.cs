

namespace ExpandedAiFramework
{
    public class SpawnModDataProxyManager : ProxyManagerBase<SpawnModDataProxy>,
                                            ISerializedDataCrossReferenceProvider<SpawnRegionModDataProxy, SpawnModDataProxy>
    {
        private object mForceSpawnLock = new object();
        private Dictionary<Guid, List<Guid>> mQueuedSpawnModDataProxiesByParentGuid = new Dictionary<Guid, List<Guid>>();
        private Dictionary<Type, int> mForceSpawnCountDict = new Dictionary<Type, int>();

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
                mForceSpawnCountDict.Clear();
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
            MaybeSetForceSpawn(proxy);
            base.RefreshData(proxy);
        }


        public void MaybeSetForceSpawn(SpawnModDataProxy proxy)
        {
            if (!mManager.Manager.AiManager.SpawnSettingsDict.TryGetValue(proxy.VariantSpawnType, out ISpawnTypePickerCandidate settings)) return;
            proxy.ForceSpawn = settings.ForceSpawningEnabled() && CanForceSpawn(proxy.VariantSpawnType);

            if (!proxy.ForceSpawn) return;
            IncrementForceSpawnCount(proxy.VariantSpawnType);
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
            proxy.Spawned = true; // if it made it's way into serialization, it spawned at some point already!
            OnRegister(proxy);
            return true;
        }


        protected override void OnRegister(SpawnModDataProxy proxy)
        {
            List<Guid> queuedGuids = GetQueuedSpawnModDataProxiesByParentGuid(proxy.ParentGuid);
            if (queuedGuids.Contains(proxy.Guid)) return;

            MaybeSetForceSpawn(proxy);
            this.LogTraceInstanced($"Queueing SpawnModDataProxy {proxy.Guid} against parent guid {proxy.ParentGuid}", LogCategoryFlags.SerializedData);
            if (proxy.ForceSpawn)
            {
                queuedGuids.Insert(0, proxy.Guid);
            }
            else
            {
                queuedGuids.Add(proxy.Guid);
            }
        }


        protected override bool IsDataValid(SpawnModDataProxy proxy)
        {
            if (proxy.ParentGuid == Guid.Empty)
            {
                this.LogTraceInstanced($"Empty parent guid: {proxy}", LogCategoryFlags.SerializedData);
                return false;
            }
            if (proxy.Disconnected)
            {
                this.LogTraceInstanced($"Disconnected: {proxy}", LogCategoryFlags.SerializedData);
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
        public bool CanForceSpawn(Type type)
        {
            lock (mForceSpawnLock)
            {
                bool canForceSpawn = GetForceSpawnCount(type) < mManager.Manager.Settings.MaxForceSpawns;
                this.LogTraceInstanced($"ForceSpawnCount[{type}]: {GetForceSpawnCount(type)} | CanForceSpawn: {canForceSpawn}", LogCategoryFlags.SerializedData);
                return canForceSpawn;
            }
        }

        private int GetForceSpawnCount(Type type)
        {
            if (!mForceSpawnCountDict.TryGetValue(type, out int count))
            {
                mForceSpawnCountDict.Add(type, count = 0);
            }
            return count;
        }

        //locks force spawn, should be OK
        public void IncrementForceSpawnCount(Type type)
        {
            lock (mForceSpawnLock)
            {
                if (!mForceSpawnCountDict.TryGetValue(type, out int count))
                {
                    count = 0;
                }
                count++;
                mForceSpawnCountDict[type] = count;
                this.LogTraceInstanced($"Incrementing force spawn count for type {type}: {count - 1} -> {count}", LogCategoryFlags.SerializedData);
            }
        }


        //explicitly NOT public because I don't want this getting picked up unless it's requested!
        List<Guid> ISerializedDataCrossReferenceProvider<SpawnRegionModDataProxy, SpawnModDataProxy>.GetCrossReferencedList<T0, T1>(Guid guid) => GetQueuedSpawnModDataProxiesByParentGuid(guid);
    }
}
