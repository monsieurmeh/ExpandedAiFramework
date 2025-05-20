

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseBear : CustomAiBase
    {
        public static BaseBearSettings Settings = new BaseBearSettings();
        public BaseBear(IntPtr ptr) : base(ptr) { }
    }
}
