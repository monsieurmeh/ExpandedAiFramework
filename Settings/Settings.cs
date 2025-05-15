

namespace ExpandedAiFramework
{
    public class ExpandedAiFrameworkSettings : JsonModSettings
    {
        public ExpandedAiFrameworkSettings()
        {
            Initialize();
        }


        protected void Initialize()
        {
            AddToModSettings(ModName);
            RefreshGUI();
        }
    }
}