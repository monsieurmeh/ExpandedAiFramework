

using Il2CppRewired.Utils.Classes.Utility;

namespace ExpandedAiFramework
{
    public class WeightedTypePicker<T> : IDisposable
    {
        private class Entry
        {
            public Type Type { get; }
            public Func<int> WeightProvider { get; }
            public Func<T, bool> Condition { get; }

            public Entry(Type type, Func<int> weightProvider, Func<T, bool> condition)
            {
                Type = type;
                WeightProvider = weightProvider;
                Condition = condition;
            }
        }

        private readonly List<Entry> allEntries = new();
        private readonly Random random = new();
        private readonly object mLock = new object();

        private List<(Type Type, int Weight)> validEntries = new();
        private float totalValidWeight = 0;
        private Func<T, Type> mGetFallbackTypeFunction;
        private Action<T, Type> mOnPick;
        private bool mRunWorker = true;
        private Task mTask;
        private Queue<Action> mQueue = new Queue<Action>();
        private Action mCurrentAction;


        public WeightedTypePicker(Func<T, Type> fallbackFunction, Action<T, Type> onPick)
        {
            mGetFallbackTypeFunction = fallbackFunction;
            mOnPick = onPick;
        }


        void IDisposable.Dispose()
        {
            StopWorker();
        }


        public void StartWorker()
        {
            LogTrace($"Starting TypePicker worker thread");
            mTask = Task.Run(Worker);
        }


        public void StopWorker()
        {
            LogTrace($"Stopping TypePicker worker thread");
            mRunWorker = false;
            try
            {
                mTask?.Wait();
            }
            catch (Exception e)
            {
                LogError($"Error stopping worker thread: {e}");
            }
        }


        private void Worker()
        {
            while (mRunWorker)
            {
                lock (mLock)
                {
                    if (mQueue.Count > 0)
                    {
                        mCurrentAction = mQueue.Dequeue();
                    }
                }
                if (mCurrentAction != null)
                {
                    mCurrentAction.Invoke();
                    mCurrentAction = null;
                }
                else
                {  
                    Thread.Sleep(50);
                }
            }
        }


        public void AddWeight(Type type, Func<int> weightProvider, Func<T, bool> condition)
        {
            if (weightProvider == null)
                throw new ArgumentNullException(nameof(weightProvider));

            allEntries.Add(new Entry(type, weightProvider, condition));
        }


        public Type PickType(T t)
        {
            lock (mLock)
            {
                Type returnType = null;
                validEntries.Clear();
                totalValidWeight = 0;

                foreach (var entry in allEntries)
                {
                    //LogDebug($"Checking {entry.Type} in WeightedTypePicker. Condition is {entry.Condition(t)}, weight is {entry.WeightProvider()}");
                    if (entry.Condition(t))
                    {
                        int weight = entry.WeightProvider();
                        if (weight > 0)
                        {
                            validEntries.Add((entry.Type, weight));
                            totalValidWeight += weight;
                            //LogDebug($"Add {entry.Type} to WeightedTypePicker valid pool. Compiled pool weight is now {totalValidWeight} with {validEntries.Count} entries.");
                        }
                    }
                }

                if (validEntries.Count == 0 || totalValidWeight <= 0)
                {
                    //LogError("WeightedTypePicker could not pick a valid spawn type!", ComplexLogger.FlaggedLoggingLevel.Critical);
                    return mGetFallbackTypeFunction.Invoke(t);
                }

                int roll = (int)(random.NextDouble() * totalValidWeight);
                float cumulative = 0;

                //LogDebug($"Rolled {roll}, checking against pool.");
                foreach (var (type, weight) in validEntries)
                {
                    cumulative += weight;
                    //LogDebug($"Added {type}, cumulative weight is {cumulative}");
                    if (roll <= cumulative)
                    {
                        //LogDebug($"Cumulative weight {cumulative} >=roll {roll}, picking type {type}!");
                        returnType = type;
                        break;
                    }
                }
                if (returnType == null)
                {
                    returnType = validEntries[0].Type;
                }
                mOnPick.Invoke(t, returnType);
                return returnType; // fallback
            }
        }


        public void PickTypeAsync(T t, Action<Type> callback)
        {
            lock (mLock)
            {
                mQueue.Enqueue(() =>
                { 
                    try
                    {
                        Type spawnType = PickType(t);
                        EAFManager.Instance.DispatchManager.Dispatch(() =>
                        {
                            callback.Invoke(spawnType);
                        });
                    }
                    catch (Exception e)
                    {
                        LogError($"ASYNC exception during WeightedTypePicker.PickTypeAsync<T>: {e}");
                        return;
                    }
                });
            }
        }


        public void Clear()
        {
            lock (mLock)
            {
                mQueue.Clear();
            }
        }
    }
}
