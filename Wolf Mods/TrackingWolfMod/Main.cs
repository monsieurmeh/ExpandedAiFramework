global using Il2Cpp;
global using MelonLoader;
global using ModSettings;
global using static ExpandedAiFramework.Utility;
using MelonLoader.Utils;


[assembly: MelonInfo(typeof(ExpandedAiFramework.TrackingWolfMod.Main), "ExpandedAiFramework.TrackingWolfMod", "0.12.3", "MonsieurMeh", null)]
[assembly: MelonGame("Hinterland", "TheLongDark")]


namespace ExpandedAiFramework.TrackingWolfMod
{
    public class Main : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg(Initialize() ? "Initialized Successfully!" : "Initialization Errors!");
        }

        protected bool Initialize()
        {
            Directory.CreateDirectory(Path.Combine(MelonEnvironment.ModsDirectory, DataFolderPath));
            TrackingWolfManager manager = new TrackingWolfManager();
            EAFManager.Instance.RegisterSpawnManager(manager);
            TrackingWolf.TrackingWolfSettings = new TrackingWolfSettings(Path.Combine(DataFolderPath, $"{nameof(TrackingWolf)}"));
            if (!EAFManager.Instance.RegisterSpawnableAi(typeof(TrackingWolf), TrackingWolf.TrackingWolfSettings))
            {
                Error("Could not register TrackingWolf spawning!");
                return false;
            }
            TrackingWolf.TrackingWolfSettings.AddToModSettings(Utility.ModName);
            return true;
        }
    }
}