global using Il2Cpp;
global using MelonLoader;
global using ModSettings;
global using static Il2Cpp.BaseAi;
using MelonLoader.Utils;
using System.Reflection;
using UnityEngine;
using System.Resources;
using MelonLoader.TinyJSON;


[assembly: MelonInfo(typeof(ExpandedAiFramework.Main), "ExpandedAiFramework", "0.11.11", "MonsieurMeh", null)]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace ExpandedAiFramework
{
    public class Main : MelonMod
    {
        private const string CurrentVersion = "0.11.11";
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
            LogTrace("OnInitializedScene");
            Manager.OnInitializedScene(sceneName);
        }


        protected bool Initialize()
        {
            string basePath = Path.Combine(MelonEnvironment.ModsDirectory, DataFolderPath);
            Directory.CreateDirectory(basePath);
            bool shouldRefreshEmbeddedData = false;
            string detectedVersion;
            if (!File.Exists(Path.Combine(basePath, "VersionInfo.txt")))
            {
                LoggerInstance.Msg($"Could not detect version info file, making with current version {CurrentVersion}");
                shouldRefreshEmbeddedData = true;
                File.WriteAllText(Path.Combine(basePath, "VersionInfo.txt"), CurrentVersion, System.Text.Encoding.UTF8);
            }
            detectedVersion = File.ReadAllText(Path.Combine(basePath, "VersionInfo.txt"), System.Text.Encoding.UTF8);
            if (detectedVersion != CurrentVersion)
            {
                LoggerInstance.Msg($"Detected version {detectedVersion} is not {CurrentVersion}");
                shouldRefreshEmbeddedData = true;
                File.WriteAllText(Path.Combine(basePath, "VersionInfo.txt"), CurrentVersion, System.Text.Encoding.UTF8);
            }

            RefreshEmbeddedData(basePath, "HidingSpots.Json", shouldRefreshEmbeddedData);
            RefreshEmbeddedData(basePath, "WanderPaths.Json", shouldRefreshEmbeddedData);
            RefreshEmbeddedData(basePath, "assets", shouldRefreshEmbeddedData);
            mManager = EAFManager.Instance;
            mManager?.Initialize(new ExpandedAiFrameworkSettings(Path.Combine(DataFolderPath, $"Settings")));
            return mManager != null;
        }

        private void RefreshEmbeddedData(string basePath, string fileName, bool force) 
        {
            if (force || !File.Exists(Path.Combine(basePath, fileName)))
            {
                EmbeddedResourceExtractor.Extract(fileName, Path.Combine(basePath, fileName));
            }
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