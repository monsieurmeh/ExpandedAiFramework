using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExpandedAiFramework
{
    public sealed class DispatchManager : BaseSubManager 
    {
        public static DispatchManager Instance;
        private readonly Queue<Action> mActionQueue = new Queue<Action>();
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


        public override void Update()
        {
            lock (mQueueLock)
            {
                if (mActionQueue.Count > 0)
                {
                    mActionQueue.Dequeue().Invoke();
                }
            }
        }
    }
}
