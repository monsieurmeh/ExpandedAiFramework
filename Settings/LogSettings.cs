using System.Reflection;

namespace ExpandedAiFramework
{
    public class LogSettings : BaseNestedSettings
    {
        [Section("Advanced Logging")]
        [Name("Enable Advanced Logging")]
        [Description("Enable advenced logging features. This should only be used for troubleshooting and debugging.")]
        public bool Enable = false;

        [Name("AiManager")]
        public bool EnableAiManagerLogging = false;

        [Name("Ai")]
        public bool EnableAiLogging = false;

        [Name("SpawnRegionManager")]
        public bool EnableSpawnRegionManagerLogging = false;

        [Name("SpawnRegion")]
        public bool EnableSpawnRegionLogging = false;

        [Name("SerializedData")]
        public bool EnableSerializedDataLogging = false;

        [Name("PaintManager")]
        public bool EnablePaintManagerLogging = false;

        [Name("ConsoleCommand")]
        public bool EnableConsoleCommandLogging = false;

        [Name("DebugMenu")]
        public bool EnableDebugMenuLogging = false;

        [Name("CougarManager")]
        public bool EnableCougarManagerLogging = false;

        [Name("PackManager")]
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