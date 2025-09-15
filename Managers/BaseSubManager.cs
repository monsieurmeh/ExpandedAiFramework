

using Il2CppRewired;

namespace ExpandedAiFramework
{
    //I am going to create a lot of these to keep things cleaner, better to have a central place to do dependency injection for common stuff
    public abstract class BaseSubManager
    {
        protected EAFManager mManager;
        protected TimeOfDay mTimeOfDay;
        protected ISubManager[] mSubManagers;

        public EAFManager Manager { get { return mManager; } }

        public BaseSubManager(EAFManager manager, ISubManager[] subManagers)
        {
            Initialize(manager, subManagers);
        }

        public virtual void Initialize(EAFManager manager, ISubManager[] subManagers)
        {
            mManager = manager;
            mSubManagers = subManagers;
        }

        public virtual void Shutdown() { }
        public virtual void OnStartNewGame() { OnLoadScene(mManager.CurrentScene); }
        public virtual void OnLoadScene(string sceneName) { }
        public virtual void OnInitializedScene(string sceneName) { }
        public virtual void OnSaveGame() { }
        public virtual void OnLoadGame() { }
        public virtual void Update() { }
        public virtual void OnQuitToMainMenu() { }
    }
}
