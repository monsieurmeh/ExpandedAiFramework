

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseMoose : CustomAiBase
    {
        internal static BaseMooseSettings Settings = new BaseMooseSettings();
        public BaseMoose(IntPtr ptr) : base(ptr) { }
    }
}