

namespace ExpandedAiFramework
{
    public class GetMapDataByNameRequest<T> : DataRequest<T> where T : IMapData, new()
    {
        protected string mScene;
        protected string mName;

        public override string InstanceInfo { get { return $"{mName} in {mScene}"; } }
        public override string TypeInfo { get { return $"GetDataByGuid<{typeof(T)}>"; } }

        public GetMapDataByNameRequest(string name, string scene, Action<T, RequestResult> callback, bool callbackIsThreadSafe) : base(callback, true, callbackIsThreadSafe)
        {
            mScene = scene;
            mName = name;
        }



        protected override bool Validate()
        {
            if (mDataContainer == null)
            {
                this.LogTraceInstanced($"null mDataContainer");
                return false;
            }
            if (string.IsNullOrEmpty(mName))
            {
                this.LogTraceInstanced($"Numm or empty name guid");
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
            foreach (T data in mDataContainer.GetSceneData(mScene).Values)
            {
                if (data == null)
                {
                    continue;
                }
                if (data.Name == mName)
                {
                    mPayload = data;
                    return RequestResult.Succeeded;
                }
            }
            return RequestResult.Failed;
        }
    }
}
