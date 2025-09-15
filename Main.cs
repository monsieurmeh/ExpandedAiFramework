global using Il2Cpp;
global using MelonLoader;
global using ModSettings;
global using static Il2Cpp.BaseAi;
using MelonLoader.Utils;
using System.Reflection;
using UnityEngine;
using System.Resources;


[assembly: MelonInfo(typeof(ExpandedAiFramework.Main), "ExpandedAiFramework", "0.11.1", "MonsieurMeh", null)]
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
            LogVerbose("OnInitializedScene");
            Manager.OnInitializedScene(sceneName);
        }


        protected bool Initialize()
        {
            Directory.CreateDirectory(Path.Combine(MelonEnvironment.ModsDirectory, DataFolderPath));
            if (!File.Exists(Path.Combine(MelonEnvironment.ModsDirectory, "EAF/HidingSpots.json")))
            {
                EmbeddedResourceExtractor.Extract("HidingSpots.Json", Path.Combine(MelonEnvironment.ModsDirectory, "EAF/HidingSpots.json"));
            }
            if (!File.Exists(Path.Combine(MelonEnvironment.ModsDirectory, "EAF/WanderPaths.json")))
            {
                EmbeddedResourceExtractor.Extract("WanderPaths.Json", Path.Combine(MelonEnvironment.ModsDirectory, "EAF/WanderPaths.json"));
            }
            mManager = EAFManager.Instance;
            mManager?.Initialize(new ExpandedAiFrameworkSettings(Path.Combine(DataFolderPath, $"Settings")));
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