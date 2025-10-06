

namespace ExpandedAiFramework.Enums
{
    public enum LogCategory : int 
    {
        None = 0,
        General,
        SpawnRegion,
        Ai,
        SerializedData,
        PaintManager,
        ConsoleCommand,
        DebugMenu,
        CougarManager,
        COUNT

    }

    public enum LogCategoryFlags : uint
    {
        None = 0,
        General = 1 << (int)LogCategory.General,
        SpawnRegion = 1 << (int)LogCategory.SpawnRegion,
        Ai = 1 << (int)LogCategory.Ai,
        SerializedData = 1 << (int)LogCategory.SerializedData,
        PaintManager = 1 << (int)LogCategory.PaintManager,
        ConsoleCommand = 1 << (int)LogCategory.ConsoleCommand,
        DebugMenu = 1 << (int)LogCategory.DebugMenu,
        CougarManager = 1 << (int)LogCategory.CougarManager,
        COUNT = 1 << (int)LogCategory.COUNT
    }
}
