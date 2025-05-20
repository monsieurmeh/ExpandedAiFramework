

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BasePtarmigan : CustomAiBase
    {
        public static BasePtarmiganSettings Settings = new BasePtarmiganSettings();
        public BasePtarmigan(IntPtr ptr) : base(ptr) { }
    }
}