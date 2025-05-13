using UnityEngine;


namespace ExpandedAiFramework.BigWolfMod
{
    [RegisterTypeInIl2Cpp]
    public class BigWolf : BaseWolf
    {
        internal static BigWolfSettings Settings = new BigWolfSettings();


        public BigWolf(IntPtr ptr) : base(ptr) { }


        public override void Augment()
        {
            if (mBaseAi.gameObject?.TryGetComponent(out SkinnedMeshRenderer renderer) ?? false)
            {
                renderer.sharedMaterial.color = Color.red;
            }
            mBaseAi.m_RunSpeed *= 8;
            mBaseAi.m_StalkSpeed *= 8;
            mBaseAi.m_WalkSpeed *= 8;
            mBaseAi.m_turnSpeed *= 8;
            mBaseAi.m_TurnSpeedDegreesPerSecond *= 8;
            mBaseAi.m_MaxHP *= 4;
            mBaseAi.m_CurrentHP *= 4;
            Vector3 newScale = new Vector3(2, 2, 2);
            mBaseAi.gameObject.transform.set_localScale_Injected(ref newScale);
            base.Augment();
        }


        protected override bool CanBleedOutCustom(out bool canBleedOut)
        {
            canBleedOut = Settings.CanBleedOut;
            return false;
        }
    }
}
