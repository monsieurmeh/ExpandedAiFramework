using Il2Cpp;
using UnityEngine;

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseBear : CustomBaseAi
    {
        public static BaseBearSettings BaseBearSettings;
        public BaseBear(IntPtr ptr) : base(ptr) { }

        protected override void IncrementKillStat() => StatsManager.IncrementValue(Il2CppTLD.Stats.StatID.BearsKilled);
    }
}
