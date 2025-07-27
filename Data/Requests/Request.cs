

namespace ExpandedAiFramework
{
    public abstract class Request : IRequest, ILogInfoProvider
    {
        protected RequestResult mResult;
        protected bool mThreadSafe = false;

        public bool ThreadSafe => mThreadSafe;
        public RequestResult Result { get { return mResult; } }

        public Request() { }
        public Request(bool threadSafe) : base() => mThreadSafe = threadSafe;

        public void PerformRequest() => mResult = Validate() ? PerformRequestInternal() : RequestResult.Invalid;
        protected virtual bool Validate() => true;
        public virtual void Preprocess(ISubDataManager manager) { }
        public virtual void Reset() { }
        public virtual string InstanceInfo { get { return string.Empty; } }
        public virtual string TypeInfo { get { return $"{GetType().Name}"; } }

        public abstract void Callback();
        protected abstract RequestResult PerformRequestInternal();
    }

    public abstract class Request<T> : Request where T : ISerializedData, new()
    {
        protected Action<RequestResult> mCallback;
        protected ISerializedDataProvider<T> mDataProvider;
        protected SerializedDataContainer<T> mDataContainer;

        public override string InstanceInfo { get { return string.Empty; } }
        public override string TypeInfo { get { return $"Request<{typeof(T)}>"; } }

        public Request(Action<RequestResult> callback) : base() => mCallback = callback;

        public Request(Action<RequestResult> callback, bool threadSafe) : base(threadSafe) => mCallback = callback;

        public override void Callback() => mCallback?.Invoke(mResult);


        protected override bool Validate()
        {
            if (mDataProvider == null)
            {
                this.LogErrorInstanced($"Null data provider");
                return false;
            }
            if (mDataContainer == null)
            {
                this.LogErrorInstanced($"null data container");
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
