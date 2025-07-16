using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandedAiFramework
{
    public class SpawnModDataProxyManager : ProxyManager<SpawnModDataProxy>
    {
        private object mLock = new object();
        private int mForceSpawnCount = 0;
        private Dictionary<Guid, List<Guid>> mQueuedSpawnModDataProxiesByParentGuid = new Dictionary<Guid, List<Guid>>();



        public SpawnModDataProxyManager(DataManager manager, string dataLocation) : base(manager, dataLocation) { }


        public override void Clear()
        {
            ClearQueuedSpawnModDataProxiesByParentGuid();
            base.Clear();
        }


        public override void Refresh(string scene)
        {
            lock (mLock)
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


        protected override void RefreshProxy(SpawnModDataProxy proxy)
        {
            if (!mQueuedSpawnModDataProxiesByParentGuid.TryGetValue(proxy.ParentGuid, out List<Guid> queuedGuids))
            {
                queuedGuids = new List<Guid>();
                mQueuedSpawnModDataProxiesByParentGuid.Add(proxy.ParentGuid, queuedGuids);
            }
            if (queuedGuids.Contains(proxy.Guid))
            {
                this.LogErrorInstanced($"Guid collision in queued spawn guids for parent guid {proxy.ParentGuid}: {proxy}");
                return;
            }
            if (mManager.Manager.AiManager.SpawnSettingsDict.TryGetValue(proxy.VariantSpawnType, out ISpawnTypePickerCandidate settings))
            {
                proxy.ForceSpawn = settings.ForceSpawningEnabled() && CanForceSpawn();
                if (proxy.ForceSpawn)
                {
                    IncrementForceSpawnCount();
                }
            }
            this.LogTraceInstanced($"Queueing SpawnModDataProxy {proxy.Guid} against parent guid {proxy.ParentGuid}");
            queuedGuids.Add(proxy.Guid);
        }


        protected override bool PostProcessProxyAfterLoad(SpawnModDataProxy proxy)
        {
            if (!proxy.InitializeType())
            {
                this.LogErrorInstanced($"Type initialize error: {proxy}");
                return false;
            }
            return base.PostProcessProxyAfterLoad(proxy);
        }


        protected override bool IsProxyValid(SpawnModDataProxy proxy)
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
            return base.IsProxyValid(proxy);
        }


        public List<Guid> GetQueuedSpawnModDataProxiesByParentGuid(Guid guid)
        {
            if (!mQueuedSpawnModDataProxiesByParentGuid.TryGetValue(guid, out List<Guid> proxyGuidsByParentGuid))
            {
                proxyGuidsByParentGuid = new List<Guid>();
                mQueuedSpawnModDataProxiesByParentGuid.Add(guid, proxyGuidsByParentGuid);
            }
            return proxyGuidsByParentGuid;
        }


        //WARNING: If this proxy gets thrown away, bad things happen! We should de-couple the 'get next' with the 'make unavailable'
        public bool TryGetNextAvailableSpawnModDataProxy(Guid spawnRegionModDataProxyGuid, bool requireForceSpawn, out SpawnModDataProxy proxy)
        {
            proxy = null;
            bool foundValidProxy = false;
            List<Guid> availableProxies = GetQueuedSpawnModDataProxiesByParentGuid(spawnRegionModDataProxyGuid);
            for (int i = 0, iMax = availableProxies.Count; i < iMax; i++)
            {
                if (!GetSubData(mManager.Manager.CurrentScene).TryGetValue(availableProxies[i], out proxy))
                {
                    this.LogErrorInstanced($"Couldnt match existing matched spawn mod data proxy guid {availableProxies[i]} to intended parent proxy guid {spawnRegionModDataProxyGuid}!");
                    continue;
                }
                if (!proxy.Available)
                {
                    this.LogTraceInstanced($"Proxy with guid {proxy.Guid} is not currently available, skipping");
                    continue;
                }
                if (requireForceSpawn && !proxy.ForceSpawn)
                {
                    this.LogTraceInstanced($"Proxy with guid {proxy.Guid} is not force spawn, skipping");
                    continue;
                }
                foundValidProxy = true;
                break;
            }
            if (!foundValidProxy)
            {
                if (!requireForceSpawn)
                {
                    this.LogErrorInstanced($"Could not get proxy!");
                }
                return false;
            }
            proxy.Available = false;
            return true;
        }


        public bool ClaimAvailableSpawnModDataProxy(SpawnModDataProxy proxy)
        {
            List<Guid> availableProxies = GetQueuedSpawnModDataProxiesByParentGuid(proxy.ParentGuid);
            if (availableProxies.Contains(proxy.Guid))
            {
                availableProxies.RemoveAt(availableProxies.IndexOf(proxy.Guid));
                return true;
            }
            return false;
        }


        public override bool TryRegisterProxy(SpawnModDataProxy proxy)
        {
            if (base.TryRegisterProxy(proxy))
            {
                List<Guid> queuedSpawnsByParentProxy = GetQueuedSpawnModDataProxiesByParentGuid(proxy.ParentGuid);
                if (!queuedSpawnsByParentProxy.Contains(proxy.Guid))
                {
                    queuedSpawnsByParentProxy.Add(proxy.Guid);
                }
                return true;
            }
            return false;
        }


        public bool CanForceSpawn()
        {
            lock (mLock)
            {
                bool canForceSpawn = mForceSpawnCount < mManager.Manager.Settings.MaxForceSpawns;
                LogVerbose($"ForceSpawnCount: {mForceSpawnCount} | CanForceSpawn: {canForceSpawn}");
                return canForceSpawn;
            }
        }


        public void IncrementForceSpawnCount()
        {
            lock (mLock)
            {
                mForceSpawnCount++;
                LogTrace($"Incrementing force spawn count: {mForceSpawnCount - 1} -> {mForceSpawnCount}");
            }
        }
    }
}
