using UnityEngine;


namespace ExpandedAiFramework.BigWolfMod
{
    [RegisterTypeInIl2Cpp]
    public class BigWolf : BaseWolf
    {
        internal static BigWolfSettings Settings = new BigWolfSettings();


        public BigWolf(IntPtr ptr) : base(ptr) { }


        public override void Initialize(BaseAi ai, TimeOfDay timeOfDay, SpawnRegion spawnRegion)//, EAFManager manager)
        {
            base.Initialize(ai, timeOfDay, spawnRegion);//, manager);
            mBaseAi.m_RunSpeed *= 8;
            mBaseAi.m_StalkSpeed *= 8;
            mBaseAi.m_WalkSpeed *= 8;
            mBaseAi.m_turnSpeed *= 8;
            mBaseAi.m_TurnSpeedDegreesPerSecond *= 8;
            mBaseAi.m_MaxHP *= 4;
            mBaseAi.m_CurrentHP *= 4;
            mBaseAi.transform.localScale = new Vector3(2, 2, 2);
        }


        protected override bool CanBleedOutCustom(out bool canBleedOut)
        {
            canBleedOut = Settings.CanBleedOut;
            return false;
        }
    }
}
