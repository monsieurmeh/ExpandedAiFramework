

namespace ExpandedAiFramework
{
    public class GetDataByGuidRequest<T> : DataRequest<T> where T : ISerializedData, new()
    {
        protected string mScene;
        protected Guid mGuid;

        public override string InstanceInfo { get { return $"{mGuid} in {mScene}"; } }
        public override string TypeInfo { get { return $"GetDataByGuid<{typeof(T)}>"; } }

        public GetDataByGuidRequest(Guid guid, string scene, Action<T, RequestResult> callback) : base(callback, false)
        {
            mScene = scene;
            mGuid = guid;
        }



        protected override bool Validate()
        {
            if (mDataContainer == null)
            {
                this.LogTraceInstanced($"null mDataContainer");
                return false;
            }
            if (mGuid == Guid.Empty)
            {
                this.LogTraceInstanced($"Empty guid");
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
            return mDataContainer.TryGetData(mScene, mGuid, out mPayload) ? RequestResult.Succeeded : RequestResult.Failed;
        }
    }
}
