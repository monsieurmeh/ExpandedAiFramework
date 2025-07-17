

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseBear : CustomBaseAi
    {
        public static BaseBearSettings BaseBearSettings = new BaseBearSettings(Path.Combine(DataFolderPath, $"EAF.Settings.{nameof(BaseBear)}"));
        public BaseBear(IntPtr ptr) : base(ptr) { }
    }
}
