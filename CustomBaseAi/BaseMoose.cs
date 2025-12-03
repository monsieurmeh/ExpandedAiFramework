

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseMoose : CustomBaseAi
    {
        public static BaseMooseSettings BaseMooseSettings;
        public BaseMoose(IntPtr ptr) : base(ptr) { }


        protected override void IncrementKillStat() => StatsManager.IncrementValue(Il2CppTLD.Stats.StatID.MooseKilled);
    }
}