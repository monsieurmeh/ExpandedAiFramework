

namespace ExpandedAiFramework
{
    public interface IDataRequest<T> : IRequest where T : ISerializedData, new()
    {

    }
}
