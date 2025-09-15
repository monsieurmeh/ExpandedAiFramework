

namespace ExpandedAiFramework
{
    public interface ISerializedDataCrossReferenceProvider<T0, T1> 
        where T0 : ISerializedData, new() 
        where T1 : ISerializedData, new()
    {
        List<Guid> GetCrossReferencedList<TRefObj, TRefHolder>(Guid guid) where TRefObj : T0 where TRefHolder : T1;
    }
}
