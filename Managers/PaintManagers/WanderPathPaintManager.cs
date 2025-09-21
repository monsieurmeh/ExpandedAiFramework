using UnityEngine;


namespace ExpandedAiFramework
{
    public class WanderPathPaintManager : MapDataPaintManager<WanderPath>
    {
        private List<Vector3> mCurrentWanderPathPoints = new List<Vector3>();
        private List<GameObject> mCurrentWanderPathPointMarkers = new List<GameObject>();
        private bool mRecordingPath = false;
        private bool mJustFinishedPath = false;
        private WanderPathTypes mWanderPathType = WanderPathTypes.IndividualPath;

        public override string TypeName => CommandString_WanderPath;

        public WanderPathPaintManager(EAFManager manager) : base(manager) { }

        public override void StartPaint(string[] args)
        {
            if (mRecordingPath)
            {
                this.LogWarningInstanced($"Already recording path {mCurrentDataName}! Use finish command first.");
                return;
            }

            string baseName = args.Length > 0 ? args[0] : "WanderPath";
            if (args.Length > 1 && !string.IsNullOrEmpty(args[1]))
            {
                mCurrentDataPath = args[1];
            }

            GetUniqueMapDataName(baseName, (uniqueName) =>
            {
                if (InitializePaintWanderPath(uniqueName))
                {
                    this.LogAlwaysInstanced($"Entered wander path paint mode. Left click to place points, right click to finish a path, right click twice to exit mode.");
                }
                else
                {
                    this.LogWarningInstanced("Failed to initialize paint mode");
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
        }


        protected override void ProcessDelete(string[] args)
        {
            if (args.Length == 0)
            {
                this.LogWarningInstanced("Delete command requires a name");
                return;
            }

            string name = args[0];
            GetMapDataByName(name, (path, result) =>
            {
                if (result != RequestResult.Succeeded)
                {
                    this.LogWarningInstanced($"No such path {name} in scene {mManager.CurrentScene}!");
                    return;
                }
                DeleteMapData(path.Guid, (deletedPath, deleteResult) =>
                {
                    this.LogAlwaysInstanced($"Deleted wander path {name} in scene {mManager.CurrentScene}.");
                    ProcessSave(new string[0]);
                });
            });
        }


        protected override void ProcessGoTo(string[] args)
        {
            if (args.Length == 0)
            {
                this.LogWarningInstanced("GoTo command requires a name");
                return;
            }

            string name = args[0];
            int pathPointIndex = args.Length > 1 ? int.Parse(args[1]) : 0;

            GetMapDataByName(name, (data, result) =>
            {
                if (result != RequestResult.Succeeded)
                {
                    this.LogErrorInstanced($"No data found with name {name}!");
                    return;
                }
                if (pathPointIndex >= data.PathPoints.Length)
                {
                    this.LogWarningInstanced($"{data} has {data.PathPoints.Length} path points, please select one in that range!");
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
                this.LogAlwaysInstanced($"Teleported to WanderPath {data.Name} point #{pathPointIndex} at {data.PathPoints[pathPointIndex]}! Watch out for wandering wolves...");
            });
        }


        protected override void ProcessPaint(string[] args)
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
                    this.LogErrorInstanced($"No wander path with name {name}!");
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
                    this.LogWarningInstanced("Failed to create paint marker");
                    return false;
                }

                mCurrentPaintMode = PaintMode.Active;
                mRecordingPath = true;
                return true;
            }
            catch (Exception e)
            {
                this.LogErrorInstanced($"Paint mode initialization failed: {e}");
                ExitPaint();
                return false;
            }
        }

        private void AddWanderPathPoint()
        {
            if (!mRecordingPath)
            {
                this.LogWarningInstanced("Cannot add wander path point - not currently recording a path");
                return;
            }
            
            AiUtils.GetClosestNavmeshPos(out Vector3 actualPos, mPaintMarkerPosition, mPaintMarkerPosition);
            mCurrentWanderPathPoints.Add(actualPos);
            mCurrentWanderPathPointMarkers.Add(CreateMarker(actualPos, Color.blue, $"{mCurrentDataName}.Position {mCurrentWanderPathPoints.Count} Marker", 100));
            
            if (mCurrentWanderPathPoints.Count > 1)
            {
                mCurrentWanderPathPointMarkers.Add(ConnectMarkers(actualPos, mCurrentWanderPathPoints[mCurrentWanderPathPoints.Count - 2], Color.blue, $"{mCurrentDataName}.Connector {mCurrentWanderPathPoints.Count - 2} -> {mCurrentWanderPathPoints.Count - 1}", 100));
            }
            
            this.LogAlwaysInstanced($"Added wanderpath point at {actualPos} to wanderpath {mCurrentDataName}");
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
            this.LogAlwaysInstanced($"Removed last wanderpath point at {removedPoint} from wanderpath {mCurrentDataName}");
        }


        private void FinishCurrentPath()
        {
            if (mCurrentWanderPathPoints.Count < 2)
            {
                this.LogWarningInstanced("Need at least 2 points to create a path");
                return;
            }

            mCurrentWanderPathPointMarkers.Add(ConnectMarkers(mCurrentWanderPathPoints[mCurrentWanderPathPoints.Count - 1], mCurrentWanderPathPoints[0], Color.blue, $"{mCurrentDataName}.Connector {mCurrentWanderPathPoints.Count - 1} -> {0}", 100));
            
            WanderPath newPath = new WanderPath(mCurrentDataName, mCurrentWanderPathPoints.ToArray(), mManager.CurrentScene, mWanderPathType);
            RegisterMapData(newPath, (registeredPath, result) =>
            {
                if (result != RequestResult.Succeeded)
                {
                    this.LogErrorInstanced("Couldn't register new wander path!");
                    return;
                }
                this.LogAlwaysInstanced($"Generated wander path {mCurrentDataName} starting at {mCurrentWanderPathPoints[0]}.");
                mDebugShownObjects.AddRange(mCurrentWanderPathPointMarkers);
                mCurrentWanderPathPoints.Clear();
                mCurrentWanderPathPointMarkers.Clear();
                mRecordingPath = false;
                ProcessSave(new string[0]);
                
                GetUniqueMapDataName(mCurrentDataNameBase ?? "WanderPath", (uniqueName) =>
                {
                    if (InitializePaintWanderPath(uniqueName))
                    {
                        this.LogAlwaysInstanced("Ready for next path. Left click to start.");
                    }
                });
            });
        }

        protected override void ProcessSetCustom(string property, string value)
        {
            switch (property)
            {
                case "wanderpathtype":
                    if (!Enum.TryParse(value, out mWanderPathType))
                    {
                        if (!int.TryParse(value, out int intValue))
                        {
                            this.LogErrorInstanced($"Invalid wander path type: {value} AND cannot parse as int");
                            return;
                        }
                        mWanderPathType = (WanderPathTypes)intValue;
                    }
                    else
                    {
                        mWanderPathType = (WanderPathTypes)Enum.Parse(typeof(WanderPathTypes), value);
                    }
                    this.LogAlwaysInstanced($"Set wander path type to {mWanderPathType}");
                    break;
                default:
                    this.LogWarningInstanced($"Unknown property: {property}");
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
            this.LogAlwaysInstanced("Discarded current wander path");
        }
    }
}
