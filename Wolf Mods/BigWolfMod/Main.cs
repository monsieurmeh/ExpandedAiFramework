global using Il2Cpp;
global using MelonLoader;
global using ModSettings;


[assembly: MelonInfo(typeof(ExpandedAiFramework.BigWolfMod.Main), "ExpandedAiFramework.BigWolfMod", "1.0.1", "MonsieurMeh", null)]
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