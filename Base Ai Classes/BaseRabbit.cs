namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseRabbit : CustomAiBase
    {
        public static BaseRabbitSettings Settings = new BaseRabbitSettings();
        public BaseRabbit(IntPtr ptr) : base(ptr) { }
    }
}