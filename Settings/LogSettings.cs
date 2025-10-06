using System.Reflection;

namespace ExpandedAiFramework
{
    public class LogSettings : BaseNestedSettings
    {
        [Section("Advanced Logging")]
        [Name("Enable Advanced Logging")]
        [Description("Enable advenced logging features. This should only be used for troubleshooting and debugging.")]
        public bool Enable = false;

        [Name("AiManager Logging")]
        [Description("Enable AiManager logging.")]
        public bool EnableAiManagerLogging = false;

        [Name("Ai Logging")]
        [Description("Enable Ai logging.")]
        public bool EnableAiLogging = false;

        [Name("SpawnRegionManager Logging")]
        [Description("Enable SpawnRegionManager logging.")]
        public bool EnableSpawnRegionManagerLogging = false;

        [Name("SpawnRegion Logging")]
        [Description("Enable SpawnRegion logging.")]
        public bool EnableSpawnRegionLogging = false;

        [Name("SerializedData Logging")]
        [Description("Enable SerializedData logging.")]
        public bool EnableSerializedDataLogging = false;

        [Name("PaintManager Logging")]
        [Description("Enable PaintManager logging.")]
        public bool EnablePaintManagerLogging = false;

        [Name("ConsoleCommand Logging")]
        [Description("Enable ConsoleCommand logging.")]
        public bool EnableConsoleCommandLogging = false;

        [Name("DebugMenu Logging")]
        [Description("Enable DebugMenu logging.")]
        public bool EnableDebugMenuLogging = false;

        [Name("CougarManager Logging")]
        [Description("Enable CougarManager logging.")]
        public bool EnableCougarManagerLogging = false;

        [Name("PackManager Logging")]
        [Description("Enable PackManager logging.")]
        public bool EnablePackManagerLogging = false;


        public LogSettings(string path) : base(path) { }

        public LogCategoryFlags GetFlags()
        {
            LogCategoryFlags flags = LogCategoryFlags.General;
            if (EnableAiLogging) flags |= LogCategoryFlags.Ai; 
            if (EnableAiManagerLogging) flags |= LogCategoryFlags.AiManager;
            if (EnableSpawnRegionLogging) flags |= LogCategoryFlags.SpawnRegion;
            if (EnableSpawnRegionManagerLogging) flags |= LogCategoryFlags.SpawnRegionManager;
            if (EnableSerializedDataLogging) flags |= LogCategoryFlags.SerializedData;
            if (EnablePaintManagerLogging) flags |= LogCategoryFlags.PaintManager;
            if (EnableConsoleCommandLogging) flags |= LogCategoryFlags.ConsoleCommand;
            if (EnableDebugMenuLogging) flags |= LogCategoryFlags.DebugMenu;
            if (EnableCougarManagerLogging) flags |= LogCategoryFlags.CougarManager;
            if (EnablePackManagerLogging) flags |= LogCategoryFlags.PackManager;
            return flags;
        }

        protected override void OnChange(FieldInfo field, object oldValue, object newValue)
        {
            base.OnChange(field, oldValue, newValue);
            EAFManager.Instance.LogCategoryFlags = GetFlags();
        }
    }
}