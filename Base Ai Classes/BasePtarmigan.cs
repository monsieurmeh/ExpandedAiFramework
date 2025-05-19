

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BasePtarmigan : CustomAiBase
    {
        internal static BasePtarmiganSettings Settings = new BasePtarmiganSettings();
        public BasePtarmigan(IntPtr ptr) : base(ptr) { }
    }
}