

using UnityEngine;

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseTimberwolf : BaseWolf
    {
        public static BaseTimberwolfSettings BaseTimberwolfSettings;
        public BaseTimberwolf(IntPtr ptr) : base(ptr) { }

        public override void Initialize(BaseAi ai, TimeOfDay timeOfDay, SpawnRegion spawnRegion, SpawnModDataProxy proxy)
        {
            base.Initialize(ai, timeOfDay, spawnRegion, proxy);
            if (CurrentMode != AiMode.None) return;
            SetAiMode(AiMode.Wander);
        }
    }
}
