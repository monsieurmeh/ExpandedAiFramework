using MelonLoader.TinyJSON;
using MelonLoader.Utils;


namespace ExpandedAiFramework
{
    //NOTE for future nick
    // This class follows a very simple format to remain thread safe
    // Public methods are allowed only to queue requests into the system
    // ONLY protected and private methods are allowed to manipulate fields and perform logic
    // Because no public fields call on the private/protected methods, we can assume that only the worker thread will access them, ensuring thread safety
    // 
    //


    public abstract class SubDataManager<T> : ISubDataManager,
                                              ISerializedDataProvider<T>,
                                              ISerializedDataValidatorProvider<T>
                                              where T : class, ISerializedData, new()
    {
        protected readonly object mRequestQueueLock = new object();
        protected DataManager mManager;
        protected DispatchManager mDispatcher;
        protected IRequest mActiveRequest;
        protected Action mActiveAction;
        protected Task mTask;
        protected bool mLoaded;
        protected bool mKeepTaskRunning = false;
        protected string mCurrentScene;
        protected Queue<Action> mInternalActionQueue = new Queue<Action>();
        protected Queue<IRequest> mRequests = new Queue<IRequest>();
        protected SerializedDataContainer<T> mDataContainer = new SerializedDataContainer<T>();

        public virtual string InstanceInfo { get { return string.Empty; } }
        public abstract string TypeInfo { get; }

        public SubDataManager(DataManager manager, DispatchManager dispatcher)
        {
            mManager = manager;
            mDispatcher = dispatcher;
        }

        public SerializedDataContainer<T> GetDataContainer() => mDataContainer;
        public Func<T, bool> GetDataValidator() => IsDataValid;


        private ManualResetEventSlim mWorkAvailable = new ManualResetEventSlim(false);

        public virtual void StartWorker()
        {
            if (mKeepTaskRunning)
            {
                return;
            }
            mKeepTaskRunning = true;
            this.LogTraceInstanced($"Starting worker thread");
            mTask = Task.Run(Worker);
        }
        public virtual void StopWorker()
        {
            if (!mKeepTaskRunning)
            {
                return;
            }
            mKeepTaskRunning = false;
            lock (mRequestQueueLock)
            {
                mWorkAvailable.Set(); // Wake up the worker to exit
            }
            try
            {
                mTask?.Wait();
            }
            catch (Exception e)
            {
                this.LogErrorInstanced($"Error stopping worker thread: {e}");
            }
            finally
            {
                lock (mRequestQueueLock)
                {
                    mWorkAvailable.Dispose();
                }
            }
        }

        public void ClearRequests()
        {
            lock(mRequestQueueLock)
            {
                mRequests.Clear();
                mInternalActionQueue.Clear();
                mActiveRequest = null;
                mActiveAction = null;
            }
        }

        public void ScheduleRequest(IRequest request)
        {
            request.Preprocess(this);
            lock (mRequestQueueLock)
            {
                request.QueueTime = DateTime.Now.Ticks;
                mRequests.Enqueue(request);
                mWorkAvailable.Set();
            }
        }
        public void ScheduleSave() => ScheduleInternalAction(Save);
        public void ScheduleLoad() => ScheduleInternalAction(Load);
        public void ScheduleClear() => ScheduleInternalAction(Clear);
        public void ScheduleLoadAdditional(string pathFromModsFolder) => ScheduleInternalAction(() => LoadFromPath(pathFromModsFolder));
        public void ScheduleRefresh(string scene) => ScheduleInternalAction(() => Refresh(scene));


        private void ScheduleInternalAction(Action action)
        {
            lock (mRequestQueueLock)
            {
                mInternalActionQueue.Enqueue(action);
                mWorkAvailable.Set();
            }
        }

        private void Worker()
        {
            while (mKeepTaskRunning)
            {
                mWorkAvailable.Wait();
                if (!mKeepTaskRunning) break;
                HandleInternalActions();
                GetActiveRequest();
                HandleActiveRequest();
            }
        }
        
        private void GetActiveRequest()
        {
            lock (mRequestQueueLock)
            {
                if (mRequests.Count > 0)
                {
                    mActiveRequest = mRequests.Dequeue();
                }
                else
                {
                    mActiveRequest = null;
                }
                if (mInternalActionQueue.Count == 0 && mRequests.Count == 0)
                {
                    mWorkAvailable.Reset();
                }
            }
        }


        private void HandleActiveRequest()
        {
            if (mActiveRequest != null)
            {
                if (mActiveRequest.ThreadSafe)
                {
                    ProcessRequest(mActiveRequest);
                }
                else
                {
                    IRequest dispatchedRequest = mActiveRequest;
                    if (mDispatcher != null)
                    {
                        mDispatcher.Dispatch(() => ProcessRequest(dispatchedRequest));
                    }
                }
                mActiveRequest = null;
            }
        }

        private void ProcessRequest(IRequest request)
        {
            request.RequestStartTime = DateTime.Now.Ticks;
            request.PerformRequest();
            request.RequestCompleteTime = DateTime.Now.Ticks;
            if (request.Result == RequestResult.Requeue)
            {
                lock (mRequestQueueLock)
                {
                    mRequests.Enqueue(request);
                    mWorkAvailable.Set();
                }   
            }
            else
            {
                if (request.ThreadSafe && !request.ThreadSafeCallback)
                {
                    mDispatcher.Dispatch(() => CallbackWithExecutionLog(request));
                }
                else
                {
                    CallbackWithExecutionLog(request);
                }
            }
        }

        
        private void CallbackWithExecutionLog(IRequest request)
        {
            if (request != null)
            {
                request.CallbackStartTime = DateTime.Now.Ticks;
                request.Callback();
                request.CallbackCompleteTime = DateTime.Now.Ticks;
                //request.LogExecutionInfo(BuildQueueExecutionInfo());
            }
        }

        private string BuildQueueExecutionInfo()
        {
            string queueExecutionInfo = string.Empty;
            lock(mRequestQueueLock)
            {
                queueExecutionInfo += $"Queue size: {mRequests.Count}\n";
                queueExecutionInfo += $"Manager: {GetType()}\n";
            }
            return queueExecutionInfo;
        }

        private void HandleInternalActions()
        {
            int requestCount = 0;
            lock (mRequestQueueLock)
            {
                requestCount = mInternalActionQueue.Count;
            }
            while (requestCount > 0)
            {
                lock (mRequestQueueLock)
                {
                    mActiveAction = mInternalActionQueue.Dequeue();
                }
                mActiveAction.Invoke();
                mActiveAction = null;
                lock (mRequestQueueLock)
                {
                    requestCount = mInternalActionQueue.Count;
                }
            }
        }


        private void Save()
        {
            this.LogTraceInstanced($"Saving");
            Dictionary<string, List<T>> masterProxyDict = new Dictionary<string, List<T>>();
            foreach (T data in mDataContainer.EnumerateContents())
            {
                this.LogTraceInstanced($"Saving {data.DisplayName}");
                if (!masterProxyDict.TryGetValue(data.DataLocation, out List<T> dataList))
                {
                    dataList = new List<T>();
                    masterProxyDict.Add(data.DataLocation, dataList); 
                }
                dataList.Add(data);
            }
            foreach (string dataLocation in masterProxyDict.Keys)
            {
                string json = JSON.Dump(masterProxyDict[dataLocation], EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints);
                if (json == null || json == string.Empty)
                {
                    continue;
                }
                SaveJsonToPath(json, dataLocation);
                this.LogDebugInstanced($"Saved to {dataLocation} with json length {json.Length}");
            }
        }


        private void Load()
        {
            this.LogTraceInstanced($"Loading");
            if (mLoaded)
            {
                this.LogTraceInstanced($"Already loaded");
                return;
            }
            Clear();
            mLoaded = LoadFromPath(GetDefaultDataPath());
            this.LogDebugInstanced($"Loaded: {mLoaded}");
        }


        private bool LoadFromPath(string dataPath)
        {
            try
            {
                this.LogTraceInstanced($"Loading from path: {dataPath}");
                string dataString = LoadJsonFromPath(dataPath);
                if (dataString == null)
                {
                    this.LogDebugInstanced($"No data found at path: {dataPath}");
                    return false;
                }
                Variant dataVariant = JSON.Load(dataString);
                foreach (var pathJSON in dataVariant as ProxyArray)
                {
                    T newData = new T();
                    JSON.Populate(pathJSON, newData);
                    newData.DataLocation = dataPath;
                    if (!PostProcessDataAfterLoad(newData))
                    {
                        this.LogErrorInstanced($"Failed to postprocess {newData.DisplayName}, skipping!");
                        continue;
                    }
                    if (!mDataContainer.TryAddData(newData))
                    {
                        this.LogErrorInstanced($"Failed to add {newData.DisplayName}!");
                    }
                    this.LogTraceInstanced($"Loaded {newData.DisplayName} from {dataPath}");
                }
                this.LogDebugInstanced($"Loaded from path: {dataPath}");
                return true;
            }
            catch (Exception e)
            {
                this.LogErrorInstanced(e.Message);
                return false;
            }
        }


        protected virtual void Refresh(string scene)
        {
            mCurrentScene = scene;
            Dictionary<Guid, T> subData = mDataContainer.GetSceneData(scene);
            List<Guid> removeIds = new List<Guid>();
            foreach (T data in subData.Values)
            { 
                if (!IsDataValid(data))
                {
                    removeIds.Add(data.Guid);
                    continue;
                }
                RefreshData(data);
            }
            foreach (Guid guid in removeIds)
            {
                subData.Remove(guid);
            }
        }


        protected abstract string LoadJsonFromPath(string dataLocation);
        protected abstract string GetDefaultDataPath();
        protected abstract void SaveJsonToPath(string json, string dataLocation);

        protected virtual void Clear() => mDataContainer.Clear();

        protected virtual bool PostProcessDataAfterLoad(T data)
        {
            return IsDataValid(data);
        }


        protected virtual bool IsDataValid(T data)
        {
            if (data == null)
            {
                this.LogTraceInstanced($"Null data");
                return false;
            }
            if (data.Guid == Guid.Empty)
            {
                this.LogTraceInstanced($"Data with empty guid");
                return false;
            }
            if (string.IsNullOrEmpty(data.Scene))
            {
                this.LogTraceInstanced($"Data with empty scene");
                return false;
            }
            if (string.IsNullOrEmpty(data.DataLocation))
            {
                this.LogTraceInstanced($"Data with empty data location");
                return false;
            }
            return true;
        }


        protected virtual void RefreshData(T data) => OnRegister(data);


        public bool TryRegister(T data)
        {
            if (mDataContainer.TryAddData(data))
            {
                OnRegister(data);
                return true;
            }
            return false;
        }


        protected virtual void OnRegister(T data) { }
    }
}
