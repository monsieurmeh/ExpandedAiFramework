using UnityEngine;


namespace ExpandedAiFramework
{
    public class GetNearestMapDataRequest<T> : DataRequest<T> where T : IMapData, new()
    {
        protected string mScene;
        protected Vector3 mPosition;
        protected string mInstanceInfo;
        protected int mExtraNearestCandidatesToMaybePickFrom;
        protected bool mHasAdditionalFilters = false;
        protected Func<T, bool> mManagerScopeFilter = null;
        protected Func<T, bool> mRequestScopeFilter = null;

        public override string InstanceInfo { get { return mInstanceInfo; } }
        public override string TypeInfo { get { return $"GetNearestMapData<{typeof(T)}>"; } }

        public GetNearestMapDataRequest(Vector3 position, string scene, Action<T, RequestResult> callback, bool callbackIsThreadSafe, Func<T, bool> additionalFilter = null, int extraNearestCandidatesToMaybePickFrom = 0) : base(callback, true, callbackIsThreadSafe)
        {
            mScene = scene;
            mPosition = position;
            mExtraNearestCandidatesToMaybePickFrom = extraNearestCandidatesToMaybePickFrom;
            mRequestScopeFilter = additionalFilter;
            mInstanceInfo = $"{mPosition} in {mScene} with {extraNearestCandidatesToMaybePickFrom} extra picks";
        }

        public override void Preprocess(ISubDataManager manager)
        {
            base.Preprocess(manager);
            if (manager is ISerializedDataFilterProvider<T> filterProvider)
            {
                mManagerScopeFilter = filterProvider.GetAdditionalDataFilters();
            }
            mHasAdditionalFilters = mManagerScopeFilter != null || mRequestScopeFilter != null;
        }


        protected override bool Validate()
        {
            if (mDataContainer == null)
            {
                this.LogTraceInstanced($"null mDataContainer", LogCategoryFlags.Request);
                return false;
            }
            if (mPosition.Equals(Vector3.zero))
            {
                this.LogTraceInstanced($"zero position", LogCategoryFlags.Request);
                return false;
            }
            if (string.IsNullOrEmpty(mScene))
            {
                this.LogTraceInstanced($"Null or empty scene", LogCategoryFlags.Request);
                return false;
            }
            return true;
        }

        protected override RequestResult PerformRequestInternal()
        {
            mPayload = GetMapData();
            return mPayload != null ? RequestResult.Succeeded : RequestResult.Failed;
        }

        protected virtual T GetMapData()
        {
            Dictionary<Guid, T> sceneData = mDataContainer.GetSceneData(mScene);

            if (sceneData.Values.Count == 0)
            {
                return default;
            }

            // Optionally pick from nearest N
            if (mExtraNearestCandidatesToMaybePickFrom > 0)
            {
                List<T> nearest = sceneData.Values
                    .Where(ValidEntry)
                    .OrderBy(OrderBy)
                    .Take(mExtraNearestCandidatesToMaybePickFrom)
                    .ToList();

                return nearest[UnityEngine.Random.Range(0, nearest.Count)];
            }
            else
            {
                return sceneData.Values
                    .Where(ValidEntry)
                    .OrderBy(OrderBy)
                    .First();
            }
            
        }

        private bool ValidEntry(T data)
        {
            if (!ValidEntryInternal(data))
            {
                return false;
            }
            if (mHasAdditionalFilters)
            {
                if (mRequestScopeFilter?.Invoke(data) ?? false)
                {
                    return false;
                }
                if (mManagerScopeFilter?.Invoke(data) ?? false)
                {
                    return false;
                }
            }
            return true;
        }

        protected virtual bool ValidEntryInternal(T data) => !data.Claimed;
        protected virtual float OrderBy(T data) => Vector3.SqrMagnitude(mPosition - data.AnchorPosition);
    }
}
