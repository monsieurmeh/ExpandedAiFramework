global using Il2Cpp;
global using MelonLoader;
global using ModSettings;
global using static ExpandedAiFramework.Utility;
using MelonLoader.Utils;


[assembly: MelonInfo(typeof(ExpandedAiFramework.CompanionWolfMod.Main), "ExpandedAiFramework.CompanionWolfMod", "0.10.1", "MonsieurMeh", null)]
[assembly: MelonGame("Hinterland", "TheLongDark")]


namespace ExpandedAiFramework.CompanionWolfMod
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
            EAFManager.Instance.LoadData("CompanionWolfMod");
            CompanionWolfManager manager = new CompanionWolfManager();
            CompanionWolf.CompanionWolfSettings = new CompanionWolfSettings(manager, Path.Combine(DataFolderPath, $"{nameof(CompanionWolf)}"));
            EAFManager.Instance.RegisterSubmanager(typeof(CompanionWolf), manager);
            if (!EAFManager.Instance.RegisterSpawnableAi(typeof(CompanionWolf), CompanionWolf.CompanionWolfSettings))
            {
                Utility.LogError("Could not register CompanionWolf spawning!");
                return false;
            }
            CompanionWolf.CompanionWolfSettings.AddToModSettings(Utility.ModName);
            return true;
        }
    }
}