using Harmony;
using Il2CppRewired.Utils;
using Il2CppVoice;
using MelonLoader.TinyJSON;
using MelonLoader.Utils;
using ModData;
using UnityEngine;
using static Il2Cpp.CarcassSite;


namespace ExpandedAiFramework
{
    public class ProxyManagerBase<T> : SubDataManager<T> where T : ModDataProxy, new()
    {
        protected string mDataLocation;

        public string DataLocation => mDataLocation;

        public ProxyManagerBase(DataManager manager, DispatchManager dispatcher, string dataLocation) : base(manager, dispatcher) => mDataLocation = dataLocation;

        public override string TypeInfo { get { return $"ProxyManagerBase<{typeof(T).Name}>"; } }
        protected override string GetDefaultDataPath() => mDataLocation;
        protected override void SaveJsonToPath(string json, string dataLocation) => mManager.ModData.Save(json, dataLocation);
        protected override string LoadJsonFromPath(string dataLocation) => mManager.ModData.Load(dataLocation);

        public void OnQuitToMainMenu() => mLoaded = false;
    }
}