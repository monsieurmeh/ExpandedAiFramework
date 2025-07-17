

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BasePtarmigan : CustomBaseAi
    {
        public static BasePtarmiganSettings BasePtarmiganSettings = new BasePtarmiganSettings(Path.Combine(DataFolderPath, $"EAF.Settings.{nameof(BasePtarmigan)}"));
        public BasePtarmigan(IntPtr ptr) : base(ptr) { }
    }
}