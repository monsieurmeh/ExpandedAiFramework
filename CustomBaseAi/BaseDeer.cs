

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseDeer : CustomBaseAi
    {
        public static BaseDeerSettings BaseDeerSettings;
        public BaseDeer(IntPtr ptr) : base(ptr) { }


        protected override void IncrementKillStat() => StatsManager.IncrementValue(Il2CppTLD.Stats.StatID.StagsKilled);
    }
}