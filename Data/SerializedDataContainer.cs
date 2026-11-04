

namespace ExpandedAiFramework
{
    public sealed class SerializedDataContainer<T> where T : ISerializedData
    {
        private readonly Dictionary<string, Dictionary<Guid, T>> mData = new Dictionary<string, Dictionary<Guid, T>>();
        private readonly object mLock = new object();

        public int Count { get { lock (mLock) { return mData.Keys.Count; } } }

        public IEnumerable<T> EnumerateContents()
        {
            lock (mLock)
            {
                List<T> contents = new List<T>();
                foreach (Dictionary<Guid, T> sceneData in mData.Values)
                {
                    contents.AddRange(sceneData.Values);
                }
                return contents;
            }
        }


        public bool TryAddData(T data)
        {
            lock (mLock)
            {
                return GetOrAddSceneData(data.Scene).TryAdd(data.Guid, data);
            }
        }


        [Obsolete("Not thread-safe: use GetSceneDataSnapshot")]
        public Dictionary<Guid, T> GetSceneData(string scene)
        {
            lock (mLock)
            {
                return GetOrAddSceneData(scene);
            }
        }


        public List<T> GetSceneDataSnapshot(string scene)
        {
            lock (mLock)
            {
                return mData.TryGetValue(scene, out Dictionary<Guid, T> subData) ? new List<T>(subData.Values) : new List<T>();
            }
        }


        public bool TryGetData(string scene, Guid guid, out T data)
        {
            data = default;
            lock (mLock)
            {
                return mData.TryGetValue(scene, out Dictionary<Guid, T> subData) && subData.TryGetValue(guid, out data);
            }
        }


        public bool TryRemoveData(string scene, Guid guid)
        {
            lock (mLock)
            {
                return mData.TryGetValue(scene, out Dictionary<Guid, T> subData) && subData.Remove(guid);
            }
        }


        public void Clear()
        {
            lock (mLock)
            {
                mData.Clear();
            }
        }


        private Dictionary<Guid, T> GetOrAddSceneData(string scene)
        {
            if (!mData.TryGetValue(scene, out Dictionary<Guid, T> subData))
            {
                subData = new Dictionary<Guid, T>();
                mData.Add(scene, subData);
            }
            return subData;
        }
    }
}
