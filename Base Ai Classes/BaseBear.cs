

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseBear : CustomAiBase
    {
        internal static BaseBearSettings Settings = new BaseBearSettings();
        public BaseBear(IntPtr ptr) : base(ptr) { }
    }
}
