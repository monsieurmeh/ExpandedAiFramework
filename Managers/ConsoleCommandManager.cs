

using UnityEngine.AI;
using UnityEngine;
using Il2Cpp;
using System.Xml.Linq;
using System.Buffers.Text;
using static Il2CppRewired.Demos.SimpleControlRemapping;

namespace ExpandedAiFramework
{
    public sealed class ConsoleCommandManager : BaseSubManager
    {
        private class ForEachMapDataRequest<T> : DataRequest<T> where T : IMapData, new()
        {
            protected Action<T> mForEachCallback;
            protected string mScene; 

            public override string TypeInfo { get { return $"ForEachMapDataRequest{typeof(T)}"; } }

            public ForEachMapDataRequest(string scene, Action<T> forEachCallback) : base(null, false)
            {
                mForEachCallback = forEachCallback;
                mScene = scene;
            }



            protected override bool Validate()
            {
                if (mForEachCallback == null)
                {
                    this.LogTraceInstanced($"null foreach callback");
                    return false;
                }
                return true;
            }


            protected override RequestResult PerformRequestInternal()
            {
                try
                {
                    foreach (T data in mDataContainer.GetSceneData(mScene).Values)
                    {
                        mForEachCallback.Invoke(data);
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

        private class GetUniqueMapDataName<T> : Request<T> where T : IMapData, new()
        {
            protected string mScene;
            protected string mBaseName;
            protected Action<string> mFoundNameCallback;

            public override string TypeInfo { get { return $"GetUniqueMapDataName{typeof(T)}"; } }
            
            public GetUniqueMapDataName(string scene, string baseName, Action<string> callback) : base(null, false)
            {
                mBaseName = baseName;
                mFoundNameCallback = callback;
                mScene = scene;
            }



            protected override bool Validate()
            {
                if (mFoundNameCallback == null)
                {
                    this.LogTraceInstanced($"null foundname callback");
                    return false;
                }
                if (string.IsNullOrEmpty(mScene))
                {
                    this.LogTraceInstanced($"null or emoty scene");
                    return false;
                }
                if (string.IsNullOrEmpty(mBaseName))
                {
                    this.LogTraceInstanced($"null or emoty mBaseName");
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


        private enum PaintMode : int
        {
            WanderPath = 0,
            HidingSpot = 1,
            COUNT
        }

        private PaintMode mCurrentPaintMode = PaintMode.COUNT;
        private string mCurrentPaintFilePath = string.Empty;
        private bool mSelectingHidingSpotRotation = false;
        private Vector3 mPendingHidingSpotPosition;
        private string mCurrentDataNameIterated = string.Empty;
        private string mCurrentDataNameBase = string.Empty;
        private List<Vector3> mCurrentWanderPathPoints = new List<Vector3>();
        private List<GameObject> mCurrentWanderPathPointMarkers = new List<GameObject>();
        private GameObject mPaintMarker = null;
        private GameObject mDirectionArrow = null;
        private Vector3 mPaintMarkerPosition = Vector3.zero;
        private Quaternion mPaintMarkerRotation = Quaternion.identity;
        private List<GameObject> mDebugShownHidingSpots = new List<GameObject>();
        private List<GameObject> mDebugShownWanderPaths = new List<GameObject>();
        private List<GameObject> mDebugShownSpawnRegions = new List<GameObject>();

        public ConsoleCommandManager(EAFManager manager, ISubManager[] subManagers) : base(manager, subManagers) { }
        
        private DataManager DataManager { get { return mManager.DataManager; } }


        public override void OnLoadScene(string sceneName)
        {
            base.OnLoadScene(sceneName);
            mCurrentPaintMode = PaintMode.COUNT;
            mCurrentDataNameIterated = string.Empty;
            mCurrentWanderPathPoints.Clear();
            for (int i = 0, iMax = mCurrentWanderPathPointMarkers.Count; i < iMax; i++)
            {
                UnityEngine.Object.Destroy(mCurrentWanderPathPointMarkers[i]);
            }
            mCurrentWanderPathPointMarkers.Clear();
            if (mPaintMarker != null)
            {
                UnityEngine.Object.Destroy(mPaintMarker);
                mPaintMarker = null;
            }
        }


        public override void Update()
        {
            UpdatePaintMarker();
        }

        #region Helpers

        private void SaveMapData() => mManager.DataManager.SaveMapData();
        private void LoadMapData() => mManager.DataManager.LoadMapData();


        public GameObject CreateDirectionArrow(Vector3 startPos, Vector3 targetPos, Color color, string name)
        {
            // Create arrow body (cylinder)
            GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.Destroy(arrow.GetComponent<Collider>());
            
            // Calculate direction and distance
            Vector3 direction = targetPos - startPos;
            direction.y = 0; // Keep in XZ plane
            float distance = Mathf.Max(direction.magnitude, 30f); // Minimum length
            
            // Position at same height as position marker (50 units up)
            Vector3 arrowPos = startPos + new Vector3(0, 50f, 0);
            arrow.transform.position = arrowPos;
            arrow.transform.localScale = new Vector3(3f, distance/2f, 3f); // Thinner than position marker
            
            // Point towards target
            UpdateArrowDirection(arrow, targetPos, startPos);
            
            // Make arrow head (sphere)
            GameObject arrowHead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.Destroy(arrowHead.GetComponent<Collider>());
            arrowHead.transform.localScale = new Vector3(6f, 6f, 6f); // Slightly larger than arrow width
            arrowHead.GetComponent<Renderer>().material.color = color;
            arrowHead.name = name + "Head";
            arrowHead.transform.SetParent(arrow.transform);
            
            // Position arrow head at end of arrow
            UpdateArrowDirection(arrow, targetPos, startPos);
            
            arrow.GetComponent<Renderer>().material.color = color;
            arrow.name = name;
            return arrow;
        }

        private void UpdateArrowDirection(GameObject arrow, Vector3 targetPos, Vector3 startPos)
        {
            if (arrow == null) return;
            
            Vector3 direction = targetPos - startPos;
            direction.y = 0; // Keep in XZ plane
            if (direction != Vector3.zero)
            {
                // Point cylinder along direction (they default to Y-up)
                arrow.transform.rotation = Quaternion.LookRotation(direction.normalized) * 
                                           Quaternion.Euler(90f, 0f, 0f);
                
                // Update arrow head position if it exists
                Transform arrowHead = arrow.transform.Find(arrow.name + "Head");
                if (arrowHead != null)
                {
                    float distance = arrow.transform.localScale.y * 2f;
                    arrowHead.position = arrow.transform.position + 
                                       (direction.normalized * distance/2f);
                }
            }
        }


        public void AttachMarker(Transform transform, Vector3 localPosition, Color color, string name, float height, float diameter = 5f)
        {
            GameObject marker = CreateMarker(localPosition, color, name, height, diameter);
            marker.transform.SetParent(transform, false);
        }


        public GameObject CreateMarker(Vector3 position, Color color, string name, float height, float diameter = 5f)
        {
            GameObject waypointMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.Destroy(waypointMarker.GetComponent<Collider>());
            waypointMarker.transform.localScale = new Vector3(diameter, height, diameter);
            waypointMarker.transform.position = position;
            waypointMarker.GetComponent<Renderer>().material.color = color;
            waypointMarker.name = name;
            GameObject waypointTopMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.Destroy(waypointTopMarker.GetComponent<Collider>());
            waypointTopMarker.transform.localScale = new Vector3(diameter * 3f, diameter * 3f, diameter * 3f);
            waypointTopMarker.transform.position = position + new Vector3(0, height, 0);
            waypointTopMarker.GetComponent<Renderer>().material.color = color;
            waypointTopMarker.name = name + "Top";
            waypointTopMarker.transform.SetParent(waypointMarker.transform);
            return waypointMarker;
        }


        public GameObject ConnectMarkers(Vector3 pos1, Vector3 pos2, Color color, string name, float height, float diameter = 5f)
        {
            GameObject waypointConnector = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.Destroy(waypointConnector.GetComponent<Collider>());
            Vector3 direction = pos1 - pos2;
            float distance = direction.magnitude;
            waypointConnector.transform.position = (pos1 + pos2) / 2.0f + new Vector3(0f, height, 0f);
            waypointConnector.transform.localScale = new Vector3(diameter, distance / 2.0f, diameter);
            waypointConnector.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            waypointConnector.GetComponent<Renderer>().material.color = color;
            waypointConnector.name = name;
            return waypointConnector;
        }

        #endregion


        public void Console_OnCommand()
        {
            string command = uConsole.GetString();
            if (command == null || command.Length == 0)
            {
                LogAlways($"Supported commands: {CommandString_OnCommandSupportedTypes}");
                return;
            }
            switch (command.ToLower())
            {
                case CommandString_Help: Console_Help(); return;
                case CommandString_Create: Console_Create(); return;
                case CommandString_Delete: Console_Delete(); return;
                case CommandString_Save: Console_Save(); return;
                case CommandString_Load: Console_Load(); return;
                case CommandString_AddTo: Console_AddTo(); return;
                case CommandString_GoTo: Console_GoTo(); return;
                case CommandString_Finish: Console_Finish(); return;
                case CommandString_Show: Console_Show(); return;
                case CommandString_Hide: Console_Hide(); return;
                case CommandString_List: Console_List(); return;
                case CommandString_Paint: Console_Paint(); return;
                case "unlock":
                    GameManager.GetFeatMasterHunter().Unlock();
                    return;
            }
        }


        #region Help

        private void Console_Help()
        {
            string command = uConsole.GetString();
            if (command == null || command.Length == 0)
            {
                LogAlways($"Supported commands: {CommandString_HelpSupportedCommands}");
                return;
            }
            switch (command.ToLower())
            {
                case CommandString_Create:
                    LogAlways($"Attempts to create an object. Syntax: '{CommandString} {CommandString_Create} <type> <name>'. Supported types: {CommandString_CreateSupportedTypes}");
                    return;
                case CommandString_Delete:
                    LogAlways($"Attempts to delete an object. Syntax: '{CommandString} {CommandString_Delete} <type> <name>'. Supported types: {CommandString_DeleteSupportedTypes}");
                    return;
                case CommandString_Save:
                    LogAlways($"Attempts to save an object. Syntax: '{CommandString} {CommandString_Save} <type>'. Supported types: {CommandString_SaveSupportedTypes}");
                    return;
                case CommandString_Load:
                    LogAlways($"Attempts to load an object. Syntax: '{CommandString} {CommandString_Load} <type>'. Supported types: {CommandString_LoadSupportedTypes}");
                    return;
                case CommandString_AddTo:
                    LogAlways($"Attempts to add something to an object. Syntax: '{CommandString} {CommandString_AddTo} <type>'. Supported types: {CommandString_AddToSupportedTypes}");
                    return;
                case CommandString_GoTo:
                    LogAlways($"Attempts to teleport to an object. Syntax: '{CommandString} {CommandString_GoTo} <type> <name>'. Supporteed Types: {CommandString_GoToSupportedTypes}");
                    return;
                case CommandString_Finish:
                    LogAlways($"Attempts to finish creation of an object during a multi-step process. Syntax: '{CommandString} {CommandString_Finish} <type>'. Supported Types: {CommandString_FinishSupportedTypes}");
                    return;
                case CommandString_Show:
                    LogAlways($"Attempts to show an object. Syntax: '{CommandString} {CommandString_Show} <type> <optional name>'. Supported Types: {CommandString_ShowSupportedTypes}");
                    return;
                case CommandString_Hide:
                    LogAlways($"Attempts to hide an object. Syntax: '{CommandString} {CommandString_Hide} <type> <optional name>'. Supported Types: {CommandString_HideSupportedTypes}");
                    return;
                case CommandString_List:
                    LogAlways($"Attempts to list available objects. Syntax: '{CommandString} {CommandString_List} <type>'. Supported Types: {CommandString_ListSupportedTypes}");
                    return;
            }
        }

        #endregion


        #region Create

        private void Console_Create()
        {
            string type = uConsole.GetString();
            if (!IsTypeSupported(type, CommandString_CreateSupportedTypes))
            {
                return;
            }
            switch (type)
            {
                case CommandString_WanderPath: Console_CreateWanderPath(); return;
                case CommandString_HidingSpot: Console_CreateHidingSpot(); return;
                default: LogAlways($"{type} is supported per debug constants, but not routed! Report this please."); return;
            }
        }


        private void Console_CreateWanderPath()
        {
            if (mCurrentPaintMode != PaintMode.WanderPath)
            {
                LogWarning($"Can't start recording path because path {mCurrentDataNameIterated} is still active! enter command '{CommandString} {CommandString_Finish} {CommandString_WanderPath}' to finish current wander path.");
                return;
            }
            string name = uConsole.GetString();
            if (!IsNameProvided(name))
            {
                return;
            }
            mCurrentPaintFilePath = uConsole.GetString();
            if (!IsNameProvided(mCurrentPaintFilePath, false))
            {
                mCurrentPaintFilePath = Path.Combine(DataFolderPath, $"ExpandedAiFramework.WanderPaths.json");
            }
            string scene = mManager.CurrentScene;
            GetMapDataByNameRequest<WanderPath> tryStartNewWanderPath = new GetMapDataByNameRequest<WanderPath>(name, scene, (path, result) =>
            {
                if (result != RequestResult.Succeeded)
                {
                    mCurrentPaintMode = PaintMode.WanderPath;
                    mCurrentDataNameIterated = name;
                    Console_AddToCurrentWanderPath();
                    LogAlways($"Started wander path with name {name} at {mCurrentWanderPathPoints[0]}. Use command '{CommandString} {CommandString_AddTo} {CommandString_WanderPath}' to add more points, and '{CommandString} {CommandString_Finish} {CommandString_WanderPath} to finish the path.");
                }
                else
                {
                    LogWarning($"Can't start recording path because a path with this name exists in this scene!");
                }                
            });
            DataManager.ScheduleMapDataRequest<WanderPath>(tryStartNewWanderPath);
        }



        private void Console_CreateHidingSpot()
        {
            string name = uConsole.GetString();
            if (!IsNameProvided(name))
            {
                return;
            }
            mCurrentPaintFilePath = uConsole.GetString();
            if (!IsNameProvided(mCurrentPaintFilePath, false))
            {
                mCurrentPaintFilePath = Path.Combine(DataFolderPath, $"ExpandedAiFramework.HidingSpots.json");
            }
            string scene = mManager.CurrentScene;

            GetMapDataByNameRequest<HidingSpot> tryCreateNewHidingSpot = new GetMapDataByNameRequest<HidingSpot>(name, scene, (path, result) =>
            {
                if (result == RequestResult.Failed)
                {
                    Vector3 pos = GameManager.m_vpFPSCamera.transform.position;
                    Quaternion rot = GameManager.m_vpFPSCamera.transform.rotation;
                    AiUtils.GetClosestNavmeshPos(out Vector3 actualPos, pos, pos);
                    HidingSpot newSpot = new HidingSpot(name, actualPos, rot, mManager.CurrentScene);
                    TryRegisterNewHidingSpot(newSpot, mCurrentPaintFilePath);
                }
                else
                {
                    LogWarning($"Can't generate hiding spot {name} another spot with that name exists in this scene!");
                }
            });
            DataManager.ScheduleMapDataRequest<HidingSpot>(tryCreateNewHidingSpot);
        }


        private void TryRegisterNewHidingSpot(HidingSpot hidingSpot, string dataPath)
        {
            RegisterDataRequest<HidingSpot> tryRegisterNewHidingSpot = new RegisterDataRequest<HidingSpot>(hidingSpot, dataPath, (path, result) =>
            {
                if (result != RequestResult.Succeeded)
                {
                    LogWarning($"Failed to register new hiding spot!!!!");
                    return;
                }
                mDebugShownHidingSpots.Add(CreateMarker(hidingSpot.Position, Color.yellow, $"Hiding spot: {hidingSpot.Name}", 100.0f));
                LogAlways($"Generated hiding spot {hidingSpot.Name} at {hidingSpot.Position} with rotation {hidingSpot.Rotation} in scene {hidingSpot.Scene}!");
                SaveMapData();
            });
            DataManager.ScheduleMapDataRequest<HidingSpot>(tryRegisterNewHidingSpot);
        }

        #endregion


        #region Delete

        private void Console_Delete()
        {
            string type = uConsole.GetString();
            if (!IsTypeSupported(type, CommandString_DeleteSupportedTypes))
            {
                return;
            }
            switch (type)
            {
                case CommandString_WanderPath: Console_DeleteMapData<WanderPath>(); return;
                case CommandString_HidingSpot: Console_DeleteMapData<HidingSpot>(); return;
                default: LogAlways($"{type} is supported per debug constants, but not routed! Report this please."); return;
            }
        }

        private void Console_DeleteMapData<T>() where T : IMapData, new()
        {
            {
                string name = uConsole.GetString();
                if (!IsNameProvided(name))
                {
                    return;
                }
                string scene = mManager.CurrentScene;
                DataManager.ScheduleMapDataRequest<T>(new GetMapDataByNameRequest<T>(name, scene, (path, result) =>
                {
                    if (result != RequestResult.Succeeded)
                    {
                        LogWarning($"No such path {path} in scene {scene}!");
                        return;
                    }
                    DataManager.ScheduleMapDataRequest<T>(new DeleteDataRequest<T>(path.Guid, scene, (deletedPath, deleteResult) =>
                    {
                        LogAlways($"Deleted wander path {name} in scene {scene}.");
                        SaveMapData();
                    }));
                }));
            }
        }


        #endregion


        #region Save

        private void Console_Save()
        {
            string type = uConsole.GetString();
            if (!IsTypeSupported(type, CommandString_SaveSupportedTypes, false))
            {
                SaveMapData();
                return;
            }
            switch (type)
            {
                case CommandString_MapData: SaveMapData(); return;
                default: LogAlways($"{type} is supported per debug constants, but not routed! Report this please."); return;
            }
        }

        #endregion


        #region Load

        private void Console_Load()
        {
            string type = uConsole.GetString();
            if (!IsTypeSupported(type, CommandString_LoadSupportedTypes, false))
            {
                LoadMapData();
                return;
            }
            switch (type)
            {
                case CommandString_MapData: LoadMapData(); return;
                default: LogAlways($"{type} is supported per debug constants, but not routed! Report this please."); return;
            }
        }

        #endregion


        #region AddTo

        private void Console_AddTo()
        {
            string type = uConsole.GetString();
            if (!IsTypeSupported(type, CommandString_AddToSupportedTypes))
            {
                return;
            }
            switch (type)
            {
                case CommandString_WanderPath: Console_AddToWanderPath(); return;
                default: LogAlways($"{type} is supported per debug constants, but not routed! Report this please."); return;
            }
        }


        private void Console_AddToWanderPath()
        {
            string name = uConsole.GetString();
            if (!IsNameProvided(name, false))
            {
                Console_AddToCurrentWanderPath();
            }
            else
            {
                LogWarning($"Unfortunately I have not yet added the capacity to add wander points to existing wander paths. Check in later!");
                return;
            }
        }


        private void Console_AddToCurrentWanderPath()
        {
            Vector3 pos = GameManager.m_vpFPSCamera.transform.position;
            if (mCurrentPaintMode != PaintMode.WanderPath)
            {
                LogWarning($"Start a path first!");
                return;
            }
            AiUtils.GetClosestNavmeshPos(out Vector3 actualPos, pos, pos);
            mCurrentWanderPathPoints.Add(actualPos);
            mCurrentWanderPathPointMarkers.Add(CreateMarker(actualPos, Color.blue, $"{mCurrentDataNameIterated}.Position {mCurrentWanderPathPoints.Count} Marker", 100));
            if (mCurrentWanderPathPoints.Count > 1)
            {
                mCurrentWanderPathPointMarkers.Add(ConnectMarkers(actualPos, mCurrentWanderPathPoints[mCurrentWanderPathPoints.Count - 2], Color.blue, $"{mCurrentDataNameIterated}.Connector {mCurrentWanderPathPoints.Count - 2} -> {mCurrentWanderPathPoints.Count - 1}", 100));
            }
            LogAlways($"Added wanderpath point at {actualPos} to wanderpath {mCurrentDataNameIterated}");
        }

        #endregion


        #region Finish

        private void Console_Finish()
        {
            string type = uConsole.GetString();
            if (!IsTypeSupported(type, CommandString_FinishSupportedTypes))
            {
                return;
            }
            switch (type)
            {
                case CommandString_WanderPath: Console_FinishWanderPath(); return;
                default: LogAlways($"{type} is supported per debug constants, but not routed! Report this please."); return;
            }
        }


        private void Console_FinishWanderPath()
        {
            if (mCurrentPaintMode != PaintMode.WanderPath)
            {
                LogWarning($"Start recording a path to cancel!");
                return;
            }
            mCurrentPaintMode = PaintMode.COUNT;
            WanderPath newPath = new WanderPath(mCurrentDataNameIterated, mCurrentWanderPathPoints.ToArray(), mManager.CurrentScene);
            DataManager.ScheduleMapDataRequest<WanderPath>(new RegisterDataRequest<WanderPath>(newPath, mCurrentPaintFilePath, (registeredPath, result) =>
            {
                if (result != RequestResult.Succeeded)
                {
                    LogError($"Couldnt register new wander path!");
                    return;
                }
                LogAlways($"Generated wander path {mCurrentDataNameIterated} starting at {mCurrentWanderPathPoints[0]}.");
                mCurrentWanderPathPointMarkers.Add(ConnectMarkers(mCurrentWanderPathPoints[mCurrentWanderPathPoints.Count - 1], mCurrentWanderPathPoints[0], Color.blue, $"{mCurrentDataNameIterated}.Connector {mCurrentWanderPathPoints.Count - 1} -> {0}", 100));
                mCurrentWanderPathPoints.Clear();
                mCurrentDataNameIterated = string.Empty;
                mDebugShownWanderPaths.AddRange(mCurrentWanderPathPointMarkers);
                mCurrentWanderPathPointMarkers.Clear();
                SaveMapData();
            }));
        }

        #endregion


        #region GoTo

        private void Console_GoTo()
        {
            string type = uConsole.GetString();
            if (!IsTypeSupported(type, CommandString_GoToSupportedTypes))
            {
                return;
            }
            switch (type)
            {
                case CommandString_WanderPath: Console_GoToWanderPath(); return;
                case CommandString_HidingSpot: Console_GoToHidingSpot(); return;
                default: LogAlways($"{type} is supported per debug constants, but not routed! Report this please."); return;
            }
        }



        private void Console_GoToHidingSpot()
        {
            string name = uConsole.GetString();
            DataManager.ScheduleMapDataRequest<HidingSpot>(new GetMapDataByNameRequest<HidingSpot>(name, mManager.CurrentScene, (data, result) =>
            {
                if (result != RequestResult.Succeeded)
                {
                    LogError($"No data found with name {name}!");
                    return;
                }
                Teleport(data.Position, data.Rotation);
                LogAlways($"Teleported to {data}! Watch out for ambush wolves...");
            }));
        }


        private void Console_GoToWanderPath()
        {
            string name = uConsole.GetString();
            int pathPointIndex = 0;
            try
            {
                pathPointIndex = uConsole.GetInt();
            }
            catch
            {
                pathPointIndex = 0;
            }
            DataManager.ScheduleMapDataRequest<WanderPath>(new GetMapDataByNameRequest<WanderPath>(name, mManager.CurrentScene, (data, result) =>
            {
                if (result != RequestResult.Succeeded)
                {
                    LogError($"No data found with name {name}!");
                    return;
                }
                if (pathPointIndex >= data.PathPoints.Length)
                {
                    LogWarning($"{data} has {data.PathPoints.Length} path points, please select one in that range!");
                    return;
                }
                Quaternion lookDir = Quaternion.identity;
                if (pathPointIndex == data.PathPoints.Length - 1)
                {
                    lookDir = Quaternion.LookRotation(data.PathPoints[0] - data.PathPoints[pathPointIndex]);
                }
                else
                {
                    lookDir = Quaternion.LookRotation(data.PathPoints[pathPointIndex + 1] - data.PathPoints[pathPointIndex]);
                }
                Teleport(data.PathPoints[pathPointIndex], lookDir);
                LogAlways($"Teleported to WanderPath {data.Name} point #{pathPointIndex} at {data.PathPoints[pathPointIndex]}! Watch out for wandering wolves...");
            }));
        }


        #endregion


        #region Show

        private void Console_Show()
        {
            string type = uConsole.GetString();
            if (!IsTypeSupported(type, CommandString_ShowSupportedTypes))
            {
                return;
            }
            switch (type)
            {
                case CommandString_WanderPath: Console_ShowWanderPath(); return;
                case CommandString_HidingSpot: Console_ShowHidingSpot(); return;
                case CommandString_NavMesh: Console_ShowNavMesh(); return;
                case CommandString_SpawnRegion: Console_ShowNavMesh(); return;
                case CommandString_Ai: Console_ShowAi(); return;
                default: LogAlways($"{type} is supported per debug constants, but not routed! Report this please."); return;
            }
        }

        
        private void Console_ShowHidingSpot()
        {
            string name = uConsole.GetString();
            if (!IsNameProvided(name, false))
            {
                Console_ShowAllHidingSpots();
            }
            else
            {
                DataManager.ScheduleMapDataRequest<HidingSpot>(new GetMapDataByNameRequest<HidingSpot>(name, mManager.CurrentScene, (data, result) =>
                {
                    if (result != RequestResult.Succeeded)
                    {
                        LogError($"No hiding spot with name {name}!");
                    }
                    lock (mDebugShownHidingSpots)
                    {
                        mDebugShownHidingSpots.Add(CreateMarker(data.Position, Color.yellow, data.Name, 100));
                    }
                }));
            }
        }


        private void Console_ShowAllHidingSpots()
        {
            DataManager.ScheduleMapDataRequest<HidingSpot>(new ForEachMapDataRequest<HidingSpot>(mManager.CurrentScene, (data) =>
            {
                lock (mDebugShownHidingSpots)
                {
                    mDebugShownHidingSpots.Add(CreateMarker(data.Position, Color.yellow, data.Name, 100));
                }
            }));
        }


        private void Console_ShowWanderPath()
        {
            string name = uConsole.GetString();
            if (!IsNameProvided(name, false))
            {
                Console_ShowAllHidingSpots();
            }
            else
            {
                DataManager.ScheduleMapDataRequest<WanderPath>(new GetMapDataByNameRequest<WanderPath>(name, mManager.CurrentScene, (data, result) =>
                {
                    if (result != RequestResult.Succeeded)
                    {
                        LogError($"No hiding spot with name {name}!");
                    }
                    for (int i = 0, iMax = data.PathPoints.Length; i < iMax; i++)
                    {
                        lock (mDebugShownWanderPaths)
                        {
                            mDebugShownWanderPaths.Add(CreateMarker(data.PathPoints[i], Color.blue, data.Name, 100));
                            if (i > 0)
                            {
                                mDebugShownWanderPaths.Add(ConnectMarkers(data.PathPoints[i], data.PathPoints[i - 1], Color.blue, data.Name, 100));
                            }
                        }
                    }
                }));
            }
        }


        private void Console_ShowAllWanderPaths()
        {
            DataManager.ScheduleMapDataRequest<WanderPath>(new ForEachMapDataRequest<WanderPath>(mManager.CurrentScene, (data) =>
            {
                for (int i = 0, iMax = data.PathPoints.Length; i < iMax; i++)
                {
                    lock (mDebugShownWanderPaths)
                    {
                        mDebugShownWanderPaths.Add(CreateMarker(data.PathPoints[i], Color.blue, data.Name, 100));
                        if (i > 0)
                        {
                            mDebugShownWanderPaths.Add(ConnectMarkers(data.PathPoints[i], data.PathPoints[i - 1], Color.blue, data.Name, 100));
                        }
                    }
                }
            }));
        }


        private void Console_ShowAi()
        {
            string name = uConsole.GetString();
            if (!IsNameProvided(name, false))
            {
                Console_ShowAllAis();
            }
            else
            {
                foreach (CustomBaseAi baseAi in mManager.AiManager.CustomAis.Values)
                {
                    if ($"{baseAi.BaseAi.GetHashCode()}" == name || $"{baseAi.ModDataProxy.Guid}" == name)
                    {
                        AttachMarker(baseAi.transform, Vector3.zero, baseAi.DebugHighlightColor, "AiDebugMarker", 100);
                    }
                }
            }
        }


        private void Console_ShowAllAis()
        {
            foreach (CustomBaseAi baseAi in mManager.AiManager.CustomAis.Values)
            {
                AttachMarker(baseAi.transform, Vector3.zero, baseAi.DebugHighlightColor, "AiDebugMarker", 100);
            }
        }


        private void Console_ShowSpawnRegion()
        {
            Console_ShowAllSpawnRegions();
        }


        private void Console_ShowAllSpawnRegions()
        {
            foreach (SpawnRegion spawnRegion in GameManager.m_SpawnRegionManager.m_SpawnRegions)
            {
                if (spawnRegion == null)
                {
                    continue;
                }
                lock (mDebugShownSpawnRegions)
                {
                    mDebugShownSpawnRegions.Add(CreateMarker(spawnRegion.transform.position, GetSpawnRegionColor(spawnRegion), $"{spawnRegion.m_AiSubTypeSpawned} SpawnRegion Marker at {spawnRegion.transform.position}", 1000, 10));
                }
            }
        }


        private Color GetSpawnRegionColor(SpawnRegion spawnRegion)
        {
            switch (spawnRegion.m_AiSubTypeSpawned)
            {
                case AiSubType.Wolf: return Color.grey;
                case AiSubType.Bear: return Color.red;
                case AiSubType.Cougar: return Color.cyan;
                case AiSubType.Rabbit: return Color.blue;
                case AiSubType.Stag: return Color.yellow;
                case AiSubType.Moose: return Color.green;
                default: return Color.clear;
            }
        }

        #endregion


        #region Hide

        private void Console_Hide()
        {
            string type = uConsole.GetString();
            if (!IsTypeSupported(type, CommandString_HideSupportedTypes))
            {
                return;
            }
            switch (type)
            {
                case CommandString_WanderPath: Console_HideWanderPath(); return;
                case CommandString_HidingSpot: Console_HideHidingSpot(); return;
                case CommandString_NavMesh: Console_HideNavMesh(); return;
                case CommandString_SpawnRegion: Console_HideSpawnRegion(); return;
                case CommandString_Ai: Console_HideAi(); return;
                default: LogAlways($"{type} is supported per debug constants, but not routed! Report this please."); return;
            }
        }


        private void Console_HideHidingSpot()
        {
            string name = uConsole.GetString();
            if (!IsNameProvided(name, false))
            {
                Console_HideAllHidingSpots();
            }
            else
            {
                lock (mDebugShownHidingSpots)
                {
                    foreach (GameObject obj in mDebugShownHidingSpots)
                    {
                        if (obj != null && obj.name != null && obj.name.Contains(name))
                        {
                            mDebugShownHidingSpots.Remove(obj);
                            GameObject.Destroy(obj);
                        }
                    }
                }
            }
        }


        private void Console_HideAllHidingSpots()
        {
            lock (mDebugShownHidingSpots)
            {
                foreach (GameObject obj in mDebugShownHidingSpots)
                {
                    UnityEngine.Object.Destroy(obj);
                }
                mDebugShownHidingSpots.Clear();
            }
        }


        private void Console_HideWanderPath()
        {
            string name = uConsole.GetString();
            if (!IsNameProvided(name, false))
            {
                Console_HideAllWanderPaths();
            }
            else
            {
                lock (mDebugShownWanderPaths)
                {
                    foreach (GameObject obj in mDebugShownWanderPaths)
                    {
                        if (obj != null && obj.name != null && obj.name.Contains(name))
                        {
                            lock (mDebugShownWanderPaths)
                            {
                                mDebugShownWanderPaths.Remove(obj);
                            }
                            GameObject.Destroy(obj);
                        }
                    }
                }
            }
        }


        private void Console_HideAllWanderPaths()
        {
            lock (mDebugShownWanderPaths)
            {
                foreach (GameObject obj in mDebugShownWanderPaths)
                {
                    UnityEngine.Object.Destroy(obj);
                }
                mDebugShownWanderPaths.Clear();
                
            }
        }


        private void Console_HideSpawnRegion()
        {
            Console_HideAllSpawnRegions();
        }


        private void Console_HideAllSpawnRegions()
        {
            lock (mDebugShownSpawnRegions)
            {
                foreach (GameObject obj in mDebugShownSpawnRegions)
                {
                    UnityEngine.Object.Destroy(obj);
                }
                mDebugShownSpawnRegions.Clear();
            }
        }



        private void Console_HideAi()
        {
            string name = uConsole.GetString();
            if (!IsNameProvided(name, false))
            {
                Console_HideAllAis();
            }
            else
            {
                foreach (CustomBaseAi baseAi in mManager.AiManager.CustomAis.Values)
                {
                    if ($"{baseAi.BaseAi.GetHashCode()}" == name || $"{baseAi.ModDataProxy.Guid}" == name)
                    {
                        foreach (Transform child in baseAi.GetComponentsInChildren<Transform>(true))
                        {
                            if (child.name.Contains("AiDebugMarker"))
                            {
                                GameObject.Destroy(child.gameObject);
                                break;
                            }
                        }
                        return;
                    }
                }
            }
        }


        private void Console_HideAllAis()
        {
            foreach (CustomBaseAi baseAi in mManager.AiManager.CustomAis.Values)
            {
                foreach (Transform child in baseAi.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name.Contains("AiDebugMarker"))
                    {
                        GameObject.Destroy(child.gameObject);
                        break;
                    }
                }
            }
        }

        #endregion


        #region List

        private void Console_List()
        {
            string type = uConsole.GetString();
            if (!IsTypeSupported(type, CommandString_ListSupportedTypes))
            {
                return;
            }
            switch (type)
            {
                case CommandString_WanderPath: Console_ListWanderPaths(); return;
                case CommandString_HidingSpot: Console_ListHidingSpots(); return;
                default: LogAlways($"{type} is supported per debug constants, but not routed! Report this please."); return;
            }
        }


        private void Console_ListHidingSpots()
        {
            DataManager.ScheduleMapDataRequest<HidingSpot>(new ForEachMapDataRequest<HidingSpot>(mManager.CurrentScene, (spot) =>
            {
                LogAlways($"Found {spot}. Occupied: {spot.Claimed}");
            }));
        }


        private void Console_ListWanderPaths()
        {
            DataManager.ScheduleMapDataRequest<HidingSpot>(new ForEachMapDataRequest<WanderPath>(mManager.CurrentScene, (spot) =>
            {
                LogAlways($"Found {spot}. Occupied: {spot.Claimed}");
            }));
        }

        #endregion


        #region Paint
        //totally didnt vibe code this region or anything like that
        //and it couldnt manage to actually render a single fuckin thing. had to fix it. lmfao
        private void Console_Paint()
        {
            string type = uConsole.GetString();
            if (!IsTypeSupported(type, CommandString_PaintSupportedTypes))
            {
                return;
            }

            mCurrentDataNameBase = uConsole.GetString();
            if (!IsNameProvided(mCurrentDataNameBase, false))
            {
                mCurrentDataNameBase = null; // Auto-generate name if not provided
            }
            mCurrentPaintFilePath = uConsole.GetString();

            if (type == CommandString_HidingSpot)
            {
                DataManager.ScheduleMapDataRequest<HidingSpot>(new GetUniqueMapDataName<HidingSpot>(mManager.CurrentScene, mCurrentDataNameBase, (uniqueName) =>
                {
                    if (!InitializePaintHidingSpot(uniqueName))
                    {
                        LogWarning("Failed to initialize paint mode");
                        return;
                    }
                    LogAlways($"Entered hiding spot paint mode. Left click to select position, then left click again to set rotation. Right click to exit mode.");
                }));
            }
            else if (type == CommandString_WanderPath)
            {
                DataManager.ScheduleMapDataRequest<WanderPath>(new GetUniqueMapDataName<WanderPath>(mManager.CurrentScene, mCurrentDataNameBase, (uniqueName) =>
                {
                    if (!InitializePaintWanderPath(uniqueName))
                    {
                        LogWarning("Failed to initialize paint mode");
                        return;
                    }
                    LogAlways($"Entered wander path paint mode. Left click to place points, right click to finish a path, right click twice to exit mode.");
                }));
            }
        }





        private bool InitializePaintWanderPath(string name)
        {
            try
            {
                if (!IsNameProvided(mCurrentPaintFilePath, false))
                {
                    mCurrentPaintFilePath = Path.Combine(DataFolderPath, $"{typeof(WanderPath)}s.json");
                }
                // Clear any existing state
                CleanUpPaintMarker();
                mCurrentWanderPathPoints.Clear();
                if (mCurrentWanderPathPointMarkers != null)
                {
                    foreach (var marker in mCurrentWanderPathPointMarkers)
                    {
                        if (marker != null) UnityEngine.Object.Destroy(marker);
                    }
                    mCurrentWanderPathPointMarkers.Clear();
                }
                else
                {
                    mCurrentWanderPathPointMarkers = new List<GameObject>();
                }

                // Set new state
                mCurrentDataNameIterated = name;

                // Create initial marker
                mPaintMarker = CreateMarker(Vector3.zero, Color.green, "PaintMarker", 50f, 2f);
                if (mPaintMarker == null)
                {
                    LogWarning("Failed to create paint marker");
                    return false;
                }

                mCurrentPaintMode = PaintMode.WanderPath;
                return true;
            }
            catch (Exception e)
            {
                LogError($"Paint mode initialization failed: {e}");
                CleanUpPaintMode();
                return false;
            }
        }

        private bool InitializePaintHidingSpot(string name)
        {
            try
            {
                if (!IsNameProvided(mCurrentPaintFilePath, false))
                {
                    mCurrentPaintFilePath = Path.Combine(DataFolderPath, $"{typeof(HidingSpot)}s.json");
                }
                // Clear any existing state
                CleanUpPaintMarker();
                mCurrentWanderPathPoints.Clear();
                if (mCurrentWanderPathPointMarkers != null)
                {
                    foreach (var marker in mCurrentWanderPathPointMarkers)
                    {
                        if (marker != null) UnityEngine.Object.Destroy(marker);
                    }
                    mCurrentWanderPathPointMarkers.Clear();
                }
                else
                {
                    mCurrentWanderPathPointMarkers = new List<GameObject>();
                }

                // Set new state
                mCurrentDataNameIterated = name;
                
                // Create initial marker
                mPaintMarker = CreateMarker(Vector3.zero, Color.green, "PaintMarker", 50f, 2f);
                if (mPaintMarker == null)
                {
                    LogWarning("Failed to create paint marker");
                    return false;
                }

                mCurrentPaintMode = PaintMode.HidingSpot;
                mSelectingHidingSpotRotation = false;
                return true;
            }
            catch (Exception e)
            {
                LogError($"Paint mode initialization failed: {e}");
                CleanUpPaintMode();
                return false;
            }
        }

        private void UpdatePaintMarker()
        {
            HandlePaintInput();
            UpdatePaintMarkerInternal();
        }


        private void HandlePaintInput()
        {
            switch (mCurrentPaintMode)
            {
                case PaintMode.WanderPath: HandlePaintWanderPathInput(); return;
                case PaintMode.HidingSpot: HandlePaintHidingSpotInput(); return;
            }
        }


        private void UpdatePaintMarkerInternal()
        {
            switch (mCurrentPaintMode)
            {
                case PaintMode.WanderPath: UpdatePaintMarkerWanderPath(); return;
                case PaintMode.HidingSpot: UpdatePaintMarkerHidingSpot(); return;
            }
        }


        private void UpdatePaintMarkerWanderPath()
        {
            if (mCurrentPaintMode == PaintMode.COUNT || GameManager.m_vpFPSCamera.m_Camera == null)
            {
                CleanUpPaintMarker();
                return;
            }

            Ray ray = GameManager.m_vpFPSCamera.m_Camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, Utils.m_PhysicalCollisionLayerMask))
            {
                mPaintMarkerPosition = hit.point;
                if (mPaintMarker != null && mPaintMarker.transform != null)
                {
                    mPaintMarker.transform.position = hit.point;
                }
            }
        }


        private GameObject mRotationTargetMarker = null;

        private void UpdatePaintMarkerHidingSpot()
        {
            if (mCurrentPaintMode == PaintMode.COUNT || GameManager.m_vpFPSCamera.m_Camera == null)
            {
                CleanUpPaintMarker();
                return;
            }

            Ray ray = GameManager.m_vpFPSCamera.m_Camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, Utils.m_PhysicalCollisionLayerMask))
            {
                mPaintMarkerPosition = hit.point;
                
                if (mSelectingHidingSpotRotation)
                {
                    // Create/show rotation target marker if needed
                    if (mRotationTargetMarker == null)
                    {
                        mRotationTargetMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        UnityEngine.Object.Destroy(mRotationTargetMarker.GetComponent<Collider>());
                        mRotationTargetMarker.transform.localScale = new Vector3(3f, 3f, 3f);
                        mRotationTargetMarker.GetComponent<Renderer>().material.color = new Color(1f, 0.5f, 0f); // Orange
                        mRotationTargetMarker.name = "RotationTargetMarker";
                    }
                    
                    // Update marker position
                    mRotationTargetMarker.transform.position = hit.point;
                    
                    // Update arrow direction
                    if (mDirectionArrow != null)
                    {
                        UpdateArrowDirection(mDirectionArrow, hit.point, mPendingHidingSpotPosition);
                    }
                }
                else
                {
                    // Clean up rotation target marker if not needed
                    if (mRotationTargetMarker != null)
                    {
                        UnityEngine.Object.Destroy(mRotationTargetMarker);
                        mRotationTargetMarker = null;
                    }
                    
                    // Update position marker
                    if (mPaintMarker != null && mPaintMarker.transform != null)
                    {
                        mPaintMarker.transform.position = hit.point;
                    }
                }
            }
        }



        private void CleanUpPaintMarker()
        {
            if (mDirectionArrow != null)
            {
                UnityEngine.Object.Destroy(mDirectionArrow);
                mDirectionArrow = null;
            }
            
            if (mPaintMarker != null)
            {
                UnityEngine.Object.Destroy(mPaintMarker);
                mPaintMarker = null;
            }
            
            if (mRotationTargetMarker != null)
            {
                UnityEngine.Object.Destroy(mRotationTargetMarker);
                mRotationTargetMarker = null;
            }
        }


        private void HandlePaintWanderPathInput()
        {
            if (Input.GetMouseButtonDown(0)) // Left click
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    // Shift+Left click - discard last point
                    if (mCurrentWanderPathPoints.Count > 0)
                    {
                        GameObject marker = mCurrentWanderPathPointMarkers[^1];
                        mCurrentWanderPathPointMarkers.Remove(marker);
                        GameObject.Destroy(marker);
                        if (mCurrentWanderPathPoints.Count > 0)
                        {
                            marker = mCurrentWanderPathPointMarkers[^1];
                            mCurrentWanderPathPointMarkers.Remove(marker);
                            GameObject.Destroy(marker);
                        }
                        mCurrentWanderPathPoints.RemoveAt(mCurrentWanderPathPoints.Count - 1);
                    }
                    return;
                }

                if (mCurrentWanderPathPointMarkers == null)
                {
                    mCurrentWanderPathPointMarkers = new List<GameObject>();
                }

                if (mCurrentWanderPathPoints.Count == 0)
                {
                    // First point
                    mCurrentWanderPathPoints.Add(mPaintMarkerPosition);
                    var marker = CreateMarker(
                        mPaintMarkerPosition,
                        Color.blue,
                        $"{mCurrentDataNameIterated}.Position {mCurrentWanderPathPoints.Count} Marker",
                        100);
                    if (marker != null)
                    {
                        mCurrentWanderPathPointMarkers.Add(marker);
                    }
                }
                else
                {
                    // Additional points
                    mCurrentWanderPathPoints.Add(mPaintMarkerPosition);
                    var marker = CreateMarker(
                        mPaintMarkerPosition,
                        Color.blue,
                        $"{mCurrentDataNameIterated}.Position {mCurrentWanderPathPoints.Count} Marker",
                        100);
                    if (marker != null)
                    {
                        mCurrentWanderPathPointMarkers.Add(marker);
                    }

                    if (mCurrentWanderPathPoints.Count > 1)
                    {
                        var connector = ConnectMarkers(
                            mPaintMarkerPosition,
                            mCurrentWanderPathPoints[mCurrentWanderPathPoints.Count - 2],
                            Color.blue,
                            $"{mCurrentDataNameIterated}.Connector {mCurrentWanderPathPoints.Count - 2} -> {mCurrentWanderPathPoints.Count - 1}",
                            100);
                        if (connector != null)
                        {
                            mCurrentWanderPathPointMarkers.Add(connector);
                        }
                    }
                }
                
            }
            else if (Input.GetMouseButtonDown(1)) // Right click
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    // Shift+Right click - discard current path
                    CleanUpPaintMode();
                    LogAlways("Discarded current wander path");
                }
                else if (mCurrentWanderPathPoints.Count > 0)
                {
                    // Regular Right click - finish current path
                    ExitPaintMode(); 
                    DataManager.ScheduleMapDataRequest<WanderPath>(new GetUniqueMapDataName<WanderPath>(mManager.CurrentScene, mCurrentDataNameBase, (uniqueName) =>
                    {
                        if (!InitializePaintWanderPath(uniqueName))
                        {
                            LogWarning("Failed to initialize paint mode");
                            return;
                        }
                    }));
                }
                else
                {
                    // Exit completely if no points placed yet
                    CleanUpPaintMode();
                }
            }
        }


        private void HandlePaintHidingSpotInput()
        {
            if (Input.GetMouseButtonDown(0)) // Left click
            {
                if (mSelectingHidingSpotRotation)
                {
                    // Second click - set rotation
                    Ray ray = GameManager.m_vpFPSCamera.m_Camera.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, Utils.m_PhysicalCollisionLayerMask))
                    {
                        Vector3 direction = hit.point - mPendingHidingSpotPosition;
                        direction.y = 0; // Keep rotation only in XZ plane
                        Quaternion rotation = Quaternion.LookRotation(direction.normalized);

                        // Create the hiding spot
                        string scene = mManager.CurrentScene;
                        HidingSpot newSpot = new HidingSpot(mCurrentDataNameIterated, mPendingHidingSpotPosition, rotation, scene);

                        DataManager.ScheduleMapDataRequest<HidingSpot>(new RegisterDataRequest<HidingSpot>(newSpot, mCurrentPaintFilePath, (spot, result) =>
                        {
                            // Show marker
                            mDebugShownHidingSpots.Add(CreateMarker(newSpot.Position, Color.yellow, $"Hiding spot: {mCurrentDataNameIterated}", 100.0f));
                            LogAlways($"Created hiding spot {mCurrentDataNameIterated} at {newSpot.Position} with rotation {newSpot.Rotation}");

                            // Exit paint mode
                            mSelectingHidingSpotRotation = false;
                            RefreshPaintMode();
                            SaveMapData();
                            DataManager.ScheduleMapDataRequest<HidingSpot>(new GetUniqueMapDataName<HidingSpot>(mManager.CurrentScene, mCurrentDataNameBase, (uniqueName) =>
                            {
                                if (!InitializePaintHidingSpot(uniqueName))
                                {
                                    LogWarning("Failed to initialize paint mode");
                                    return;
                                }
                            }));
                        }));
                    }
                }
                else
                {
                    // First click for hiding spot - set position
                    mPendingHidingSpotPosition = mPaintMarkerPosition;
                    mSelectingHidingSpotRotation = true;
                    
                    // Create directional arrow marker
                    if (mPaintMarker != null && mDirectionArrow == null)
                    {
                        // Create arrow pointing from marker position to current mouse position
                        mDirectionArrow = CreateDirectionArrow(
                            mPaintMarker.transform.position,
                            mPaintMarker.transform.position + Vector3.forward, // Initial forward direction
                            Color.green,
                            "DirectionArrow");
                        
                        // Parent to paint marker so it moves with it
                        mDirectionArrow.transform.SetParent(mPaintMarker.transform);
                    }
                    
                    LogAlways($"Selected hiding spot position at {mPendingHidingSpotPosition}. Left click to set rotation.");
                }
            }
            else if (Input.GetMouseButtonDown(1)) // Right click
            {
                ExitPaintMode();
            }
        }


        private void CleanUpPaintMode()
        {
            RefreshPaintMode();
            mCurrentPaintMode = PaintMode.COUNT;
        }


        private void RefreshPaintMode()
        {
            CleanUpPaintMarker();
            mCurrentDataNameIterated = string.Empty;
        }


        private void ExitPaintMode()
        {
            if (mCurrentWanderPathPoints.Count > 1)
            {
                // Close the loop for wanderpaths
                mCurrentWanderPathPointMarkers.Add(ConnectMarkers(
                    mCurrentWanderPathPoints[mCurrentWanderPathPoints.Count - 1],
                    mCurrentWanderPathPoints[0],
                    Color.blue,
                    $"{mCurrentDataNameIterated}.Connector {mCurrentWanderPathPoints.Count - 1} -> {0}",
                    100));

                // Save the path
                string scene = mManager.CurrentScene;
                WanderPath newPath = new WanderPath(mCurrentDataNameIterated, mCurrentWanderPathPoints.ToArray(), scene);


                DataManager.ScheduleMapDataRequest<WanderPath>(new RegisterDataRequest<WanderPath>(newPath, mCurrentPaintFilePath, (spot, result) =>
                {
                    mDebugShownWanderPaths.AddRange(mCurrentWanderPathPointMarkers);
                    mCurrentWanderPathPoints.Clear();
                    if (mCurrentWanderPathPointMarkers != null)
                    {
                        mCurrentWanderPathPointMarkers.Clear();
                    }
                    SaveMapData();
                    DataManager.ScheduleMapDataRequest<WanderPath>(new GetUniqueMapDataName<WanderPath>(mManager.CurrentScene, mCurrentDataNameBase, (uniqueName) =>
                    {
                        if (!InitializePaintWanderPath(uniqueName))
                        {
                            LogWarning("Failed to initialize paint mode");
                            return;
                        }
                    }));
                }));

            }

            CleanUpPaintMode();
        }

        #endregion


        #region NavMesh Management

        private GameObject mNavmeshObj = null;

        private Color GetNavMeshColor(int layer)
        {
            switch (layer)
            {
                case 0:
                    return Color.black;
                case 1:
                    return Color.white;
                case 2:
                    return Color.red;
                case 3:
                    return Color.blue;
                case 4:
                    return Color.green;
                case 5:
                    return Color.grey;
                case 6:
                    return Color.cyan;
                case 7:
                    return Color.yellow;
                case 8:
                    return Color.magenta;
                default:
                    return new Color(100, 200, 70);
            }
        }


        private void Console_ShowNavMesh()
        {
            if (mNavmeshObj == null)
            {
                try
                {
                    var triangulation = NavMesh.CalculateTriangulation();

                    if (triangulation.vertices.Length == 0 || triangulation.indices.Length == 0)
                    {
                        LogWarning("NavMesh triangulation is empty.");
                        return;
                    }
                    

                    Vector3[] vertices = triangulation.vertices;
                    int[] rawIndices = triangulation.indices;
                    int[] areas = triangulation.areas;
                    int[] areaColorCounts = new int[31];

                    Color[] areaColors = new Color[vertices.Length]; // Vertex colors
                    List<int> validIndices = new List<int>(rawIndices.Length);

                    for (int i = 0; i < rawIndices.Length; i += 3)
                    {
                        int idx0 = rawIndices[i];
                        int idx1 = rawIndices[i + 1];
                        int idx2 = rawIndices[i + 2];

                        if (idx0 < vertices.Length && idx1 < vertices.Length && idx2 < vertices.Length)
                        {
                            validIndices.Add(idx0);
                            validIndices.Add(idx1);
                            validIndices.Add(idx2);

                            int triangleIndex = i / 3;
                            int areaType = areas[triangleIndex];

                            Color color = GetNavMeshColor(areaType); // define your color mapping
                            areaColorCounts[areaType]++;

                            // Assign the same color to all vertices of the triangle
                            areaColors[idx0] = color;
                            areaColors[idx1] = color;
                            areaColors[idx2] = color;
                        }
                        else
                        {
                            LogVerbose($"Skipping invalid triangle at index {i} (out of bounds)");
                        }
                    }

                    for (int i = 0, iMax = 31; i < iMax; i++)
                    {
                        LogAlways($"Vertices with area index {i}: {areaColorCounts[i]}");
                    }

                    Mesh mesh = new Mesh();
                    mesh.name = "LargeNavMesh";
                    mesh.name = "LargeNavMesh";
                    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Enables >65k vertices

                    mesh.vertices = vertices;
                    mesh.triangles = validIndices.ToArray();
                    mesh.colors = areaColors;
                    mesh.RecalculateNormals();

                    mNavmeshObj = new GameObject();

                    // Assign to MeshFilter
                    MeshFilter meshFilter = mNavmeshObj.AddComponent<MeshFilter>();
                    meshFilter.sharedMesh = mesh;

                    // Apply material
                    MeshRenderer renderer = mNavmeshObj.AddComponent<MeshRenderer>();

                    Material vertexColorMat = new Material(Shader.Find("Legacy Shaders/Diffuse")); // or a custom shader
                    renderer.material = vertexColorMat;
                    mNavmeshObj.name = "eafNavMeshObj";
                }
                catch (Exception e)
                {
                    LogError(e.ToString());
                    return;
                }
            }
            mNavmeshObj.SetActive(true);
        }


        private void Console_HideNavMesh()
        {
            if (mNavmeshObj != null)
            {
                mNavmeshObj.SetActive(false);
            }
        }


        #endregion
    }
}
