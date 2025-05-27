

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseCougar : CustomBaseAi
    {
        public static BaseCougarSettings BaseCougarSettings = new BaseCougarSettings();
        public BaseCougar(IntPtr ptr) : base(ptr) { }
    }
}