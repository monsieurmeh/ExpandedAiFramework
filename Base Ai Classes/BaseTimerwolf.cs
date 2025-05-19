

using UnityEngine;

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseTimberwolf : BaseWolf
    {
        new internal static BaseTimberwolfSettings Settings = new BaseTimberwolfSettings();
        public BaseTimberwolf(IntPtr ptr) : base(ptr) { }
    }
}
