

using UnityEngine;

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseWolf : CustomAiBase
    {
        internal static BaseWolfSettings Settings = new BaseWolfSettings();
        public BaseWolf(IntPtr ptr) : base(ptr) { }

        protected override bool ProcessCustom()
        {
            if (CurrentMode == AiMode.Stalking && mBaseAi.m_TimeInModeSeconds >= Settings.StalkingTimeout)
            {
                SetAiMode(AiMode.Attack);
                return false;
            }
            return true;
        }

        protected override float m_HoldGroundDistanceFromSpear { get { return 3f; } }
     
        protected override float m_HoldGroundOuterDistanceFromSpear { get { return 5f; } }
    }
}
