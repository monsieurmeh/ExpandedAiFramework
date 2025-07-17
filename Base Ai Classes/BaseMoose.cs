

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseMoose : CustomBaseAi
    {
        public static BaseMooseSettings BaseMooseSettings = new BaseMooseSettings(Path.Combine(DataFolderPath, $"EAF.Settings.{nameof(BaseMoose)}"));
        public BaseMoose(IntPtr ptr) : base(ptr) { }
    }
}