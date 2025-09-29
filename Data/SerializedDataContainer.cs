

namespace ExpandedAiFramework
{
    public sealed class SerializedDataContainer<T> where T : ISerializedData
    {
        private readonly Dictionary<string, Dictionary<Guid, T>> mData = new Dictionary<string, Dictionary<Guid, T>>();

        public int Count => mData.Keys.Count;

        public IEnumerable<T> EnumerateContents()
        {
            foreach (Dictionary<Guid, T> sceneData in mData.Values)
            {
                foreach (T data in sceneData.Values)
                {
                    yield return data;
                }
            }
        }


        public bool TryAddData(T data)
        {
            if (!GetSceneData(data.Scene).TryAdd(data.Guid, data))
            {
                return false;
            }
            return true;
        }


        public Dictionary<Guid, T> GetSceneData(string scene)
        {
            if (!mData.TryGetValue(scene, out Dictionary<Guid, T> subData))
            {
                subData = new Dictionary<Guid, T>();
                mData.Add(scene, subData);
            }
            return subData;
        }


        public bool TryGetData(string scene, Guid guid, out T data)
        {
            data = default;
            return GetSceneData(scene).TryGetValue(guid, out data);
        }


        public bool TryRemoveData(string scene, Guid guid)
        {
            return GetSceneData(scene).Remove(guid);
        }


        public void Clear()
        {
            mData.Clear();
        }
    }
}
