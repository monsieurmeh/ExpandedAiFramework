

using UnityEngine.AI;
using UnityEngine;
using Il2Cpp;
using System.Xml.Linq;

namespace ExpandedAiFramework
{
    public sealed class ConsoleCommandManager : BaseSubManager
    {
        private enum PaintMode : int
        {
            WanderPath = 0,
            HidingSpot = 1,
            COUNT
        }

        private PaintMode mCurrentPaintMode = PaintMode.COUNT;
        private bool mSelectingHidingSpotRotation = false;
        private Vector3 mPendingHidingSpotPosition;
        private string mCurrentWanderPathName = string.Empty;
        private List<Vector3> mCurrentWanderPathPoints = new List<Vector3>();
        private List<GameObject> mCurrentWanderPathPointMarkers = new List<GameObject>();
        private GameObject mPaintMarker = null;
        private Vector3 mPaintMarkerPosition = Vector3.zero;
        private Quaternion mPaintMarkerRotation = Quaternion.identity;
        private List<GameObject> mDebugShownHidingSpots = new List<GameObject>();
        private List<GameObject> mDebugShownWanderPaths = new List<GameObject>();
        private List<GameObject> mDebugShownSpawnRegions = new List<GameObject>();

        public ConsoleCommandManager(EAFManager manager, ISubManager[] subManagers) : base(manager, subManagers) { }


        public override void OnInitializedScene(string sceneName)
        {
            mCurrentPaintMode = PaintMode.COUNT;
            mCurrentWanderPathName = string.Empty;
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
        private Dictionary<string, List<HidingSpot>> HidingSpots => mManager.DataManager.HidingSpots;
        private Dictionary<string, List<WanderPath>> WanderPaths => mManager.DataManager.WanderPaths;
        private List<HidingSpot> AvailableHidingSpots => mManager.DataManager.AvailableHidingSpots;
        private List<WanderPath> AvailableWanderPaths => mManager.DataManager.AvailableWanderPaths;


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
                LogWarning($"Can't start recording path because path {mCurrentWanderPathName} is still active! enter command '{CommandString} {CommandString_Finish} {CommandString_WanderPath}' to finish current wander path.");
                return;
            }
            string name = uConsole.GetString();
            if (!IsNameProvided(name))
            {
                return;
            }
            string scene = GameManager.m_ActiveScene;
            if (!WanderPaths.TryGetValue(scene, out List<WanderPath> paths))
            {
                paths = new List<WanderPath>();
                WanderPaths.Add(scene, paths);
            }
            for (int i = 0, iMax = paths.Count; i < iMax; i++)
            {
                if (paths[i].Name == name)
                {
                    LogWarning($"Can't start recording path because a path with this name exists in this scene!");
                    return;
                }
            }
            mCurrentPaintMode = PaintMode.WanderPath;
            mCurrentWanderPathName = name;
            Console_AddToCurrentWanderPath();
            LogAlways($"Started wander path with name {name} at {mCurrentWanderPathPoints[0]}. Use command '{CommandString} {CommandString_AddTo} {CommandString_WanderPath}' to add more points, and '{CommandString} {CommandString_Finish} {CommandString_WanderPath} to finish the path.");
        }



        private void Console_CreateHidingSpot()
        {
            string name = uConsole.GetString();
            if (!IsNameProvided(name))
            {
                return;
            }
            string scene = GameManager.m_ActiveScene;
            if (!HidingSpots.TryGetValue(scene, out List<HidingSpot> spots))
            {
                spots = new List<HidingSpot>();
                HidingSpots.Add(scene, spots);
            }
            for (int i = 0, iMax = spots.Count; i < iMax; i++)
            {
                if (spots[i].Name == name)
                {
                    LogWarning($"Can't generate hiding spot {name} another spot with that name exists in this scene!");
                    return;
                }
            }
            Vector3 pos = GameManager.m_vpFPSCamera.transform.position;
            Quaternion rot = GameManager.m_vpFPSCamera.transform.rotation;

            AiUtils.GetClosestNavmeshPos(out Vector3 actualPos, pos, pos);
            HidingSpots[scene].Add(new HidingSpot(name, actualPos, rot, GameManager.m_ActiveScene));
            mDebugShownHidingSpots.Add(CreateMarker(actualPos, Color.yellow, $"Hiding spot: {name}", 100.0f));
            LogAlways($"Generated hiding spot {name} at {actualPos} with rotation {rot} in scene {scene}!");
            SaveMapData();
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
                case CommandString_WanderPath: Console_DeleteWanderPath(); return;
                case CommandString_HidingSpot: Console_DeleteHidingSpot(); return;
                default: LogAlways($"{type} is supported per debug constants, but not routed! Report this please."); return;
            }
        }


        private void Console_DeleteWanderPath()
        {
            string name = uConsole.GetString();
            if (!IsNameProvided(name))
            {
                return;
            }
            string scene = GameManager.m_ActiveScene;
            if (!WanderPaths.TryGetValue(scene, out List<WanderPath> paths))
            {
                LogWarning($"No paths found in scene!");
                return;
            }
            for (int i = 0, iMax = paths.Count; i < iMax; i++)
            {
                if (paths[i].Name == name)
                {
                    LogAlways($"Deleting wander path {name} in scene {scene}.");
                    WanderPaths[scene].RemoveAt(i);
                    SaveMapData();
                    return;
                }
            }
            LogWarning($"No path matching name {name} found in scene {scene}!");
        }

        private void Console_DeleteHidingSpot()
        {
            string name = uConsole.GetString();
            if (!IsNameProvided(name))
            {
                return;
            }
            string scene = GameManager.m_ActiveScene;
            if (!HidingSpots.TryGetValue(scene, out List<HidingSpot> spots))
            {
                LogWarning($"No paths found in scene!");
                return;
            }
            for (int i = 0, iMax = spots.Count; i < iMax; i++)
            {
                if (spots[i].Name == name)
                {
                    LogAlways($"Deleting hiding spot {name} in scene {scene}.");
                    HidingSpots[scene].RemoveAt(i);
                    SaveMapData();
                    return;
                }
            }
            LogWarning($"No spot matching name {name} found in scene {scene}!");
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
            mCurrentWanderPathPointMarkers.Add(CreateMarker(actualPos, Color.blue, $"{mCurrentWanderPathName}.Position {mCurrentWanderPathPoints.Count} Marker", 100));
            if (mCurrentWanderPathPoints.Count > 1)
            {
                mCurrentWanderPathPointMarkers.Add(ConnectMarkers(actualPos, mCurrentWanderPathPoints[mCurrentWanderPathPoints.Count - 2], Color.blue, $"{mCurrentWanderPathName}.Connector {mCurrentWanderPathPoints.Count - 2} -> {mCurrentWanderPathPoints.Count - 1}", 100));
            }
            LogAlways($"Added wanderpath point at {actualPos} to wanderpath {mCurrentWanderPathName}");
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
            WanderPaths[GameManager.m_ActiveScene].Add(new WanderPath(mCurrentWanderPathName, mCurrentWanderPathPoints.ToArray(), GameManager.m_ActiveScene));
            LogAlways($"Generated wander path {mCurrentWanderPathName} starting at {mCurrentWanderPathPoints[0]}.");
            mCurrentWanderPathPointMarkers.Add(ConnectMarkers(mCurrentWanderPathPoints[mCurrentWanderPathPoints.Count - 1], mCurrentWanderPathPoints[0], Color.blue, $"{mCurrentWanderPathName}.Connector {mCurrentWanderPathPoints.Count - 1} -> {0}", 100));
            mCurrentWanderPathPoints.Clear();
            mCurrentWanderPathName = string.Empty;
            mDebugShownWanderPaths.AddRange(mCurrentWanderPathPointMarkers);
            mCurrentWanderPathPointMarkers.Clear();
            SaveMapData();
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
            if (!HidingSpots.TryGetValue(GameManager.m_ActiveScene, out List<HidingSpot> spots))
            {
                LogWarning("No hiding spots in active scene!");
                return;
            }
            int spotIndex = -1;
            string spotName = uConsole.GetString();
            for (int i = 0, iMax = spots.Count; i < iMax; i++)
            {
                if (spots[i].Name == spotName)
                {
                    spotIndex = i;
                    break;
                }
            }
            if (spotIndex == -1)
            {
                LogWarning($"Could not locate hiding spot with name {spotName}!");
                return;
            }
            Teleport(spots[spotIndex].Position, spots[spotIndex].Rotation);
            LogAlways($"Teleported to {spots[spotIndex]}! Watch out for ambush wolves...");
        }


        private void Console_GoToWanderPath()
        {
            if (!WanderPaths.TryGetValue(GameManager.m_ActiveScene, out List<WanderPath> paths))
            {
                LogWarning("No wander paths in active scene!");
                return;
            }
            int pathIndex = -1;
            string pathName = uConsole.GetString();
            for (int i = 0, iMax = paths.Count; i < iMax; i++)
            {
                if (paths[i].Name == pathName)
                {
                    pathIndex = i;
                    break;
                }
            }
            if (pathIndex == -1)
            {
                LogWarning($"Could not locate wander path with name {pathName}!");
                return;
            }
            int pathPointIndex = 0;

            try
            {
                pathPointIndex = uConsole.GetInt();
            }
            catch (Exception e)
            {
                LogError(e.ToString());
            }

            if (pathPointIndex >= paths[pathIndex].PathPoints.Length)
            {
                LogWarning($"{paths[pathIndex]} has {paths[pathIndex].PathPoints.Length} path points, please select one in that range!");
                return;
            }
            Quaternion lookDir = Quaternion.identity;
            if (pathPointIndex == paths[pathIndex].PathPoints.Length - 1)
            {
                lookDir = Quaternion.LookRotation(paths[pathIndex].PathPoints[0] - paths[pathIndex].PathPoints[pathPointIndex]);
            }
            else
            {
                lookDir = Quaternion.LookRotation(paths[pathIndex].PathPoints[pathPointIndex + 1] - paths[pathIndex].PathPoints[pathPointIndex]);
            }
            Teleport(paths[pathIndex].PathPoints[pathPointIndex], lookDir);
            LogAlways($"Teleported to WanderPath {paths[pathIndex].Name} point #{pathPointIndex} at {paths[pathIndex].PathPoints[pathPointIndex]}! Watch out for wandering wolves...");
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
                default: LogAlways($"{type} is supported per debug constants, but not routed! Report this please."); return;
            }
        }


        private void Console_ShowHidingSpot()
        {
            if (!HidingSpots.TryGetValue(GameManager.m_ActiveScene, out List<HidingSpot> spots))
            {
                LogWarning("No hiding spots found in active scene!");
                return;
            }
            string name = uConsole.GetString();
            if (!IsNameProvided(name, false))
            {
                Console_ShowAllHidingSpots();
            }
            else
            {
                foreach (HidingSpot spot in spots)
                {
                    if (spot.Name == name)
                    {
                        mDebugShownHidingSpots.Add(CreateMarker(spot.Position, Color.yellow, spot.Name, 100));
                        return;
                    }
                }
            }
        }


        private void Console_ShowAllHidingSpots()
        {
            if (!HidingSpots.TryGetValue(GameManager.m_ActiveScene, out List<HidingSpot> spots))
            {
                return;
            }
            foreach (HidingSpot spot in spots)
            {
                mDebugShownHidingSpots.Add(CreateMarker(spot.Position, Color.yellow, spot.Name, 100));
            }
        }


        private void Console_ShowWanderPath()
        {
            if (!WanderPaths.TryGetValue(GameManager.m_ActiveScene, out List<WanderPath> paths))
            {
                LogWarning("No wander paths found in active scene!");
                return;
            }
            string name = uConsole.GetString();
            if (!IsNameProvided(name, false))
            {
                Console_ShowAllWanderPaths();
            }
            else
            {
                foreach (WanderPath path in paths)
                {
                    if (path.Name == name)
                    {
                        for (int i = 0, iMax = path.PathPoints.Length; i < iMax; i++)
                        {
                            mDebugShownWanderPaths.Add(CreateMarker(path.PathPoints[i], Color.blue, path.Name, 100));
                            if (i > 0)
                            {
                                mDebugShownWanderPaths.Add(ConnectMarkers(path.PathPoints[i], path.PathPoints[i - 1], Color.blue, path.Name, 100));
                            }
                        }
                    }
                }
            }
        }


        private void Console_ShowAllWanderPaths()
        {
            if (!WanderPaths.TryGetValue(GameManager.m_ActiveScene, out List<WanderPath> paths))
            {
                return;
            }
            foreach (WanderPath path in paths)
            {
                for (int i = 0, iMax = path.PathPoints.Length; i < iMax; i++)
                {
                    mDebugShownWanderPaths.Add(CreateMarker(path.PathPoints[i], Color.blue, path.Name, 100));
                    if (i > 0)
                    {
                        mDebugShownWanderPaths.Add(ConnectMarkers(path.PathPoints[i], path.PathPoints[i - 1], Color.blue, path.Name, 100));
                    }
                }
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
                mDebugShownSpawnRegions.Add(CreateMarker(spawnRegion.transform.position, GetSpawnRegionColor(spawnRegion), $"{spawnRegion.m_AiSubTypeSpawned} SpawnRegion Marker at {spawnRegion.transform.position}", 1000, 10));
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
                default: LogAlways($"{type} is supported per debug constants, but not routed! Report this please."); return;
            }
        }


        private void Console_HideHidingSpot()
        {
            if (!HidingSpots.TryGetValue(GameManager.m_ActiveScene, out List<HidingSpot> spots))
            {
                LogWarning("No hiding spots found in active scene!");
                return;
            }
            string name = uConsole.GetString();
            if (!IsNameProvided(name, false))
            {
                Console_HideAllHidingSpots();
            }
            else
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


        private void Console_HideAllHidingSpots()
        {
            foreach (GameObject obj in mDebugShownHidingSpots)
            {
                UnityEngine.Object.Destroy(obj);
            }
            mDebugShownHidingSpots.Clear();
        }


        private void Console_HideWanderPath()
        {
            if (!WanderPaths.TryGetValue(GameManager.m_ActiveScene, out List<WanderPath> paths))
            {
                LogWarning("No wander paths found in active scene!");
                return;
            }
            string name = uConsole.GetString();
            if (!IsNameProvided(name, false))
            {
                Console_HideAllWanderPaths();
            }
            else
            {
                foreach (GameObject obj in mDebugShownWanderPaths)
                {
                    if (obj != null && obj.name != null && obj.name.Contains(name))
                    {
                        mDebugShownWanderPaths.Remove(obj);
                        GameObject.Destroy(obj);
                    }
                }
            }
        }


        private void Console_HideAllWanderPaths()
        {
            foreach (GameObject obj in mDebugShownWanderPaths)
            {
                UnityEngine.Object.Destroy(obj);
            }
            mDebugShownWanderPaths.Clear();
        }


        private void Console_HideSpawnRegion()
        {
            Console_HideAllSpawnRegions();
        }


        private void Console_HideAllSpawnRegions()
        {
            foreach (GameObject obj in mDebugShownSpawnRegions)
            {
                UnityEngine.Object.Destroy(obj);
            }
            mDebugShownSpawnRegions.Clear();
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
            if (!HidingSpots.TryGetValue(GameManager.m_ActiveScene, out List<HidingSpot> spots))
            {
                LogAlways("No hiding spots found in active scene.");
                return;
            }
            foreach (HidingSpot spot in HidingSpots[GameManager.m_ActiveScene])
            {
                if (spot.Scene == GameManager.m_ActiveScene)
                {
                    LogAlways($"Found {spot}. Occupied: {!AvailableHidingSpots.Contains(spot)}");
                }
            }
        }


        private void Console_ListWanderPaths()
        {
            if (!WanderPaths.TryGetValue(GameManager.m_ActiveScene, out List<WanderPath> paths))
            {
                LogAlways("No wander paths found in active scene.");
                return;
            }
            foreach (WanderPath path in WanderPaths[GameManager.m_ActiveScene])
            {
                if (path.Scene == GameManager.m_ActiveScene)
                {
                    LogAlways($"Found {path}. Occupied: {!AvailableWanderPaths.Contains(path)}");
                }
            }
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

            string name = uConsole.GetString();
            if (!IsNameProvided(name, false))
            {
                name = null; // Auto-generate name if not provided
            }

            if (type == CommandString_HidingSpot)
            {
                if (!InitializePaintHidingSpot(name ?? "HidingSpot")) //AIDER: Implement a "auto new name" that checks current hiding spots and creats a new "ExpressHidingSpot" with a number after as needed, similar to windows auto-renaming new files to avoid overwrites
                {
                    LogWarning("Failed to initialize paint mode");
                    return;
                }
                LogAlways($"Entered hiding spot paint mode. Left click to select position, then left click again to set rotation.");
            }
            else if (type == CommandString_WanderPath)
            {
                if (!InitializePaintWanderPath(name ?? "WanderPath"))//AIDER: Implement a "auto new name" that checks current wander paths and creats a new "ExpressHidingSpot" with a number after as needed, similar to windows auto-renaming new files to avoid overwrites
                {
                    LogWarning("Failed to initialize paint mode");
                    return;
                }
                LogAlways($"Entered wander path paint mode for {name}. Left click to place points, right click to finish.");
            }
        }

        private bool mIsPaintModeInitialized = false;


        private bool InitializePaintWanderPath(string name)
        {
            try
            {
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
                mCurrentWanderPathName = name;

                // Create initial marker
                mPaintMarker = CreateMarker(Vector3.zero, Color.green, "PaintMarker", 50f, 2f);
                if (mPaintMarker == null)
                {
                    LogWarning("Failed to create paint marker");
                    return false;
                }

                mCurrentPaintMode = PaintMode.WanderPath;
                mIsPaintModeInitialized = true;
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
                mCurrentWanderPathName = name;
                
                // Create initial marker
                mPaintMarker = CreateMarker(Vector3.zero, Color.green, "PaintMarker", 50f, 2f);
                if (mPaintMarker == null)
                {
                    LogWarning("Failed to create paint marker");
                    return false;
                }

                mCurrentPaintMode = PaintMode.HidingSpot;
                mSelectingHidingSpotRotation = false;
                mIsPaintModeInitialized = true;
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
                    //AIDER: Add logic for rotating upper arrow-shaped marker in XZ direction of raycast hit above, it is temporary so it should be added already and turned into the "current" marker for this step
                }
                else
                {
                    if (mPaintMarker != null && mPaintMarker.transform != null)
                    {
                        mPaintMarker.transform.position = hit.point;
                    }
                }
            }
        }



        private void CleanUpPaintMarker()
        {
            if (mPaintMarker != null)
            {
                UnityEngine.Object.Destroy(mPaintMarker);
                mPaintMarker = null;
            }
        }


        private void HandlePaintWanderPathInput()
        {
            if (Input.GetMouseButtonDown(0)) // Left click
            {
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
                        $"{mCurrentWanderPathName}.Position {mCurrentWanderPathPoints.Count} Marker",
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
                        $"{mCurrentWanderPathName}.Position {mCurrentWanderPathPoints.Count} Marker",
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
                            $"{mCurrentWanderPathName}.Connector {mCurrentWanderPathPoints.Count - 2} -> {mCurrentWanderPathPoints.Count - 1}",
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
                ExitPaintMode();
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
                        string scene = GameManager.m_ActiveScene;
                        if (!HidingSpots.TryGetValue(scene, out List<HidingSpot> spots))
                        {
                            spots = new List<HidingSpot>();
                            HidingSpots.Add(scene, spots);
                        }

                        string name = $"HidingSpot_{spots.Count + 1}";
                        HidingSpot newSpot = new HidingSpot(name, mPendingHidingSpotPosition, rotation, scene);
                        spots.Add(newSpot);
                            
                        // Show marker
                        mDebugShownHidingSpots.Add(CreateMarker(newSpot.Position, Color.yellow, $"Hiding spot: {name}", 100.0f));
                        LogAlways($"Created hiding spot {name} at {newSpot.Position} with rotation {newSpot.Rotation}");

                        // Exit paint mode
                        mSelectingHidingSpotRotation = false;
                        CleanUpPaintMode();
                        SaveMapData();
                    }
                }
                else
                {
                    // First click for hiding spot - set position
                    mPendingHidingSpotPosition = mPaintMarkerPosition;
                    mSelectingHidingSpotRotation = true;
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
            CleanUpPaintMarker();
            mCurrentPaintMode = PaintMode.COUNT;
            mIsPaintModeInitialized = false;
            mCurrentWanderPathName = string.Empty;
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
                    $"{mCurrentWanderPathName}.Connector {mCurrentWanderPathPoints.Count - 1} -> {0}",
                    100));

                // Save the path
                string scene = GameManager.m_ActiveScene;
                if (!WanderPaths.TryGetValue(scene, out List<WanderPath> paths))
                {
                    paths = new List<WanderPath>();
                    WanderPaths.Add(scene, paths);
                }
                paths.Add(new WanderPath(mCurrentWanderPathName, mCurrentWanderPathPoints.ToArray(), scene));
                SaveMapData();
            }
            mDebugShownWanderPaths.AddRange(mCurrentWanderPathPointMarkers);

            mCurrentWanderPathPoints.Clear();
            if (mCurrentWanderPathPointMarkers != null)
            {
                mCurrentWanderPathPointMarkers.Clear();
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
