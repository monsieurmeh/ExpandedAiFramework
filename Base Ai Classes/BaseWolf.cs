

using UnityEngine;

namespace ExpandedAiFramework
{
    //todo's:
    // No attacking bears, moose or cougars! RUN when you see them!

    [RegisterTypeInIl2Cpp]
    public class BaseWolf : CustomBaseAi
    {
        public static BaseWolfSettings BaseWolfSettings = new BaseWolfSettings(Path.Combine(DataFolderPath, $"EAF.Settings.{nameof(BaseTimberwolf)}"));
        public BaseWolf(IntPtr ptr) : base(ptr) { }

        protected override bool ProcessCustom() 
        {
            if (CurrentMode == AiMode.Stalking && mBaseAi.m_TimeInModeSeconds >= BaseWolfSettings.StalkingTimeout)
            {
                SetAiMode(AiMode.Attack);
                return false;
            }
            return true;
        }


        protected override bool ChangeModeWhenTargetDetectedCustom()
        {
            if (CurrentTarget.IsBear() || CurrentTarget.IsCougar() || CurrentTarget.IsBear())
            {
                LogVerbose($"Wolves run from larger threats!");
                SetAiMode(AiMode.Flee);
                return false;
            }
            return true;
        }
    }
}
