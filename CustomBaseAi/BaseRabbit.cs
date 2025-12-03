namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseRabbit : CustomBaseAi
    {
        public static BaseRabbitSettings BaseRabbitSettings;
        public BaseRabbit(IntPtr ptr) : base(ptr) { }


        protected override void IncrementKillStat() => StatsManager.IncrementValue(Il2CppTLD.Stats.StatID.RabbitsKilled);
    }
}