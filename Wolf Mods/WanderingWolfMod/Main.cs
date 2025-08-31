global using Il2Cpp;
global using MelonLoader;
global using ModSettings;
global using static ExpandedAiFramework.Utility;
using MelonLoader.Utils;


[assembly: MelonInfo(typeof(ExpandedAiFramework.WanderingWolfMod.Main), "ExpandedAiFramework.WanderingWolfMod", "1.0.2", "MonsieurMeh", null)]
[assembly: MelonGame("Hinterland", "TheLongDark")]


namespace ExpandedAiFramework.WanderingWolfMod
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
            WanderingWolfManager manager = new WanderingWolfManager();
            EAFManager.Instance.RegisterSubmanager(typeof(WanderingWolf), manager);
            WanderingWolf.WanderingWolfSettings = new WanderingWolfSettings(Path.Combine(DataFolderPath, $"Settings.{nameof(WanderingWolf)}"));
            if (!EAFManager.Instance.RegisterSpawnableAi(typeof(WanderingWolf), WanderingWolf.WanderingWolfSettings))
            {
                Utility.LogError("Could not register WanderingWolf spawning!");
                return false;
            }
            WanderingWolf.WanderingWolfSettings.AddToModSettings(Utility.ModName);
            return true;
        }
    }
}