

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseDeer : CustomAiBase
    {
        public static BaseDeerSettings Settings = new BaseDeerSettings();
        public BaseDeer(IntPtr ptr) : base(ptr) { }
    }
}