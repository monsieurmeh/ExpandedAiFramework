

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseDeer : CustomAiBase
    {
        internal static BaseDeerSettings Settings = new BaseDeerSettings();
        public BaseDeer(IntPtr ptr) : base(ptr) { }
    }
}