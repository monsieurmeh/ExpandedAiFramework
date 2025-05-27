

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseMoose : CustomBaseAi
    {
        public static BaseMooseSettings BaseMooseSettings = new BaseMooseSettings();
        public BaseMoose(IntPtr ptr) : base(ptr) { }
    }
}