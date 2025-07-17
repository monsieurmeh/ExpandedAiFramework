

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseDeer : CustomBaseAi
    {
        public static BaseDeerSettings BaseDeerSettings = new BaseDeerSettings(Path.Combine(DataFolderPath, $"EAF.Settings.{nameof(BaseDeer)}"));
        public BaseDeer(IntPtr ptr) : base(ptr) { }
    }
}