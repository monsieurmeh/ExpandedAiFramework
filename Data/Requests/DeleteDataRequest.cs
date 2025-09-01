

namespace ExpandedAiFramework
{
    public class DeleteDataRequest<T> : GetDataByGuidRequest<T> where T : ISerializedData, new()
    {
        public override string TypeInfo { get { return $"DeleteDataRequest<{typeof(T)}>"; } }


        public DeleteDataRequest(Guid guid, string scene, Action<T, RequestResult> callback, bool callbackIsThreadSafe) : base(guid, scene, callback, callbackIsThreadSafe) { }


        protected override RequestResult PerformRequestInternal()
        {
            return (mDataContainer.TryGetData(mScene, mGuid, out mPayload) && mDataContainer.TryRemoveData(mScene, mGuid)) ? RequestResult.Succeeded : RequestResult.Failed;
        }
    }
}
