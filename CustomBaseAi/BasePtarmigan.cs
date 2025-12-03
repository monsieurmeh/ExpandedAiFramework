

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BasePtarmigan : CustomBaseAi
    {
        public static BasePtarmiganSettings BasePtarmiganSettings;
        public BasePtarmigan(IntPtr ptr) : base(ptr) { }

        protected override void IncrementKillStat() => StatsManager.IncrementValue(Il2CppTLD.Stats.StatID.PtarmigansKilled);

    }
}