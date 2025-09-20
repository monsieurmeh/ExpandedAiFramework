using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Il2Cpp;

namespace ExpandedAiFramework
{
    // Base request for debug menu queries
    public abstract class DebugMenuRequest : Request
    {
        protected DebugMenuRequest(bool threadSafe = true, bool threadSafeCallback = false) : base(threadSafe, threadSafeCallback) { }
    }

    // Request to get all SpawnModDataProxies with filtering
    public class GetSpawnModDataProxiesRequest : DebugMenuRequest
    {
        private Action<List<SpawnModDataProxy>, RequestResult> mCallback;
        private WildlifeMode mWildlifeMode;
        private string mSceneFilter;
        private AiSubType? mAiSubTypeFilter;
        private List<SpawnModDataProxy> mResults;
        private SpawnModDataProxyManager mManager;

        public GetSpawnModDataProxiesRequest(
            WildlifeMode wildlifeMode, 
            Action<List<SpawnModDataProxy>, RequestResult> callback,
            string sceneFilter = null,
            AiSubType? aiSubTypeFilter = null) : base()
        {
            mWildlifeMode = wildlifeMode;
            mCallback = callback;
            mSceneFilter = sceneFilter;
            mAiSubTypeFilter = aiSubTypeFilter;
            mResults = new List<SpawnModDataProxy>();
        }

        public override void Preprocess(ISubDataManager manager)
        {
            if (manager is SpawnModDataProxyManager proxyManager)
            {
                mManager = proxyManager;
            }
        }

        protected override RequestResult PerformRequestInternal()
        {
            if (mManager == null)
            {
                LogError("SpawnModDataProxyManager not found");
                return RequestResult.Failed;
            }

            var container = mManager.GetDataContainer();
            if (container == null)
            {
                LogError("Data container is null");
                return RequestResult.Failed;
            }

            var allData = container.EnumerateContents();
            mResults.Clear();

            foreach (var proxy in allData)
            {
                if (proxy.WildlifeMode != mWildlifeMode)
                    continue;

                if (!string.IsNullOrEmpty(mSceneFilter) && !proxy.Scene.Contains(mSceneFilter))
                    continue;

                if (mAiSubTypeFilter.HasValue && proxy.AiSubType != mAiSubTypeFilter.Value)
                    continue;

                mResults.Add(proxy);
            }

            return RequestResult.Succeeded;
        }

        public override void Callback()
        {
            mCallback?.Invoke(mResults, mResult);
        }
    }

    // Request to get all SpawnRegionModDataProxies with filtering
    public class GetSpawnRegionModDataProxiesRequest : DebugMenuRequest
    {
        private Action<List<SpawnRegionModDataProxy>, RequestResult> mCallback;
        private string mSceneFilter;
        private AiType? mAiTypeFilter;
        private AiSubType? mAiSubTypeFilter;
        private List<SpawnRegionModDataProxy> mResults;
        private SpawnRegionModDataProxyManager mManager;

        public GetSpawnRegionModDataProxiesRequest(
            Action<List<SpawnRegionModDataProxy>, RequestResult> callback,
            string sceneFilter = null,
            AiType? aiTypeFilter = null,
            AiSubType? aiSubTypeFilter = null) : base()
        {
            mCallback = callback;
            mSceneFilter = sceneFilter;
            mAiTypeFilter = aiTypeFilter;
            mAiSubTypeFilter = aiSubTypeFilter;
            mResults = new List<SpawnRegionModDataProxy>();
        }

        public override void Preprocess(ISubDataManager manager)
        {
            if (manager is SpawnRegionModDataProxyManager proxyManager)
            {
                mManager = proxyManager;
            }
        }

        protected override RequestResult PerformRequestInternal()
        {
            if (mManager == null)
            {
                LogError("SpawnRegionModDataProxyManager not found");
                return RequestResult.Failed;
            }

            var container = mManager.GetDataContainer();
            if (container == null)
            {
                LogError("Data container is null");
                return RequestResult.Failed;
            }

            var allData = container.EnumerateContents();
            mResults.Clear();

            foreach (var proxy in allData)
            {
                if (!string.IsNullOrEmpty(mSceneFilter) && !proxy.Scene.Contains(mSceneFilter))
                    continue;

                if (mAiTypeFilter.HasValue && proxy.AiType != mAiTypeFilter.Value)
                    continue;

                if (mAiSubTypeFilter.HasValue && proxy.AiSubType != mAiSubTypeFilter.Value)
                    continue;

                mResults.Add(proxy);
            }

            return RequestResult.Succeeded;
        }

        public override void Callback()
        {
            mCallback?.Invoke(mResults, mResult);
        }
    }

    // Request to get HidingSpots with filtering
    public class GetHidingSpotsRequest : DebugMenuRequest
    {
        private Action<List<HidingSpot>, RequestResult> mCallback;
        private string mSceneFilter;
        private string mNameFilter;
        private List<HidingSpot> mResults;
        private HidingSpotManager mManager;

        public GetHidingSpotsRequest(
            Action<List<HidingSpot>, RequestResult> callback,
            string sceneFilter = null,
            string nameFilter = null) : base()
        {
            mCallback = callback;
            mSceneFilter = sceneFilter;
            mNameFilter = nameFilter;
            mResults = new List<HidingSpot>();
        }

        public override void Preprocess(ISubDataManager manager)
        {
            if (manager is HidingSpotManager hidingSpotManager)
            {
                mManager = hidingSpotManager;
            }
        }

        protected override RequestResult PerformRequestInternal()
        {
            if (mManager == null)
            {
                LogError("HidingSpotManager not found");
                return RequestResult.Failed;
            }

            var container = mManager.GetDataContainer();
            if (container == null)
            {
                LogError("Data container is null");
                return RequestResult.Failed;
            }

            var allData = container.EnumerateContents();
            mResults.Clear();

            foreach (var spot in allData)
            {
                if (!string.IsNullOrEmpty(mSceneFilter) && !spot.Scene.Contains(mSceneFilter))
                    continue;

                if (!string.IsNullOrEmpty(mNameFilter) && !spot.Name.Contains(mNameFilter))
                    continue;

                mResults.Add(spot);
            }

            return RequestResult.Succeeded;
        }

        public override void Callback()
        {
            mCallback?.Invoke(mResults, mResult);
        }
    }

    // Request to get WanderPaths with filtering
    public class GetWanderPathsRequest : DebugMenuRequest
    {
        private Action<List<WanderPath>, RequestResult> mCallback;
        private string mSceneFilter;
        private string mNameFilter;
        private WanderPathTypes? mTypeFilter;
        private List<WanderPath> mResults;
        private WanderPathManager mManager;

        public GetWanderPathsRequest(
            Action<List<WanderPath>, RequestResult> callback,
            string sceneFilter = null,
            string nameFilter = null,
            WanderPathTypes? typeFilter = null) : base()
        {
            mCallback = callback;
            mSceneFilter = sceneFilter;
            mNameFilter = nameFilter;
            mTypeFilter = typeFilter;
            mResults = new List<WanderPath>();
        }

        public override void Preprocess(ISubDataManager manager)
        {
            if (manager is WanderPathManager wanderPathManager)
            {
                mManager = wanderPathManager;
            }
        }

        protected override RequestResult PerformRequestInternal()
        {
            if (mManager == null)
            {
                LogError("WanderPathManager not found");
                return RequestResult.Failed;
            }

            var container = mManager.GetDataContainer();
            if (container == null)
            {
                LogError("Data container is null");
                return RequestResult.Failed;
            }

            var allData = container.EnumerateContents();
            mResults.Clear();

            foreach (var path in allData)
            {
                if (!string.IsNullOrEmpty(mSceneFilter) && !path.Scene.Contains(mSceneFilter))
                    continue;

                if (!string.IsNullOrEmpty(mNameFilter) && !path.Name.Contains(mNameFilter))
                    continue;

                if (mTypeFilter.HasValue && path.WanderPathType != mTypeFilter.Value)
                    continue;

                mResults.Add(path);
            }

            return RequestResult.Succeeded;
        }

        public override void Callback()
        {
            mCallback?.Invoke(mResults, mResult);
        }
    }

    // Request to update SpawnModDataProxy
    public class UpdateSpawnModDataProxyRequest : DebugMenuRequest
    {
        private Action<SpawnModDataProxy, RequestResult> mCallback;
        private SpawnModDataProxy mProxy;
        private SpawnModDataProxyManager mManager;

        public UpdateSpawnModDataProxyRequest(SpawnModDataProxy proxy, Action<SpawnModDataProxy, RequestResult> callback) : base()
        {
            mProxy = proxy;
            mCallback = callback;
        }

        public override void Preprocess(ISubDataManager manager)
        {
            if (manager is SpawnModDataProxyManager proxyManager)
            {
                mManager = proxyManager;
            }
        }

        protected override RequestResult PerformRequestInternal()
        {
            if (mManager == null || mProxy == null)
            {
                return RequestResult.Failed;
            }

            // The proxy should already be updated in the container since we're modifying the reference
            // This request mainly serves as a way to trigger save operations if needed
            return RequestResult.Succeeded;
        }

        public override void Callback()
        {
            mCallback?.Invoke(mProxy, mResult);
        }
    }

    // Request to update SpawnRegionModDataProxy
    public class UpdateSpawnRegionModDataProxyRequest : DebugMenuRequest
    {
        private Action<SpawnRegionModDataProxy, RequestResult> mCallback;
        private SpawnRegionModDataProxy mProxy;
        private SpawnRegionModDataProxyManager mManager;

        public UpdateSpawnRegionModDataProxyRequest(SpawnRegionModDataProxy proxy, Action<SpawnRegionModDataProxy, RequestResult> callback) : base()
        {
            mProxy = proxy;
            mCallback = callback;
        }

        public override void Preprocess(ISubDataManager manager)
        {
            if (manager is SpawnRegionModDataProxyManager proxyManager)
            {
                mManager = proxyManager;
            }
        }

        protected override RequestResult PerformRequestInternal()
        {
            if (mManager == null || mProxy == null)
            {
                return RequestResult.Failed;
            }

            return RequestResult.Succeeded;
        }

        public override void Callback()
        {
            mCallback?.Invoke(mProxy, mResult);
        }
    }
}
