

using Il2CppRewired;

namespace ExpandedAiFramework
{
    //I am going to create a lot of these to keep things cleaner, better to have a central place to do dependency injection for common stuff
    public abstract class BaseSubManager : ISubManager
    {
        protected EAFManager mManager;
        protected TimeOfDay mTimeOfDay;

        public EAFManager Manager { get { return mManager; } }

        public BaseSubManager(EAFManager manager)
        {
            Initialize(manager);
        }

        public virtual void Initialize(EAFManager manager)
        {
            mManager = manager;
        }

        public virtual void Shutdown() { }
        public virtual void OnStartNewGame() { OnLoadScene(mManager.CurrentScene); }
        public virtual void OnLoadScene(string sceneName) { }
        public virtual void OnInitializedScene(string sceneName) { }
        public virtual void OnSaveGame() { }
        public virtual void OnLoadGame() { }
        public virtual void UpdateFromManager() { }
        public virtual void OnQuitToMainMenu() { }
    }
}
