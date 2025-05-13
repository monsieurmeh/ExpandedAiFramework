using Il2CppNodeCanvas.Tasks.Actions;
using System.Reflection;

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
            AddToModSettings(Manager.ModName);
            RefreshGUI();
        }
    }
}