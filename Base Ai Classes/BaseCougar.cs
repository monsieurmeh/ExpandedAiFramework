

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseCougar : CustomAiBase
    {
        public static BaseCougarSettings Settings = new BaseCougarSettings();
        public BaseCougar(IntPtr ptr) : base(ptr) { }
    }
}