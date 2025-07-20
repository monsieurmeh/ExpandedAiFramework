using UnityEngine;


namespace ExpandedAiFramework
{
    public class GetNearestMapDataRequest<T> : DataRequest<T> where T : IMapData, new()
    {
        protected string mScene;
        protected Vector3 mPosition;
        protected int mExtraNearestCandidatesToMaybePickFrom;
        protected bool mHasAdditionalFilters = false;
        protected Func<T, bool> mAdditionalFilters = null;

        public override string InstanceInfo { get { return $"{mPosition} in {mScene}"; } }
        public override string TypeInfo { get { return $"GetNearestMapData<{typeof(T)}>"; } }

        public GetNearestMapDataRequest(Vector3 position, string scene, Action<T, RequestResult> callback, int extraNearestCandidatesToMaybePickFrom = 0) : base(callback, false)
        {
            mScene = scene;
            mPosition = position;
            mExtraNearestCandidatesToMaybePickFrom = extraNearestCandidatesToMaybePickFrom;
            mCachedString += $"at {position} in scene {scene} with {extraNearestCandidatesToMaybePickFrom} extra top candidates to pick from";
        }

        public override void Preprocess(ISubDataManager manager)
        {
            base.Preprocess(manager);
            if (manager is ISerializedDataFilterProvider<T> filterProvider)
            {
                mAdditionalFilters = filterProvider.GetAdditionalDataFilters();
                mHasAdditionalFilters = mAdditionalFilters != null;
            }
        }


        protected override bool Validate()
        {
            if (mDataContainer == null)
            {
                this.LogTraceInstanced($"null mDataContainer");
                return false;
            }
            if (mPosition.Equals(Vector3.zero))
            {
                this.LogTraceInstanced($"zero position");
                return false;
            }
            if (string.IsNullOrEmpty(mScene))
            {
                this.LogTraceInstanced($"Null or empty scene");
                return false;
            }
            return true;
        }

        protected override RequestResult PerformRequestInternal()
        {
            mPayload = GetMapData();
            return mPayload != null ? RequestResult.Succeeded : RequestResult.Failed;
        }


        public void AttachAdditionalFilters(Func<T, bool> filter) => mAdditionalFilters = filter;


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

        private bool ValidEntry(T data) => ValidEntryInternal(data) && (mHasAdditionalFilters ? mAdditionalFilters.Invoke(data) : true);
        protected virtual bool ValidEntryInternal(T data) => !data.Claimed;
        protected virtual float OrderBy(T data) => Vector3.SqrMagnitude(mPosition - data.AnchorPosition);
    }
}
