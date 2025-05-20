

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseBear : CustomAiBase
    {
        public static BaseBearSettings BaseBearSettings = new BaseBearSettings();
        public BaseBear(IntPtr ptr) : base(ptr) { }
    }
}
