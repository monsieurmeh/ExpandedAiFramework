

namespace ExpandedAiFramework
{
    public interface ISerializedDataFilterProvider<T> where T : ISerializedData, new()
    {
        public Func<T, bool> GetAdditionalDataFilters();
    }
}
