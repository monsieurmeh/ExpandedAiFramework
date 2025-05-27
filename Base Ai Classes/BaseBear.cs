

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseBear : CustomBaseAi
    {
        public static BaseBearSettings BaseBearSettings = new BaseBearSettings();
        public BaseBear(IntPtr ptr) : base(ptr) { }
    }
}
