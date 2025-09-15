

namespace ExpandedAiFramework
{
    public interface ISerializedDataValidatorProvider<T> where T : ISerializedData, new()
    {
        public Func<T, bool> GetDataValidator();
    }
}
