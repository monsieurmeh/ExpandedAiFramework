

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseDeer : CustomBaseAi
    {
        public static BaseDeerSettings BaseDeerSettings = new BaseDeerSettings();
        public BaseDeer(IntPtr ptr) : base(ptr) { }
    }
}