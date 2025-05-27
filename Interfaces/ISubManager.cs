using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        void Update();
        void OnQuitToMainMenu();
        bool ShouldInterceptSpawn(BaseAi baseAi, SpawnRegion region);
        Type SpawnType { get; }
    }
}
