global using Il2Cpp;
global using MelonLoader;
global using ModSettings;


[assembly: MelonInfo(typeof(ExpandedAiFramework.WanderingWolfMod.Main), "ExpandedAiFramework.WanderingWolfMod", "0.8.0", "MonsieurMeh", null)]
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
            return EAFManager.Instance.RegisterSpawnableAi(typeof(WanderingWolf), WanderingWolf.Settings);
        }
    }
}