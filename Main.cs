global using Il2Cpp;
global using MelonLoader;
global using ModSettings;
global using static Il2Cpp.BaseAi;
using System.Reflection;


[assembly: MelonInfo(typeof(ExpandedAiFramework.Main), "ExpandedAiFramework", "0.10.2", "MonsieurMeh", null)]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace ExpandedAiFramework
{
    public class Main : MelonMod
    {
        protected EAFManager mManager;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg(Initialize() ? "Initialized Successfully!" : "Initialization Errors!");
        }


        public override void OnDeinitializeMelon()
        {
            LoggerInstance.Msg(Shutdown() ? "Shutdown Successfully!" : "Shutdown Errors!");
        }


        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            LogDebug("OnInitializedScene");
            Manager.OnInitializedScene();
        }
       

        protected bool Initialize()
        {
            mManager = EAFManager.Instance;
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