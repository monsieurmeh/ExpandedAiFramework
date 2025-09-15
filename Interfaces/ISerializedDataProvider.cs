

namespace ExpandedAiFramework
{
    public interface ISerializedDataProvider<T> where T : ISerializedData, new()
    {
        public SerializedDataContainer<T> GetDataContainer();
        public bool TryRegister(T data);
    }
}
