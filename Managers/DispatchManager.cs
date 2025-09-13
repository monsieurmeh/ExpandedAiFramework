using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExpandedAiFramework
{
    public sealed class DispatchManager : BaseSubManager 
    {
        public static DispatchManager Instance;
        private readonly Queue<Action> mActionQueue = new Queue<Action>();
        private Action mCurrentAction;
        private readonly object mQueueLock = new object();

        public DispatchManager(EAFManager manager, ISubManager[] subManagers) : base(manager, subManagers) 
        {
            Instance = this;
        }


        public void Dispatch(Action action)
        {
            lock (mQueueLock)
            {
                mActionQueue.Enqueue(action);
            }
        }

        
        public override void OnQuitToMainMenu()
        {
            lock (mQueueLock)
            {
                mActionQueue.Clear();
            }
        }


        public override void Update()
        {
            int actionsProcessed = 0;
            const int maxActionsPerFrame = 10;
            
            while (actionsProcessed < maxActionsPerFrame)
            {
                Action action = null;
                lock (mQueueLock)
                {
                    if (mActionQueue.Count > 0)
                    {
                        action = mActionQueue.Dequeue();
                    }
                }
                if (action != null)
                {
                    action.Invoke();
                    actionsProcessed++;
                }
                else
                {
                    break;
                }
            }
        }
    }
}
