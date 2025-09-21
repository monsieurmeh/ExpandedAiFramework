using UnityEngine;


namespace ExpandedAiFramework
{
    public abstract class MapDataPaintManager<T> : BasePaintManager where T : class, IMapData, new()
    {
        protected string mCurrentDataName = string.Empty;
        protected string mCurrentDataNameBase = string.Empty;
        protected string mCurrentDataPath = string.Empty;
        protected string mDefaultDataPath = string.Empty;
        protected List<GameObject> mDebugShownObjects = new List<GameObject>();
        protected DataManager DataManager => mManager.DataManager;

        public override string TypeInfo => $"MapDataPaintManager<{typeof(T).Name}>";

        public MapDataPaintManager(EAFManager manager) : base(manager) 
        {
            mDefaultDataPath = Path.Combine(DataFolderPath, $"{typeof(T).Name}s.json");
        }

        public override void Initialize()
        {
            base.Initialize();
            // Only set default path if no custom path has been set
            if (string.IsNullOrEmpty(mCurrentDataPath))
            {
                mCurrentDataPath = mDefaultDataPath;
            }
        }

        public override void ProcessCommand(string command, string[] args)
        {
            switch (command.ToLower())
            {
                case CommandString_Delete: ProcessDelete(args); break;
                case CommandString_Save: ProcessSave(args); break;
                case CommandString_Load: ProcessLoad(args); break;
                case CommandString_GoTo: ProcessGoTo(args); break;
                case CommandString_Show: ProcessShow(args); break;
                case CommandString_Hide: ProcessHide(args); break;
                case CommandString_List: ProcessList(args); break;
                case CommandString_Paint: ProcessPaint(args); break;
                case CommandString_Set: ProcessSet(args); break;
                default: this.LogWarningInstanced($"Unknown command: {command}"); break;
            }
        }

        public virtual void ProcessSet(string[] args)
        {
            if (args.Length < 2)
            {
                this.LogWarningInstanced("Set command requires property and value");
                return;
            }
            
            string property = args[0].ToLower();
            string value = args[1];
            
            switch (property)
            {
                case "datapath": 
                    mCurrentDataPath = value;
                    this.LogAlwaysInstanced($"Set data path to: {value}");
                    break;
                default:
                    ProcessSetCustom(property, value);
                    break;
            }
        }

        protected virtual void ProcessSetCustom(string property, string value)
        {
            this.LogWarningInstanced($"Unknown property: {property}");
        }

        protected abstract void ProcessDelete(string[] args);
        protected abstract void ProcessGoTo(string[] args);
        protected abstract void ProcessPaint(string[] args);

        protected virtual void ProcessSave(string[] args)
        {
            DataManager.SaveMapData();
        }

        protected virtual void ProcessLoad(string[] args)
        {
            DataManager.LoadMapData();
        }

        protected virtual void ProcessShow(string[] args)
        {
            if (args.Length == 0)
            {
                ShowAll();
            }
            else
            {
                ShowByName(args[0]);
            }
        }

        protected virtual void ProcessHide(string[] args)
        {
            if (args.Length == 0)
            {
                HideAll();
            }
            else
            {
                HideByName(args[0]);
            }
        }

        protected virtual void ProcessList(string[] args)
        {
            DataManager.ScheduleMapDataRequest<T>(new ForEachMapDataRequest<T>(mManager.CurrentScene, (data) =>
            {
                this.LogAlwaysInstanced($"Found {data}. Occupied: {data.Claimed}");
            }, false));
        }

        protected abstract void ShowAll();
        protected abstract void ShowByName(string name);
        protected abstract void HideAll();
        protected abstract void HideByName(string name);

        protected void GetUniqueMapDataName(string baseName, Action<string> callback)
        {
            DataManager.ScheduleMapDataRequest<T>(new GetUniqueMapDataNameRequest<T>(mManager.CurrentScene, baseName, callback));
        }

        protected void GetMapDataByName(string name, Action<T, RequestResult> callback)
        {
            DataManager.ScheduleMapDataRequest<T>(new GetMapDataByNameRequest<T>(name, mManager.CurrentScene, callback, false));
        }

        protected void RegisterMapData(T data, Action<T, RequestResult> callback)
        {
            DataManager.ScheduleMapDataRequest<T>(new RegisterDataRequest<T>(data, mCurrentDataPath, callback, false));
        }

        protected void DeleteMapData(Guid guid, Action<T, RequestResult> callback)
        {
            DataManager.ScheduleMapDataRequest<T>(new DeleteDataRequest<T>(guid, mManager.CurrentScene, callback, false));
        }

        protected void ForEachMapData(Action<T> callback)
        {
            DataManager.ScheduleMapDataRequest<T>(new ForEachMapDataRequest<T>(mManager.CurrentScene, callback, false));
        }

        protected virtual string GetDataFolderPath()
        {
            return Path.Combine(MelonLoader.Utils.MelonEnvironment.ModsDirectory, DataFolderPath);
        }

        protected virtual bool IsNameProvided(string name, bool required = true)
        {
            if (string.IsNullOrEmpty(name))
            {
                if (required)
                {
                    this.LogWarningInstanced("Name is required but not provided");
                }
                return false;
            }
            return true;
        }

        private class ForEachMapDataRequest<TData> : DataRequest<TData> where TData : IMapData, new()
        {
            protected Action<TData> mForEachCallback;
            protected string mScene;

            public override string TypeInfo => $"ForEachMapDataRequest<{typeof(TData).Name}>";

            public ForEachMapDataRequest(string scene, Action<TData> forEachCallback, bool callbackIsThreadSafe) 
                : base(null, true, callbackIsThreadSafe)
            {
                mForEachCallback = forEachCallback;
                mScene = scene;
            }

            protected override bool Validate()
            {
                if (mForEachCallback == null)
                {
                    this.LogTraceInstanced("null foreach callback");
                    return false;
                }
                return true;
            }

            protected override RequestResult PerformRequestInternal()
            {
                try
                {
                    foreach (TData data in mDataContainer.GetSceneData(mScene).Values)
                    {
                        if (mThreadSafeCallback)
                        {
                            mForEachCallback.Invoke(data);
                        }
                        else
                        {
                            TData dispatchedData = data;
                            EAFManager.Instance.DispatchManager.Dispatch(() => mForEachCallback.Invoke(dispatchedData));
                        }
                    }
                    return RequestResult.Succeeded;
                }
                catch (Exception e)
                {
                    this.LogErrorInstanced(e.Message);
                    return RequestResult.Failed;
                }
            }
        }

        private class GetUniqueMapDataNameRequest<TData> : Request<TData> where TData : IMapData, new()
        {
            protected string mScene;
            protected string mBaseName;
            protected Action<string> mFoundNameCallback;

            public override string TypeInfo => $"GetUniqueMapDataNameRequest<{typeof(TData).Name}>";

            public GetUniqueMapDataNameRequest(string scene, string baseName, Action<string> callback) 
                : base(null, true, false)
            {
                mBaseName = baseName;
                mFoundNameCallback = callback;
                mScene = scene;
            }

            protected override bool Validate()
            {
                if (mFoundNameCallback == null)
                {
                    this.LogTraceInstanced("null foundname callback");
                    return false;
                }
                if (string.IsNullOrEmpty(mScene))
                {
                    this.LogTraceInstanced("null or empty scene");
                    return false;
                }
                if (string.IsNullOrEmpty(mBaseName))
                {
                    this.LogTraceInstanced("null or empty mBaseName");
                    return false;
                }
                return true;
            }

            protected override RequestResult PerformRequestInternal()
            {
                try
                {
                    int counter = 1;
                    while (counter < 1000)
                    {
                        if (mDataContainer.GetSceneData(mScene).Values.Any(s => s.Name == $"{mBaseName}_{counter}"))
                        {
                            counter++;
                            continue;
                        }
                        mFoundNameCallback.Invoke($"{mBaseName}_{counter}");
                        return RequestResult.Succeeded;
                    }
                    return RequestResult.Failed;
                }
                catch (Exception e)
                {
                    this.LogErrorInstanced(e.Message);
                    return RequestResult.Failed;
                }
            }
        }
    }
}
