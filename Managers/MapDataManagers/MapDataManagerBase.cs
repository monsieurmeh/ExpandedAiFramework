using Il2CppRewired.Utils;
using Il2CppVoice;
using MelonLoader.TinyJSON;
using MelonLoader.Utils;
using ModData;
using UnityEngine;
using static Il2Cpp.CarcassSite;

namespace ExpandedAiFramework
{
    public abstract class MapDataManagerBase 
    {
        protected DataManager mManager;

        public MapDataManagerBase(DataManager manager)
        {
            mManager = manager;
        }

        public abstract void StartWorker();
        public abstract void StopWorker();
        public abstract void RefreshData(string sceneName);
        public abstract void Save();
        public abstract void Load();
        public abstract void Clear();
    }

    public class MapDataRequest<T>
    {
        public Vector3 Position;
        public Action<T> Callback;
        public int ExtraCandidates;
        private string mCachedString;
        public object[] Args;

        public MapDataRequest(Vector3 position, Action<T> callback, int extraCandidates, params object[] args)
        {
            Position = position;
            Callback = callback;
            ExtraCandidates = extraCandidates;
            Args = args;
            mCachedString = $"{nameof(MapDataRequest<T>)}.<{typeof(T)}> at {Position} with {ExtraCandidates} extra candidates";
            for (int i = 0, iMax = args?.Length ?? 0; i < iMax; i++)
            {
                mCachedString += $" (Arg{i}: {args[i]})";
            }
        }

        public override string ToString()
        {
            return mCachedString;
        }
    }

    public class MapDataManager<T> : MapDataManagerBase, ILogInfoProvider where T : MapData, new()
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
                List<T> allDatas = new List<T>();
                foreach (string key in mData.Keys)
                {
                    foreach (T tItem in mData[key])
                    {
                        if (!tItem.Transient)
                        {
                            allDatas.Add(tItem);
                        }
                    }
                    allDatas.AddRange(mData[key]);
                }
                File.WriteAllText(Path.Combine(MelonEnvironment.ModsDirectory, $"{typeof(T)}s.json"), JSON.Dump(allDatas, EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints), System.Text.Encoding.UTF8);
            } 
            catch (Exception e)
            {

                this.LogErrorInstanced($"[{nameof(MapDataRequest<T>)}<{typeof(T)}>.{nameof(Save)}] {e}");
            }
        }


        public override void Load()
        {
            mData.Clear();
            bool canAdd;
            try
            {
                string hidingSpots = File.ReadAllText(Path.Combine(MelonEnvironment.ModsDirectory, $"{typeof(T)}s.json"), System.Text.Encoding.UTF8);
                if (hidingSpots != null)
                {
                    Variant hidingSpotsVariant = JSON.Load(hidingSpots);
                    foreach (var spotJSON in hidingSpotsVariant as ProxyArray)
                    {
                        canAdd = true;
                        T newData = spotJSON.Make<T>();
                        newData.UpdateCachedString();
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
                this.LogErrorInstanced($"[{nameof(MapDataRequest<T>)}<{typeof(T)}>.{nameof(Load)}] {e}");
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
