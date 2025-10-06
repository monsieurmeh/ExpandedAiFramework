

namespace ExpandedAiFramework
{
    public interface ISubManager
    {
        void Initialize(EAFManager manager);
        void Shutdown();
        void OnStartNewGame();
        void OnLoadScene(string sceneName);
        void OnInitializedScene(string sceneName);
        void OnSaveGame();
        void OnLoadGame();
        void UpdateFromManager();
        void OnQuitToMainMenu();
    }
}
