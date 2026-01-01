using Il2CppSystem.IO;
using UnityEngine;


namespace ExpandedAiFramework
{
    public class WanderPathPaintManager : MapDataPaintManager<WanderPath>
    {
        private List<Vector3> mCurrentWanderPathPoints = new List<Vector3>();
        private List<GameObject> mCurrentWanderPathPointMarkers = new List<GameObject>();
        private bool mRecordingPath = false;
        private bool mJustFinishedPath = false;
        private WanderPathFlags mWanderPathType = WanderPath.DefaultFlags;

        public override string TypeName => CommandString_WanderPath;

        public WanderPathPaintManager(EAFManager manager) : base(manager) { }

        public override void StartPaint(IList<string> args)
        {
            if (mRecordingPath)
            {
                this.LogInstanced($"Already recording path {mCurrentDataName}! Use finish command first.", LogCategoryFlags.PaintManager);
                return;
            }

            string baseName = GetNextArg(args);
            if (string.IsNullOrEmpty(baseName))
            {
                baseName = "WanderPath";
            }
            mCurrentDataNameBase = baseName; // Store the base name for continuation
            string dataPath = GetNextArg(args);
            if (!string.IsNullOrEmpty(dataPath))
            {
                mCurrentDataPath = dataPath;
                this.LogInstanced($"Using custom data path: {mCurrentDataPath}", LogCategoryFlags.PaintManager);
            }
            GetUniqueMapDataName(baseName, (uniqueName) =>
            {
                if (InitializePaintWanderPath(uniqueName))
                {
                    this.LogInstanced($"Entered wander path paint mode. Left click to place points, right click to finish a path, right click twice to exit mode.", LogCategoryFlags.PaintManager);
                }
                else
                {
                    this.LogInstanced("Failed to initialize paint mode", LogCategoryFlags.PaintManager);
                }
            });
        }

        public override void HandlePaintInput()
        {
            if (mCurrentPaintMode != PaintMode.Active) return;
            base.HandlePaintInput();
        }

        protected override void HandleLeftClick()
        {
            mJustFinishedPath = false;
            AddWanderPathPoint();
        }

        protected override void HandleShiftLeftClick()
        {
            RemoveLastWanderPathPoint();
        }

        protected override void HandleRightClick()
        {
            if (mJustFinishedPath)
            {
                ExitPaint();
                return;
            }

            if (mCurrentWanderPathPoints.Count > 0)
            {
                FinishCurrentPath();
                mJustFinishedPath = true;
            }
            else
            {
                ExitPaint();
            }
        }

        protected override void HandleShiftRightClick()
        {
            DiscardCurrentPath();
        }

        public override void ExitPaint()
        {
            DiscardCurrentPath();
            mCurrentPaintMode = PaintMode.Inactive;
            CleanupPaintMarker();
            mManager.ConsoleCommandManager.ClearActivePaintManager(this);
        }


        protected override void ProcessDelete(IList<string> args)
        {
            string name = GetNextArg(args);
            GetMapDataByName(name, (path, result) =>
            {
                if (result != RequestResult.Succeeded)
                {
                    this.LogInstanced($"No such path {name} in scene {mManager.CurrentScene}!", LogCategoryFlags.PaintManager);
                    return;
                }
                DeleteMapData(path.Guid, (deletedPath, deleteResult) =>
                {
                    this.LogInstanced($"Deleted wander path {name} in scene {mManager.CurrentScene}.", LogCategoryFlags.PaintManager);
                    DataManager.SaveMapData();
                });
            });
        }


        protected override void ProcessGoTo(IList<string> args)
        {
            string name = GetNextArg(args);
            string pathPointIndexStr = GetNextArg(args);
            int pathPointIndex = string.IsNullOrEmpty(pathPointIndexStr) ? 0 : int.Parse(pathPointIndexStr);

            GetMapDataByName(name, (data, result) =>
            {
                if (result != RequestResult.Succeeded)
                {
                    this.ErrorInstanced($"No data found with name {name}!");
                    return;
                }
                if (pathPointIndex >= data.PathPoints.Length)
                {
                    this.LogInstanced($"{data} has {data.PathPoints.Length} path points, please select one in that range!", LogCategoryFlags.PaintManager);
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
                this.LogInstanced($"Teleported to WanderPath {data.Name} point #{pathPointIndex} at {data.PathPoints[pathPointIndex]}! Watch out for wandering wolves...", LogCategoryFlags.PaintManager);
            });
        }


        protected override void ProcessPaint(IList<string> args)
        {
            StartPaint(args);
        }

        protected override void ShowAll()
        {
            ForEachMapData((data) =>
            {
                for (int i = 0, iMax = data.PathPoints.Length; i < iMax; i++)
                {
                    lock (mDebugShownObjects)
                    {
                        mDebugShownObjects.Add(CreateMarker(data.PathPoints[i], Color.blue, data.Name, 100));
                        if (i > 0)
                        {
                            mDebugShownObjects.Add(ConnectMarkers(data.PathPoints[i], data.PathPoints[i - 1], Color.blue, data.Name, 100));
                        }
                    }
                }
            });
        }

        protected override void ShowByName(string name)
        {
            GetMapDataByName(name, (data, result) =>
            {
                if (result != RequestResult.Succeeded)
                {
                    this.ErrorInstanced($"No wander path with name {name}!");
                    return;
                }
                for (int i = 0, iMax = data.PathPoints.Length; i < iMax; i++)
                {
                    lock (mDebugShownObjects)
                    {
                        mDebugShownObjects.Add(CreateMarker(data.PathPoints[i], Color.blue, data.Name, 100));
                        if (i > 0)
                        {
                            mDebugShownObjects.Add(ConnectMarkers(data.PathPoints[i], data.PathPoints[i - 1], Color.blue, data.Name, 100));
                        }
                    }
                }
            });
        }

        protected override void HideAll()
        {
            lock (mDebugShownObjects)
            {
                foreach (GameObject obj in mDebugShownObjects)
                {
                    UnityEngine.Object.Destroy(obj);
                }
                mDebugShownObjects.Clear();
            }
        }

        protected override void HideByName(string name)
        {
            lock (mDebugShownObjects)
            {
                foreach (GameObject obj in mDebugShownObjects)
                {
                    if (obj != null && obj.name != null && obj.name.Contains(name))
                    {
                        mDebugShownObjects.Remove(obj);
                        GameObject.Destroy(obj);
                    }
                }
            }
        }

        private bool InitializePaintWanderPath(string name)
        {
            try
            {
                CleanupPaintMarker();
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

                mCurrentDataName = name;
                mPaintMarker = CreateMarker(Vector3.zero, Color.green, "PaintMarker", 50f, 2f);
                if (mPaintMarker == null)
                {
                    this.LogInstanced("Failed to create paint marker");
                    return false;
                }

                mCurrentPaintMode = PaintMode.Active;
                mRecordingPath = true;
                return true;
            }
            catch (Exception e)
            {
                this.ErrorInstanced($"Paint mode initialization failed: {e}");
                ExitPaint();
                return false;
            }
        }

        private void AddWanderPathPoint()
        {
            if (!mRecordingPath)
            {
                this.LogInstanced("Cannot add wander path point - not currently recording a path");
                return;
            }
            
            AiUtils.GetClosestNavmeshPos(out Vector3 actualPos, mPaintMarkerPosition, mPaintMarkerPosition);
            mCurrentWanderPathPoints.Add(actualPos);
            mCurrentWanderPathPointMarkers.Add(CreateMarker(actualPos, Color.blue, $"{mCurrentDataName}.Position {mCurrentWanderPathPoints.Count} Marker", 100));
            
            if (mCurrentWanderPathPoints.Count > 1)
            {
                mCurrentWanderPathPointMarkers.Add(ConnectMarkers(actualPos, mCurrentWanderPathPoints[mCurrentWanderPathPoints.Count - 2], Color.blue, $"{mCurrentDataName}.Connector {mCurrentWanderPathPoints.Count - 2} -> {mCurrentWanderPathPoints.Count - 1}", 100));
            }
            
            this.LogInstanced($"Added wanderpath point at {actualPos} to wanderpath {mCurrentDataName}", LogCategoryFlags.PaintManager);
        }

        private void RemoveLastWanderPathPoint()
        {
            if (!mRecordingPath || mCurrentWanderPathPoints.Count == 0) return;

            GameObject marker = mCurrentWanderPathPointMarkers[^1];
            mCurrentWanderPathPointMarkers.Remove(marker);
            GameObject.Destroy(marker);
            
            if (mCurrentWanderPathPoints.Count > 1)
            {
                marker = mCurrentWanderPathPointMarkers[^1];
                mCurrentWanderPathPointMarkers.Remove(marker);
                GameObject.Destroy(marker);
            }
            
            Vector3 removedPoint = mCurrentWanderPathPoints[^1];
            mCurrentWanderPathPoints.RemoveAt(mCurrentWanderPathPoints.Count - 1);
            this.LogInstanced($"Removed last wanderpath point at {removedPoint} from wanderpath {mCurrentDataName}", LogCategoryFlags.PaintManager);
        }


        private void FinishCurrentPath()
        {
            if (mCurrentWanderPathPoints.Count < 2)
            {
                this.LogInstanced("Need at least 2 points to create a path");
                return;
            }

            mCurrentWanderPathPointMarkers.Add(ConnectMarkers(mCurrentWanderPathPoints[mCurrentWanderPathPoints.Count - 1], mCurrentWanderPathPoints[0], Color.blue, $"{mCurrentDataName}.Connector {mCurrentWanderPathPoints.Count - 1} -> {0}", 100));
            
            WanderPath newPath = new WanderPath(mCurrentDataName, mCurrentWanderPathPoints.ToArray(), mManager.CurrentScene, mWanderPathType);
            RegisterMapData(newPath, (registeredPath, result) =>
            {
                if (result != RequestResult.Succeeded)
                {
                    this.ErrorInstanced("Couldn't register new wander path!");
                    return;
                }
                this.LogInstanced($"Generated wander path {mCurrentDataName} starting at {mCurrentWanderPathPoints[0]}.", LogCategoryFlags.PaintManager);
                mDebugShownObjects.AddRange(mCurrentWanderPathPointMarkers);
                mCurrentWanderPathPoints.Clear();
                mCurrentWanderPathPointMarkers.Clear();
                mRecordingPath = false;
                DataManager.SaveMapData();

                GetUniqueMapDataName(mCurrentDataNameBase ?? "WanderPath", (uniqueName) =>
                {
                    if (InitializePaintWanderPath(uniqueName))
                    {
                        this.LogInstanced("Ready for next path. Left click to start.");
                    }
                });
            });
        }

        protected override void ProcessSetCustom(string property, string value, IList<string> args)
        {
            switch (property)
            {
                case "wanderpathtype":
                    try
                    {
                        uint tryParseFromString = uint.Parse(value);
                        mWanderPathType = (WanderPathFlags)tryParseFromString;
                    }
                    catch (Exception e)
                    {
                        this.ErrorInstanced($"Could not parse uint from wanderpathtype input ({e})");
                        return;
                    }
                    this.LogInstanced($"Set wander path type to {mWanderPathType}", LogCategoryFlags.PaintManager);
                    break;
                default:
                    this.LogInstanced($"Unknown property: {property}", LogCategoryFlags.PaintManager);
                    break;
            }
        }


        private void DiscardCurrentPath()
        {
            if (mCurrentWanderPathPointMarkers != null)
            {
                foreach (var marker in mCurrentWanderPathPointMarkers)
                {
                    if (marker != null) UnityEngine.Object.Destroy(marker);
                }
                mCurrentWanderPathPointMarkers.Clear();
            }
            mCurrentWanderPathPoints.Clear();
            mRecordingPath = false;
            CleanupPaintMarker();
            this.LogInstanced("Discarded current wander path");
        }
    }
}
