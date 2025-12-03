using UnityEngine;

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseBear : CustomBaseAi
    {
        public static BaseBearSettings BaseBearSettings;
        public BaseBear(IntPtr ptr) : base(ptr) { }

        public override void Initialize(BaseAi ai, TimeOfDay timeOfDay, SpawnRegion spawnRegion, SpawnModDataProxy proxy)
        {
            base.Initialize(ai, timeOfDay, spawnRegion, proxy);
            if (!proxy.Fresh) return;
            if (spawnRegion.m_Den == null) return;
            if (!AiUtils.GetClosestNavmeshPos(out Vector3 denPos, spawnRegion.m_Den.transform.position, spawnRegion.m_Den.transform.position)) return;
            mBaseAi.m_MoveAgent.transform.position = denPos;
            mBaseAi.m_MoveAgent.Warp(denPos, 2.0f, true, -1);
        }

        protected override void IncrementKillStat() => StatsManager.IncrementValue(Il2CppTLD.Stats.StatID.BearsKilled);
    }
}
