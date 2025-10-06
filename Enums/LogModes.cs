

namespace ExpandedAiFramework
{
    public enum LogCategory : int 
    {
        None = 0,
        General,
        SpawnRegion,
        SpawnRegionManager,
        Ai,
        AiManager,
        SerializedData,
        PaintManager,
        ConsoleCommand,
        DebugMenu,
        CougarManager,
        PackManager,
        Request,
        System,
        COUNT

    }

    public enum LogCategoryFlags : uint
    {
        None = 0,
        General = 1 << (int)LogCategory.General,
        SpawnRegion = 1 << (int)LogCategory.SpawnRegion,
        SpawnRegionManager = 1 << (int)LogCategory.SpawnRegionManager,
        Ai = 1 << (int)LogCategory.Ai,
        AiManager = 1 << (int)LogCategory.AiManager,
        SerializedData = 1 << (int)LogCategory.SerializedData,
        PaintManager = 1 << (int)LogCategory.PaintManager,
        ConsoleCommand = 1 << (int)LogCategory.ConsoleCommand,
        DebugMenu = 1 << (int)LogCategory.DebugMenu,
        CougarManager = 1 << (int)LogCategory.CougarManager,
        PackManager = 1 << (int)LogCategory.PackManager,
        Request = 1 << (int)LogCategory.Request,
        System = 1 << (int)LogCategory.System,
    }
}
