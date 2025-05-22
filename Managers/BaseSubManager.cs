

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

        public BaseSubManager(EAFManager manager, ISubManager[] subManagers, TimeOfDay timeOfDay)
        {
            Initialize(manager, subManagers, timeOfDay);
        }

        public virtual void Initialize(EAFManager manager, ISubManager[] subManagers, TimeOfDay timeOfDay)
        {
            mManager = manager;
            mSubManagers = subManagers;
            mTimeOfDay = timeOfDay;
        }

        public virtual void Shutdown() { }
        public virtual void OnStartNewGame() { }
        public virtual void OnLoadScene() { }
        public virtual void OnInitializedScene() { }
        public virtual void OnSaveGame() { }
        public virtual void OnLoadGame() { }
        public virtual void Update() { }
    }
}
