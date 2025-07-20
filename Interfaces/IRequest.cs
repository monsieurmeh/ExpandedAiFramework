

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
        void Reset();
        void PerformRequest();
        void Callback();
    }
}
