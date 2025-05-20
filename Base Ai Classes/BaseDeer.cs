

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseDeer : CustomAiBase
    {
        public static BaseDeerSettings BaseDeerSettings = new BaseDeerSettings();
        public BaseDeer(IntPtr ptr) : base(ptr) { }
    }
}