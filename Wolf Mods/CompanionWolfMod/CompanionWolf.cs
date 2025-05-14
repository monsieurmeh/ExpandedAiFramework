using UnityEngine;


namespace ExpandedAiFramework.CompanionWolfMod
{
    [RegisterTypeInIl2Cpp]
    public class CompanionWolf : BaseWolf
    {
        private enum CompanionWolfAiMode : int
        {
            Follow = (int)AiMode.Disabled + 1,
            Fetch,
            BigCarry,
            COUNT
        }

        private const AiModeFlags UntamedCompanionWolfIgnoreModes = AiModeFlags.Attack
                                                                | AiModeFlags.Stalking
                                                                | AiModeFlags.PassingAttack
                                                                | AiModeFlags.Struggle
                                                                | AiModeFlags.HoldGround;


        internal static CompanionWolfSettings Settings;

        protected CompanionWolfManager mSubManager;
        protected bool mTamed = false;

        public CompanionWolf(IntPtr ptr) : base(ptr) { }


        public override void Initialize(BaseAi ai, TimeOfDay timeOfDay)//, EAFManager manager)
        {
            base.Initialize(ai, timeOfDay);//, manager);
            if (!mManager.SubManagers.TryGetValue(GetType(), out ISubManager subManager))
            {
                LogError("Could not fetch submanager for CompanionWolf!");
                return;
            }
            CompanionWolfManager companionWolfManager = subManager as CompanionWolfManager;
            if (companionWolfManager == null)
            {
                LogError("Fetched submanager for CompanionWolf is NOT a CompanionWolfManager, type mismatch!");
                return;
            }
            mSubManager = companionWolfManager;
            mBaseAi.m_MaxHP = Settings.MaximumCondition;
            mBaseAi.m_CurrentHP = companionWolfManager.Data.CurrentCondition;
            mBaseAi.transform.localScale = new Vector3(2, 2, 2);
        }


        protected override bool PreprocesSetAiModeCustom(AiMode mode, out AiMode newMode)
        {
            if (!mTamed && mode.ToFlag().AnyOf(UntamedCompanionWolfIgnoreModes))
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
                case (AiMode)CompanionWolfAiMode.BigCarry:
                case (AiMode)CompanionWolfAiMode.Follow:
                case (AiMode)CompanionWolfAiMode.Fetch:
                    return false;
                default:
                    return true;
            }
        }
    }
}
