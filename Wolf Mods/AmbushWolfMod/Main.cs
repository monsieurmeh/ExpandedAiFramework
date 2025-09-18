global using Il2Cpp;
global using MelonLoader;
global using ModSettings;
global using static ExpandedAiFramework.Utility;
using ExpandedAiFramework.WanderingWolfMod;
using MelonLoader.Utils;


[assembly: MelonInfo(typeof(ExpandedAiFramework.AmbushWolfMod.Main), "ExpandedAiFramework.AmbushWolfMod", "0.11.4", "MonsieurMeh", null)]
[assembly: MelonGame("Hinterland", "TheLongDark")]


namespace ExpandedAiFramework.AmbushWolfMod
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
            AmbushWolfManager manager = new AmbushWolfManager();
            EAFManager.Instance.RegisterSubmanager(typeof(AmbushWolf), manager);
            AmbushWolf.AmbushWolfSettings = new AmbushWolfSettings(Path.Combine(DataFolderPath, $"{nameof(AmbushWolf)}"));
            if (!EAFManager.Instance.RegisterSpawnableAi(typeof(AmbushWolf), AmbushWolf.AmbushWolfSettings))
            {
                Utility.LogError("Could not register AmbushWolf spawning!");
                return false;
            }
            AmbushWolf.AmbushWolfSettings.AddToModSettings(Utility.ModName);
            return true;
        }        
    }
}