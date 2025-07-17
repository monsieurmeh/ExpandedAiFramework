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
    public class ProxyManager<T> : ILogInfoProvider where T : ModDataProxyBase, new()
    {
        protected DataManager mManager;
        protected string mDataLocation;
        protected bool mLoaded = false;
        private Dictionary<string, Dictionary<Guid, T>> mData = new Dictionary<string, Dictionary<Guid, T>>();

        public ProxyManager(DataManager manager, string dataLocation)
        {
            mManager = manager;
            mDataLocation = dataLocation;
        }

        public virtual string InstanceInfo { get { return string.Empty; } }
        public string TypeInfo { get { return $"ProxyManager<{typeof(T).Name}>"; } }


        public virtual void Clear()
        {
            mLoaded = false;
            mData.Clear();
        }


        public virtual void Save()
        {
            //if (!mLoaded)
            //{
            //    this.LogTraceInstanced($"Not loaded");
            //    return;
            //}
            this.LogTraceInstanced($"Saving");
            List<T> masterProxyList = new List<T>();
            foreach (Dictionary<Guid, T> subProxyList in mData.Values)
            {
                foreach (T proxy in subProxyList.Values)
                {
                    this.LogTraceInstanced($"Serializing {proxy}");
                }
                masterProxyList.AddRange(subProxyList.Values);
            }
            string json = JSON.Dump(masterProxyList, EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints);
            if (json == null || json == string.Empty)
            {
                return;
            }
            mManager.ModData.Save(json, mDataLocation);


            //For viewing only - remove in public build!
            File.WriteAllText(Path.Combine(MelonEnvironment.ModsDirectory, $"{mDataLocation}.json"), json);


            this.LogTraceInstanced($"Saved");
        }


        public virtual void Load()
        {
            try
            {
                if (mLoaded)
                {
                    this.LogTraceInstanced($"Already loaded");
                    return;
                }
                Clear();
                this.LogTraceInstanced($"Loading");
                string proxiesString = mManager.ModData.Load(mDataLocation);
                if (proxiesString == null)
                {
                    this.LogTraceInstanced($"No data found");
                    return;
                }
                Variant proxiesVariant = JSON.Load(proxiesString);
                foreach (var pathJSON in proxiesVariant as ProxyArray)
                {
                    T newProxy = new T();
                    JSON.Populate(pathJSON, newProxy);
                    //TODO: See if this code is still needed. If ambush wolves and tracking wolves retain their data on load, then we should be good.
                    /*
                    if (pathJSON is ProxyObject proxyObject && proxyObject.TryGetValue("CustomData", out Variant item) && item is ProxyArray proxyArray)
                    {
                        newProxy.CustomData = new string[proxyArray.Count];
                        for (int i = 0, iMax = proxyArray.Count; i < iMax; i++)
                        {
                            newProxy.CustomData[i] = proxyArray[i];
                            this.LogTraceInstanced($"Extracted custom data: {newProxy.CustomData[i]}");
                        }
                    }
                    */
                    if (!PostProcessProxyAfterLoad(newProxy))
                    {
                        continue;
                    }
                    this.LogTraceInstanced($"Deserializing {newProxy}");
                    GetSubData(newProxy.Scene).Add(newProxy.Guid, newProxy);
                }
                mLoaded = true;
                this.LogTraceInstanced($"Loaded");
            }
            catch (Exception e)
            {
                MelonLogger.Error($"{e}");
            }
        }



        public virtual void Refresh(string scene)
        {
            EAFManager.LogWithStackTrace("Refreshing");
            Dictionary<Guid, T> subData = GetSubData(scene);
            foreach (T proxy in subData.Values)
            {
                if (!IsProxyValid(proxy))
                {
                    //hopefully this doesnt cause an enumeration error...?
                    subData.Remove(proxy.Guid);
                    continue;
                }
                RefreshProxy(proxy);
            }
        }


        public virtual bool TryGetProxy(Guid guid, out T proxy, string scene = null)
        {
            if (string.IsNullOrEmpty(scene))
            {
                scene = mManager.Manager.CurrentScene;
            }
            return GetSubData(scene).TryGetValue(guid, out proxy);
        }


        public virtual bool TryRegisterProxy(T proxy)
        {
            if (!IsProxyValid(proxy))
            {
                EAFManager.LogWithStackTrace($"Invalid proxy: {proxy}");
                return false;
            }
            if (string.IsNullOrEmpty(proxy.Scene))
            {
                this.LogErrorInstanced($"No scene: {proxy}");
                return false;
            }
            Dictionary<Guid, T> subData = GetSubData(proxy.Scene);
            if (subData.ContainsKey(proxy.Guid))
            {
                EAFManager.LogWithStackTrace($"Guid collision: {proxy.Guid}");
                return false;
            }
            this.LogTraceInstanced($"Registered: {proxy}");
            subData.Add(proxy.Guid, proxy);
            return true;
        }


        protected Dictionary<Guid, T> GetSubData(string scene)
        {
            if (!mData.TryGetValue(scene, out Dictionary<Guid, T> subData))
            {
                subData = new Dictionary<Guid, T>();
                mData.Add(scene, subData);
            }
            return subData;
        }


        protected virtual void RefreshProxy(T proxy)
        {

        }


        protected virtual bool PostProcessProxyAfterLoad(T proxy)
        {
            if (GetSubData(proxy.Scene).ContainsKey(proxy.Guid))
            {
                this.LogErrorInstanced($"Guid collision: {proxy}");
                return false;
            }
            if (!IsProxyValid(proxy))
            {
                this.LogTraceInstanced($"Invalid Proxy: {proxy}");
                return false;
            }
            return true;
        }


        protected virtual bool IsProxyValid(T proxy) => true;
    }
}