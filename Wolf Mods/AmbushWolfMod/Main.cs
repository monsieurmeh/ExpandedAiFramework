global using Il2Cpp;
global using MelonLoader;
global using ModSettings;


[assembly: MelonInfo(typeof(ExpandedAiFramework.AmbushWolfMod.Main), "ExpandedAiFramework.AmbushWolfMod", "0.8.0", "MonsieurMeh", null)]
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
            return EAFManager.Instance.RegisterSpawnableAi(typeof(AmbushWolf), AmbushWolf.Settings);
        }
    }
}