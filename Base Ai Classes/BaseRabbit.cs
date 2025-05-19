namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseRabbit : CustomAiBase
    {
        internal static BaseRabbitSettings Settings = new BaseRabbitSettings();
        public BaseRabbit(IntPtr ptr) : base(ptr) { }
    }
}