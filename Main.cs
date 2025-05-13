global using Il2Cpp;
global using MelonLoader;
global using ModSettings;
global using static Il2Cpp.BaseAi;


[assembly: MelonInfo(typeof(ExpandedAiFramework.Main), "ExpandedAiFramework", "0.8.0", "MonsieurMeh", null)]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace ExpandedAiFramework
{
    public class Main : MelonMod
    {
        protected Manager mManager;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg(Initialize() ? "Initialized Successfully!" : "Initialization Errors!");
        }

        public override void OnDeinitializeMelon()
        {
            LoggerInstance.Msg(Shutdown() ? "Shutdown Successfully!" : "Shutdown Errors!");
        }


        protected bool Initialize()
        {
            mManager = Manager.Instance;
            mManager?.Initialize(new ExpandedAiFrameworkSettings());
            return mManager != null;
        }


        public override void OnUpdate()
        {
            mManager.Update();
        }


        protected bool Shutdown()
        {
            mManager?.Shutdown();
            return true;
        }
    }
}