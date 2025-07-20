

namespace ExpandedAiFramework
{
    public abstract class DataRequest<T> : Request, IDataRequest<T> where T : ISerializedData, new()
    {
        protected Action<T, RequestResult> mCallback;
        protected SerializedDataContainer<T> mDataContainer;
        protected T mPayload;

        public override string InstanceInfo { get { return string.Empty; } }
        public override string TypeInfo { get { return $"DataRequest<{typeof(T)}>"; } }

        public DataRequest(Action<T, RequestResult> callback) : base() => mCallback = callback;

        public DataRequest(Action<T, RequestResult> callback, bool threadSafe) : base(threadSafe) => mCallback = callback;

        public override void Callback() => mCallback?.Invoke(mPayload, mResult);

        public override void Preprocess(ISubDataManager manager)
        {
            if (manager is ISerializedDataProvider<T> dataProvider)
            {
                mDataContainer = dataProvider.GetDataContainer();
            }
        }
    }
}
