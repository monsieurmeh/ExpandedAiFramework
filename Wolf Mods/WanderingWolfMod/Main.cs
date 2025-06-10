global using Il2Cpp;
global using MelonLoader;
global using ModSettings;
global using static ExpandedAiFramework.Utility;


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
            WanderingWolfManager manager = new WanderingWolfManager();
            manager.Initialize(EAFManager.Instance);
            EAFManager.Instance.RegisterSubmanager(typeof(WanderingWolf), manager);
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