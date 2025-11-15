

namespace ExpandedAiFramework
{
    public interface ISerializedData
    {
        Guid Guid { get; }
        string Scene { get; }
        string DataLocation { get; set; }
        string DisplayName { get; }
        bool PostProcess();
    }
}
