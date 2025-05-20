

using UnityEngine;

namespace ExpandedAiFramework
{
    //todo's:
    // No attacking bears, moose or cougars! RUN when you see them!

    [RegisterTypeInIl2Cpp]
    public class BaseWolf : CustomAiBase
    {
        public static BaseWolfSettings Settings = new BaseWolfSettings();
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
    }
}
