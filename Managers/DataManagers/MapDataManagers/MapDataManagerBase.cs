using MelonLoader.TinyJSON;
using MelonLoader.Utils;
using UnityEngine;

namespace ExpandedAiFramework
{
    public class MapDataManager<T> : SubDataManagerBase, ILogInfoProvider where T : MapData, new()
    {

        private readonly object mQueueLock = new object();
        private Queue<MapDataRequest<T>> mRequests = new Queue<MapDataRequest<T>>();
        private Task mTask;
        private bool mKeepTaskRunning = false;
        private Dictionary<string, List<T>> mData = new Dictionary<string, List<T>>();
        private Dictionary<Guid, T> mAvailableData = new Dictionary<Guid, T>();

        public MapDataManager(DataManager manager) : base(manager) { }

        public Dictionary<string, List<T>> Data { get { return mData; } }
        public Dictionary<Guid, T> AvailableData { get { return mAvailableData; } }
        public virtual string InstanceInfo { get { return string.Empty; } }
        public string TypeInfo { get { return $"MapDataManager<{typeof(T).Name}>"; } }


        public override void StartWorker()
        {
            if (mKeepTaskRunning)
            {
                return;
            }
            mKeepTaskRunning = true;
            this.LogVerboseInstanced($"Starting worker thread");
            mTask = Task.Run(Worker);
        }


        public override void StopWorker()
        {
            if (!mKeepTaskRunning)
            {
                return;
            }
            mKeepTaskRunning = false;
            try
            {
                mTask?.Wait();
            }
            catch (Exception e)
            {
                this.LogErrorInstanced($"Error stopping MapDataManagerBase<{nameof(T)}>: {e}");
            }
        }


        private void Worker()
        {
            while (mKeepTaskRunning)
            {
                MapDataRequest<T> request = null;
                lock (mQueueLock)
                {
                    if (mRequests.Count > 0)
                    {
                        request = mRequests.Dequeue();
                        this.LogVerboseInstanced($"(Queue count: {mRequests.Count + 1} -> {mRequests.Count}) Processing {request}");
                    }
                }

                if (request != null)
                {
                    T result = GetNearestMapData(request);
                    mManager.Manager.DispatchManager.Dispatch(() => request.Callback(result));
                }
                else
                {
                    // Small delay when queue is empty to prevent busy waiting
                    Thread.Sleep(50);
                }
            }
        }


        public override void Clear()
        {
            mData.Clear();
            mAvailableData.Clear();
        }


        public void GetNearestMapDataAsync(Vector3 position, Action<T> callback, int extraNearestCandidatesToMaybePickFrom = 0, params object[] args)
        {
            lock (mQueueLock)
            {
                this.LogVerboseInstanced($"(Queue count: {mRequests.Count} -> {mRequests.Count + 1}");
                mRequests.Enqueue(new MapDataRequest<T>(position, callback, extraNearestCandidatesToMaybePickFrom, args));
            }
        }


        protected T GetNearestMapData(MapDataRequest<T> request)
        {
            lock (mAvailableData)
            {
                if (mAvailableData.Count == 0)
                {
                    this.LogVerboseInstanced($"No available dictionary entries for {typeof(T).Name}");
                    return null;
                }


                // Optionally pick from nearest N
                if (request.ExtraCandidates > 0)
                {
                    var nearest = mAvailableData.Values
                        .Where(data => ValidEntry(request, data))
                        .OrderBy(data => OrderBy(request, data))
                        .Take(request.ExtraCandidates)
                        .ToList();

                    return nearest[UnityEngine.Random.Range(0, nearest.Count)];
                }
                else
                {
                    return mAvailableData.Values
                        .Where(data => ValidEntry(request, data))
                        .OrderBy(data => OrderBy(request, data))
                        .First();
                }
            }
        }


        public override void RefreshData(string sceneName)
        {
            mAvailableData.Clear();
            if (mData.TryGetValue(sceneName, out List<T> datas))
            {
                foreach (T tItem in datas)
                {
                    if (!mAvailableData.TryAdd(tItem.Guid, tItem))
                    {
                        this.LogErrorInstanced($"Guid collision while trying to add {tItem}!");
                    }
                }
            }
        }


        public override void Save()
        {
            try
            {
                Dictionary<string, List<T>> allDatas = new Dictionary<string, List<T>>();
                foreach (string key in mData.Keys)
                {
                    foreach (T tItem in mData[key])
                    {
                        if (!tItem.Transient)
                        {
                            if (!allDatas.TryGetValue(tItem.FilePath, out List<T> subDatas))
                            {
                                subDatas = new List<T>();
                                allDatas.Add(tItem.FilePath, subDatas);
                            }
                            subDatas.Add(tItem);
                        }
                    }
                }
                foreach (string key in allDatas.Keys)
                {
                    File.WriteAllText(Path.Combine(MelonEnvironment.ModsDirectory, key), JSON.Dump(allDatas[key], EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints), System.Text.Encoding.UTF8);
                }
            } 
            catch (Exception e)
            {
                this.LogErrorInstanced(e.Message);
            }
        }


        public override void Load()
        {
            mData.Clear();
            bool canAdd;
            try
            {
                string hidingSpots = File.ReadAllText(Path.Combine(MelonEnvironment.ModsDirectory, "EAF", $"{typeof(T)}s.json"), System.Text.Encoding.UTF8);
                if (hidingSpots != null)
                {
                    Variant hidingSpotsVariant = JSON.Load(hidingSpots);
                    foreach (var spotJSON in hidingSpotsVariant as ProxyArray)
                    {
                        canAdd = true;
                        T newData = spotJSON.Make<T>();
                        newData.UpdateCachedString();
                        newData.FilePath = Path.Combine("EAF", $"{typeof(T)}s.json");
                        if (!mData.TryGetValue(newData.Scene, out List<T> sceneData))
                        {
                            sceneData = new List<T>();
                            mData.Add(newData.Scene, sceneData);
                        }
                        for (int i = 0, iMax = sceneData.Count; i < iMax; i++)
                        {
                            if (sceneData[i] == newData)
                            {
                                //this.LogWarningInstanced($"Can't add duplicate {newData} (existing: {sceneData[i]})");
                                canAdd = false;
                            }
                        }
                        if (canAdd)
                        {
                            this.LogVerboseInstanced($"Found and adding {newData}");
                            sceneData.Add(newData);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                this.LogErrorInstanced(e.Message);
            }
        }


        public override void LoadAdditional(string pathFromModsFolder)
        {
            bool canAdd;
            try
            {
                string hidingSpots = File.ReadAllText(Path.Combine(MelonEnvironment.ModsDirectory, pathFromModsFolder), System.Text.Encoding.UTF8);
                if (hidingSpots != null)
                {
                    Variant hidingSpotsVariant = JSON.Load(hidingSpots);
                    foreach (var spotJSON in hidingSpotsVariant as ProxyArray)
                    {
                        canAdd = true;
                        T newData = spotJSON.Make<T>();
                        newData.UpdateCachedString();
                        newData.FilePath = pathFromModsFolder;
                        if (!mData.TryGetValue(newData.Scene, out List<T> sceneData))
                        {
                            sceneData = new List<T>();
                            mData.Add(newData.Scene, sceneData);
                        }
                        for (int i = 0, iMax = sceneData.Count; i < iMax; i++)
                        {
                            if (sceneData[i] == newData)
                            {
                                //this.LogWarningInstanced($"Can't add duplicate {newData} (existing: {sceneData[i]})");
                                canAdd = false;
                            }
                        }
                        if (canAdd)
                        {
                            this.LogVerboseInstanced($"Found and adding {newData}");
                            sceneData.Add(newData);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                this.LogErrorInstanced(e.Message);
            }
        }


        public bool TryClaim(Guid guid)
        {
            if (mAvailableData.TryGetValue(guid, out T data))
            {
                return false;
            }
            if (data.Claimed)
            {
                return false;
            }
            data.Claim();
            return true;
        }


        protected virtual bool ValidEntry(MapDataRequest<T> request, T tItem) => !tItem.Claimed;
        protected virtual float OrderBy(MapDataRequest<T> request, T tItem) => Vector3.SqrMagnitude(request.Position - tItem.AnchorPosition);
    }
}
