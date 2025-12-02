

using UnityEngine;

namespace ExpandedAiFramework
{
    //todo's:
    // No attacking bears, moose or cougars! RUN when you see them!

    [RegisterTypeInIl2Cpp]
    public class BaseWolf : CustomBaseAi
    {
        public static BaseWolfSettings BaseWolfSettings;
        public BaseWolf(IntPtr ptr) : base(ptr) { }

        protected override bool ProcessCustom() 
        {
            if (ShouldActivateStalkingTimeout())
            {
                SetAiMode(AiMode.Attack);
                return false;
            }
            return true;
        }

        private bool ShouldActivateStalkingTimeout()
        {
            if (CurrentMode != AiMode.Stalking) return false;
            if (!BaseWolfSettings.EnableStalkingTimeout) return false;
            if (mBaseAi.m_TimeInModeSeconds < BaseWolfSettings.StalkingTimeout) return false;
            this.LogTraceInstanced($"Stalking timeout activated! GETTEM, BOY!", LogCategoryFlags.Ai);
            return true;
        }


        protected override bool ChangeModeWhenTargetDetectedCustom()
        {
            if (CurrentTarget.IsBear() || CurrentTarget.IsCougar() || CurrentTarget.IsBear())
            {
                this.LogTraceInstanced($"Wolves run from larger threats!", LogCategoryFlags.Ai);
                SetAiMode(AiMode.Flee);
                return false;
            }
            return true;
        }
    }
}
