global using Il2Cpp;
global using MelonLoader;
global using ModSettings;


[assembly: MelonInfo(typeof(ExpandedAiFramework.CompanionWolfMod.Main), "ExpandedAiFramework.CompanionWolfMod", "0.8.0", "MonsieurMeh", null)]
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
            EAFManager.Instance.ModData.Load("CompanionWolfMod");
            CompanionWolfManager manager = new CompanionWolfManager();
            manager.Initialize(EAFManager.Instance);
            CompanionWolf.Settings = new CompanionWolfSettings(manager);
            EAFManager.Instance.RegisterSubmanager(typeof(CompanionWolf), manager);
            return EAFManager.Instance.RegisterSpawnableAi(typeof(CompanionWolf), CompanionWolf.Settings);
        }
    }
}