using UnityEngine;


namespace ExpandedAiFramework
{
    public class HidingSpotPaintManager : MapDataPaintManager<HidingSpot>
    {
        private bool mSelectingRotation = false;
        private Vector3 mPendingPosition;
        private GameObject mDirectionArrow = null;
        private GameObject mRotationTargetMarker = null;

        public override string TypeName => CommandString_HidingSpot;

        public HidingSpotPaintManager(EAFManager manager) : base(manager) { }

        public override void StartPaint(IList<string> args)
        {
            string baseName = GetNextArg(args);
            if (string.IsNullOrEmpty(baseName))
            {
                baseName = "HidingSpot";
            }
            string dataPath = GetNextArg(args);
            if (!string.IsNullOrEmpty(dataPath))
            {
                mCurrentDataPath = dataPath;
                this.LogAlwaysInstanced($"Using custom data path: {mCurrentDataPath}", LogCategoryFlags.PaintManager);
            }
            mCurrentDataNameBase = baseName; // Store the base name for continuation
            GetUniqueMapDataName(baseName, (uniqueName) =>
            {
                if (InitializePaintHidingSpot(uniqueName))
                {
                    this.LogAlwaysInstanced("Entered hiding spot paint mode. Left click to select position, then left click again to set rotation. Right click to exit mode.", LogCategoryFlags.PaintManager);
                }
                else
                {
                    this.LogWarningInstanced("Failed to initialize paint mode", LogCategoryFlags.PaintManager);
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
            if (mSelectingRotation)
            {
                CreateHidingSpotWithRotation();
            }
            else
            {
                SelectHidingSpotPosition();
            }
        }

        protected override void HandleShiftLeftClick()
        {
            if (mSelectingRotation)
            {
                RevertToPositionSelection();
            }
        }

        protected override void HandleRightClick()
        {
            ExitPaint();
        }

        public override void ExitPaint()
        {
            CleanupDirectionMarkers();
            mSelectingRotation = false;
            mCurrentPaintMode = PaintMode.Inactive;
            CleanupPaintMarker();
            mManager.ConsoleCommandManager.ClearActivePaintManager(this);
        }

        protected override void UpdatePaintMarkerInternal()
        {
            if (mCurrentPaintMode == PaintMode.Inactive || GameManager.m_vpFPSCamera.m_Camera == null)
            {
                CleanupPaintMarker();
                return;
            }

            Ray ray = GameManager.m_vpFPSCamera.m_Camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, Utils.m_PhysicalCollisionLayerMask))
            {
                mPaintMarkerPosition = hit.point;
                
                if (mSelectingRotation)
                {
                    if (mRotationTargetMarker == null)
                    {
                        mRotationTargetMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        UnityEngine.Object.Destroy(mRotationTargetMarker.GetComponent<Collider>());
                        mRotationTargetMarker.transform.localScale = new Vector3(3f, 3f, 3f);
                        mRotationTargetMarker.GetComponent<Renderer>().material.color = new Color(1f, 0.5f, 0f);
                        mRotationTargetMarker.name = "RotationTargetMarker";
                    }
                    
                    mRotationTargetMarker.transform.position = hit.point;
                    
                    if (mDirectionArrow != null)
                    {
                        UpdateArrowDirection(mDirectionArrow, hit.point, mPendingPosition);
                    }
                }
                else
                {
                    if (mRotationTargetMarker != null)
                    {
                        UnityEngine.Object.Destroy(mRotationTargetMarker);
                        mRotationTargetMarker = null;
                    }
                    
                    if (mPaintMarker != null && mPaintMarker.transform != null)
                    {
                        mPaintMarker.transform.position = hit.point;
                    }
                }
            }
        }


        protected override void ProcessDelete(IList<string> args)
        {
            string name = GetNextArg(args);
            GetMapDataByName(name, (spot, result) =>
            {
                if (result != RequestResult.Succeeded)
                {
                    this.LogWarningInstanced($"No such hiding spot {name} in scene {mManager.CurrentScene}!", LogCategoryFlags.PaintManager);
                    return;
                }
                DeleteMapData(spot.Guid, (deletedSpot, deleteResult) =>
                {
                    this.LogAlwaysInstanced($"Deleted hiding spot {name} in scene {mManager.CurrentScene}.", LogCategoryFlags.PaintManager);
                    DataManager.SaveMapData();
                });
            });
        }


        protected override void ProcessGoTo(IList<string> args)
        {
            string name = GetNextArg(args);
            GetMapDataByName(name, (data, result) =>
            {
                if (result != RequestResult.Succeeded)
                {
                    this.LogErrorInstanced($"No data found with name {name}!");
                    return;
                }
                Teleport(data.Position, data.Rotation);
                this.LogAlwaysInstanced($"Teleported to {data}! Watch out for ambush wolves...", LogCategoryFlags.PaintManager);
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
                lock (mDebugShownObjects)
                {
                    mDebugShownObjects.Add(CreateMarker(data.Position, Color.yellow, data.Name, 100));
                }
            });
        }

        protected override void ShowByName(string name)
        {
            GetMapDataByName(name, (data, result) =>
            {
                if (result != RequestResult.Succeeded)
                {
                    this.LogErrorInstanced($"No hiding spot with name {name}!");
                    return;
                }
                lock (mDebugShownObjects)
                {
                    mDebugShownObjects.Add(CreateMarker(data.Position, Color.yellow, data.Name, 100));
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

        private bool InitializePaintHidingSpot(string name)
        {
            try
            {
                CleanupPaintMarker();
                CleanupDirectionMarkers();
                
                mCurrentDataName = name;
                mPaintMarker = CreateMarker(Vector3.zero, Color.green, "PaintMarker", 50f, 2f);
                if (mPaintMarker == null)
                {
                    this.LogWarningInstanced("Failed to create paint marker", LogCategoryFlags.PaintManager);
                    return false;
                }

                mCurrentPaintMode = PaintMode.Active;
                mSelectingRotation = false;
                return true;
            }
            catch (Exception e)
            {
                this.LogErrorInstanced($"Paint mode initialization failed: {e}", LogCategoryFlags.PaintManager);
                ExitPaint();
                return false;
            }
        }

        private void CleanupDirectionMarkers()
        {
            if (mDirectionArrow != null)
            {
                UnityEngine.Object.Destroy(mDirectionArrow);
                mDirectionArrow = null;
            }
            
            if (mRotationTargetMarker != null)
            {
                UnityEngine.Object.Destroy(mRotationTargetMarker);
                mRotationTargetMarker = null;
            }
        }

        private GameObject CreateDirectionArrow(Vector3 startPos, Vector3 targetPos, Color color, string name)
        {
            GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.Destroy(arrow.GetComponent<Collider>());
            
            Vector3 direction = targetPos - startPos;
            direction.y = 0;
            float distance = Mathf.Max(direction.magnitude, 30f);
            
            Vector3 arrowPos = startPos + new Vector3(0, 50f, 0);
            arrow.transform.position = arrowPos;
            arrow.transform.localScale = new Vector3(3f, distance/2f, 3f);
            
            UpdateArrowDirection(arrow, targetPos, startPos);
            
            GameObject arrowHead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.Destroy(arrowHead.GetComponent<Collider>());
            arrowHead.transform.localScale = new Vector3(6f, 6f, 6f);
            arrowHead.GetComponent<Renderer>().material.color = color;
            arrowHead.name = name + "Head";
            arrowHead.transform.SetParent(arrow.transform);
            
            UpdateArrowDirection(arrow, targetPos, startPos);
            
            arrow.GetComponent<Renderer>().material.color = color;
            arrow.name = name;
            return arrow;
        }

        private void UpdateArrowDirection(GameObject arrow, Vector3 targetPos, Vector3 startPos)
        {
            if (arrow == null) return;
            
            Vector3 direction = targetPos - startPos;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                arrow.transform.rotation = Quaternion.LookRotation(direction.normalized) * Quaternion.Euler(90f, 0f, 0f);
                
                Transform arrowHead = arrow.transform.Find(arrow.name + "Head");
                if (arrowHead != null)
                {
                    float distance = arrow.transform.localScale.y * 2f;
                    arrowHead.position = arrow.transform.position + (direction.normalized * distance/2f);
                }
            }
        }

        private void CreateHidingSpotWithRotation()
        {
            Ray ray = GameManager.m_vpFPSCamera.m_Camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, Utils.m_PhysicalCollisionLayerMask))
            {
                Vector3 direction = hit.point - mPendingPosition;
                direction.y = 0;
                Quaternion rotation = Quaternion.LookRotation(direction.normalized);

                HidingSpot newSpot = new HidingSpot(mCurrentDataName, mPendingPosition, rotation, mManager.CurrentScene);
                RegisterMapData(newSpot, (spot, result) =>
                {
                    mDebugShownObjects.Add(CreateMarker(newSpot.Position, Color.yellow, $"Hiding spot: {mCurrentDataName}", 100.0f));
                    this.LogAlwaysInstanced($"Created hiding spot {mCurrentDataName} at {newSpot.Position} with rotation {newSpot.Rotation}", LogCategoryFlags.PaintManager);
                    
                    mSelectingRotation = false;
                    CleanupDirectionMarkers();
                    DataManager.SaveMapData();
                    
                    GetUniqueMapDataName(mCurrentDataNameBase ?? "HidingSpot", (uniqueName) =>
                    {
                        if (InitializePaintHidingSpot(uniqueName))
                        {
                            this.LogAlwaysInstanced("Ready for next hiding spot. Left click to place.", LogCategoryFlags.PaintManager);
                        }
                    });
                });
            }
        }

        private void SelectHidingSpotPosition()
        {
            mPendingPosition = mPaintMarkerPosition;
            mSelectingRotation = true;
            
            if (mPaintMarker != null && mDirectionArrow == null)
            {
                mDirectionArrow = CreateDirectionArrow(mPaintMarker.transform.position, mPaintMarker.transform.position + Vector3.forward, Color.green, "DirectionArrow");
                mDirectionArrow.transform.SetParent(mPaintMarker.transform);
            }
            
            this.LogAlwaysInstanced($"Selected hiding spot position at {mPendingPosition}. Left click to set rotation, or Shift+Left click to reselect position.", LogCategoryFlags.PaintManager);
        }

        private void RevertToPositionSelection()
        {
            mSelectingRotation = false;
            CleanupDirectionMarkers();
            this.LogAlwaysInstanced("Reverted to position selection. Left click to select new position.", LogCategoryFlags.PaintManager);
        }
    }
}
