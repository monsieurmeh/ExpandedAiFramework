namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseRabbit : CustomBaseAi
    {
        public static BaseRabbitSettings BaseRabbitSettings = new BaseRabbitSettings(Path.Combine(DataFolderPath, $"EAF.Settings.{nameof(BaseRabbit)}"));
        public BaseRabbit(IntPtr ptr) : base(ptr) { }
    }
}