namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseRabbit : CustomBaseAi
    {
        public static BaseRabbitSettings BaseRabbitSettings = new BaseRabbitSettings();
        public BaseRabbit(IntPtr ptr) : base(ptr) { }
    }
}