

using UnityEngine;

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseTimberwolf : BaseWolf
    {
        public static BaseTimberwolfSettings BaseTimberwolfSettings = new BaseTimberwolfSettings(Path.Combine(DataFolderPath, $"EAF.Settings.{nameof(BaseTimberwolf)}"));
        public BaseTimberwolf(IntPtr ptr) : base(ptr) { }
    }
}
