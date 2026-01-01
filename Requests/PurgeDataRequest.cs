

namespace ExpandedAiFramework
{
    public class PurgeDataRequest<T> : DataRequest<T> where T : ISerializedData, new()
    {
        ISubDataManager mManager;
        ISerializedDataProvider<T> mDataProvider;

        public override string InstanceInfo { get { return $"{mPayload?.Guid ?? Guid.Empty}"; } }
        public override string TypeInfo { get { return $"RegisterData<{typeof(T)}>"; } }

        //Adding explicit constructor param here so I remember to attach it when we make the request!
        public PurgeDataRequest(Action<T, RequestResult> callback, bool callbackIsThreadSafe) : base(callback, true, callbackIsThreadSafe) {}


        protected override bool Validate()
        {
            if (mManager == null)
            {
                this.ErrorInstanced($"null manager, aborting");
                return false;
            }
            if (mDataProvider == null)
            {
                this.ErrorInstanced($"Null data provider, aborting");
                return false;
            }
            return true;
        }


        protected override RequestResult PerformRequestInternal()
        {
            try
            {
                mManager.ScheduleClear();
                mManager.ScheduleSave();
                return RequestResult.Succeeded;
            }
            catch (Exception e)
            {
                this.ErrorInstanced($"Error purging data: {e}");
                return RequestResult.Failed;
            }
        }

        public override void Preprocess(ISubDataManager manager)
        {
            base.Preprocess(manager);
            mManager = manager;
            if (manager is ISerializedDataProvider<T> dataProvider)
            {
                mDataProvider = dataProvider;
            }
        }
    }
}
