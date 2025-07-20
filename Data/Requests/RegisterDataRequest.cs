

namespace ExpandedAiFramework
{
    public class RegisterDataRequest<T> : DataRequest<T> where T : ISerializedData, new()
    {
        ISerializedDataProvider<T> mDataProvider;
        protected Func<T, bool> mDataValidator = null;


        public override string InstanceInfo { get { return $"{mPayload?.Guid ?? Guid.Empty}"; } }
        public override string TypeInfo { get { return $"RegisterData<{typeof(T)}>"; } }

        //Adding explicit constructor param here so I remember to attach it when we make the request!
        public RegisterDataRequest(T data, string dataLocation, Action<T, RequestResult> callback) : base(callback, false)
        {
            mPayload = data;
            mPayload.DataLocation = dataLocation;
        }


        protected override bool Validate()
        {
            if (mPayload == null)
            {
                this.LogTraceInstanced($"null data, aborting");
                return false;
            }
            if (!mDataValidator?.Invoke(mPayload) ?? false)
            {
                this.LogTraceInstanced($"Invalid data, aborting");
                return false;
            }
            return true;
        }


        protected override RequestResult PerformRequestInternal()
        {
            return mDataProvider.TryRegister(mPayload) ? RequestResult.Succeeded : RequestResult.Failed;
        }


        public override void Preprocess(ISubDataManager manager)
        {
            base.Preprocess(manager);
            if (manager is ISerializedDataValidatorProvider<T> validatorProvider)
            {
                mDataValidator = validatorProvider.GetDataValidator();
            }
            if (manager is ISerializedDataProvider<T> dataProvider)
            {
                mDataProvider = dataProvider;
            }
        }
    }
}
