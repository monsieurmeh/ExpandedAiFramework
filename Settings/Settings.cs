

namespace ExpandedAiFramework
{
    public class ExpandedAiFrameworkSettings : JsonModSettings
    {
        [Name("Max Force Spawns")]
        [Slider(1, 30)]
        [Description("Adjusts number of force-spawnable entities allowed at once. Combined with high spawn weights for force-spawned types like TrackingWolf, this can quickly get overwhelming! Recommended limit: 10!")]
        public int MaxForceSpawns = 10;


        public ExpandedAiFrameworkSettings(string path) : base(path)
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