

using UnityEngine;

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseTimberwolf : BaseWolf
    {
        public static BaseTimberwolfSettings TimberwolfSettings = new BaseTimberwolfSettings();
        public BaseTimberwolf(IntPtr ptr) : base(ptr) { }
    }
}
