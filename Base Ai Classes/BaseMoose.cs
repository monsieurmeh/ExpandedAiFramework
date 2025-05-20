

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseMoose : CustomAiBase
    {
        public static BaseMooseSettings BaseMooseSettings = new BaseMooseSettings();
        public BaseMoose(IntPtr ptr) : base(ptr) { }
    }
}