

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BasePtarmigan : CustomBaseAi
    {
        public static BasePtarmiganSettings BasePtarmiganSettings = new BasePtarmiganSettings();
        public BasePtarmigan(IntPtr ptr) : base(ptr) { }
    }
}