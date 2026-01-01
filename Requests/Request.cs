

namespace ExpandedAiFramework
{
    public abstract class Request : IRequest, ILogInfoProvider
    {
        protected RequestResult mResult;
        protected bool mThreadSafe = false;
        protected bool mThreadSafeCallback = false;
        protected long mQueueTime = 0L;
        protected long mRequestStartTime = 0L;
        protected long mRequestCompleteTime = 0L;
        protected long mCallbackStartTime = 0L;
        protected long mCallbackCompleteTime = 0L;

        public bool ThreadSafe => mThreadSafe;
        public bool ThreadSafeCallback => mThreadSafeCallback;
        public RequestResult Result { get { return mResult; } }
        public long QueueTime { get {  return mQueueTime; } set {  mQueueTime = value; } }
        public long RequestStartTime { get { return mRequestStartTime; } set { mRequestStartTime = value; } }
        public long RequestCompleteTime { get { return mRequestCompleteTime; } set { mRequestCompleteTime = value; } }
        public long CallbackStartTime { get { return mCallbackStartTime; } set { mCallbackStartTime = value; } }
        public long CallbackCompleteTime { get { return mCallbackCompleteTime; } set { mCallbackCompleteTime = value; } }

        public Request() { }
        public Request(bool threadSafe, bool threadSafeCallback) : base()
        {
            mThreadSafe = threadSafe;
            mThreadSafeCallback = threadSafeCallback;
        }
        public void PerformRequest() => mResult = Validate() ? PerformRequestInternal() : RequestResult.Invalid;
        protected virtual bool Validate() => true;
        public virtual void Preprocess(ISubDataManager manager) { }
        public virtual void Reset() { }
        public virtual string InstanceInfo { get { return string.Empty; } }
        public virtual string TypeInfo { get { return $"{GetType().Name}"; } }

        public abstract void Callback();
        protected abstract RequestResult PerformRequestInternal();

        public void LogExecutionInfo(string extraInfo)
        {
            this.LogDebugInstanced($"\nExecutionInformation\nWait Time: {(mRequestStartTime - mQueueTime) * 0.00001}ms\nRequest execution time: {(mRequestCompleteTime - mRequestStartTime) * 0.0001}ms\nCallback Wait Time: {(mCallbackStartTime - mRequestCompleteTime) * 0.0001}ms\nCallback Execution Time: {(mCallbackCompleteTime - mCallbackStartTime) * 0.0001}ms\n{extraInfo}", LogCategoryFlags.Request);
        }

    }

    public abstract class Request<T> : Request where T : ISerializedData, new()
    {
        protected Action<RequestResult> mCallback;
        protected ISerializedDataProvider<T> mDataProvider;
        protected SerializedDataContainer<T> mDataContainer;

        public override string InstanceInfo { get { return string.Empty; } }
        public override string TypeInfo { get { return $"Request<{typeof(T)}>"; } }

        public Request(Action<RequestResult> callback) : base() => mCallback = callback;

        public Request(Action<RequestResult> callback, bool threadSafe, bool threadSafeCallback) : base(threadSafe, threadSafeCallback) => mCallback = callback;

        public override void Callback() => mCallback?.Invoke(mResult);
        

        protected override bool Validate()
        {
            if (mDataProvider == null)
            {
                this.ErrorInstanced($"Null data provider");
                return false;
            }
            if (mDataContainer == null)
            {
                this.ErrorInstanced($"null data container");
                return false;
            }
            return true;
        }

        public override void Preprocess(ISubDataManager manager)
        {
            if (manager is ISerializedDataProvider<T> dataProvider)
            {
                mDataProvider = dataProvider;
                mDataContainer = dataProvider.GetDataContainer();
            }
        }
    }
}
