using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExpandedAiFramework
{
    public sealed class DispatchManager : BaseSubManager 
    {
        private readonly Queue<Action> mActionQueue = new Queue<Action>();
        private readonly object mQueueLock = new object();

        public DispatchManager(EAFManager manager, ISubManager[] subManagers) : base(manager, subManagers) 
        {
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
            lock (mQueueLock)
            {
                while (mActionQueue.Count > 0)
                {
                    mActionQueue.Dequeue().Invoke();
                }
            }
        }
    }
}
