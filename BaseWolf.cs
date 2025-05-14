

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseWolf : CustomAiBase
    {
        internal static BaseWolfSettings Settings = new BaseWolfSettings();
        public BaseWolf(IntPtr ptr) : base(ptr) { }

        protected override float m_HoldGroundDistanceFromSpear { get { return 3f; } }
     
        protected override float m_HoldGroundOuterDistanceFromSpear { get { return 5f; } }
    }
}
