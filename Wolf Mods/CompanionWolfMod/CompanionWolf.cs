

namespace ExpandedAiFramework.CompanionWolfMod
{
    [RegisterTypeInIl2Cpp]
    public class CompanionWolf : BaseWolf
    {
        internal static CompanionWolfSettings Settings = new CompanionWolfSettings();

        protected bool mTamed = false;

        public CompanionWolf(IntPtr ptr) : base(ptr) { }

        protected override bool PreprocesSetAiModeCustom(AiMode mode, out AiMode newMode)
        {
            if (!mTamed && mode.ToFlag().AnyOf(AiModeFlags.UntamedCompanionWolfIgnoreModes))
            {
                newMode = AiMode.Flee;
                return false;
            }
            newMode = mode;
            return true;
        }


        protected override bool ProcessCustom()
        {
            switch (CurrentMode)
            {
                case (AiMode)AiModeEAF.BigCarry:
                case (AiMode)AiModeEAF.FollowLeader:
                case (AiMode)AiModeEAF.Fetch:
                    return false;
                default:
                    return true;
            }
        }
    }
}
