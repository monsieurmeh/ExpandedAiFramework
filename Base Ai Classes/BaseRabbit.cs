namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseRabbit : CustomAiBase
    {
        public static BaseRabbitSettings BaseRabbitSettings = new BaseRabbitSettings();
        public BaseRabbit(IntPtr ptr) : base(ptr) { }
    }
}