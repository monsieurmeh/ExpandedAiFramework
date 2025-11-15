global using Il2Cpp;
global using MelonLoader;
global using ModSettings;
global using static ExpandedAiFramework.Utility;
using MelonLoader.Utils;


[assembly: MelonInfo(typeof(ExpandedAiFramework.BigWolfMod.Main), "ExpandedAiFramework.BigWolfMod", "0.11.13", "MonsieurMeh", null)]
[assembly: MelonGame("Hinterland", "TheLongDark")]


namespace ExpandedAiFramework.BigWolfMod
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
            BigWolf.BigWolfSettings = new BigWolfSettings(Path.Combine(DataFolderPath, $"{nameof(BigWolf)}"));
            if (!EAFManager.Instance.RegisterSpawnableAi(typeof(BigWolf), BigWolf.BigWolfSettings))
            {
                Utility.LogError("Could not register BigWolf spawning!");
                return false;
            }
            BigWolf.BigWolfSettings.AddToModSettings(Utility.ModName);
            return true;
        }
    }
}