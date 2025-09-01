

namespace ExpandedAiFramework
{ 
    public enum RequestResult : int
    {
        Invalid = 0,
        Pending,
        Active,
        Failed,
        Succeeded,
        Requeue,
        COUNT
    }


    public interface IRequest
    {
        RequestResult Result { get; }
        bool ThreadSafe { get; }
        bool ThreadSafeCallback { get; }
        void Preprocess(ISubDataManager manager);
        void Reset();
        void PerformRequest();
        void Callback();
    }
}
