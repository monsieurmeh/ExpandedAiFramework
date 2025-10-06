using UnityEngine;


namespace ExpandedAiFramework
{
    public abstract class MapDataPaintManager<T> : BasePaintManager where T : class, IMapData, new()
    {
        protected string mCurrentDataName = string.Empty;
        protected string mCurrentDataNameBase = string.Empty;
        protected string mCurrentDataPath = string.Empty;
        protected string mDefaultDataPath = string.Empty;

        // Public accessors for settings
        public string CurrentDataPath => mCurrentDataPath;
        public string DefaultDataPath => mDefaultDataPath;
        public string CurrentDataName => mCurrentDataName;
        public string CurrentDataNameBase => mCurrentDataNameBase;

        // Public setters for settings
        public void SetDataPath(string path)
        {
            mCurrentDataPath = path;
            this.LogAlwaysInstanced($"Data path set to: {path}", LogCategoryFlags.PaintManager);
        }
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

        public override void ProcessCommand(string command, IList<string> args)
        {
            switch (command.ToLower())
            {
                case CommandString_Delete: ProcessDelete(args); break;
                case CommandString_GoTo: ProcessGoTo(args); break;
                case CommandString_Show: ProcessShow(args); break;
                case CommandString_Hide: ProcessHide(args); break;
                case CommandString_List: ProcessList(args); break;
                case CommandString_Paint: ProcessPaint(args); break;
                case CommandString_Set: ProcessSet(args); break;
                default: this.LogWarningInstanced($"Unknown command: {command}", LogCategoryFlags.PaintManager); break;
            }
        }

        public virtual void ProcessSet(IList<string> args)
        {           
            string property = GetNextArg(args).ToLower();
            string value = GetNextArg(args);
            
            switch (property)
            {
                case "datapath": 
                    mCurrentDataPath = value;
                    this.LogAlwaysInstanced($"Set data path to: {value}", LogCategoryFlags.PaintManager);
                    break;
                default:
                    ProcessSetCustom(property, value, args);
                    break;
            }
        }

        protected virtual void ProcessSetCustom(string property, string value, IList<string> args)
        {
            this.LogWarningInstanced($"Unknown property: {property}", LogCategoryFlags.PaintManager);
        }

        protected abstract void ProcessDelete(IList<string> args);
        protected abstract void ProcessGoTo(IList<string> args);
        protected abstract void ProcessPaint(IList<string> args);

        protected virtual void ProcessShow(IList<string> args)
        {
            if (args.Count == 0)
            {
                ShowAll();
            }
            else
            {
                ShowByName(GetNextArg(args));
            }
        }

        protected virtual void ProcessHide(IList<string> args)
        {
            if (args.Count == 0)
            {
                HideAll();
            }
            else
            {
                HideByName(GetNextArg(args));
            }
        }

        protected virtual void ProcessList(IList<string> args)
        {
            DataManager.ScheduleMapDataRequest<T>(new ForEachMapDataRequest<T>(mManager.CurrentScene, (data) =>
            {
                this.LogAlwaysInstanced($"Found {data}. Occupied: {data.Claimed}", LogCategoryFlags.PaintManager);
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
