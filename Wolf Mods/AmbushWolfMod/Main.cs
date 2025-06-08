global using Il2Cpp;
global using MelonLoader;
global using ModSettings;
using ExpandedAiFramework.WanderingWolfMod;


[assembly: MelonInfo(typeof(ExpandedAiFramework.AmbushWolfMod.Main), "ExpandedAiFramework.AmbushWolfMod", "1.0.1", "MonsieurMeh", null)]
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
            AmbushWolfManager manager = new AmbushWolfManager();
            manager.Initialize(EAFManager.Instance);
            EAFManager.Instance.RegisterSubmanager(typeof(AmbushWolf), manager);
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