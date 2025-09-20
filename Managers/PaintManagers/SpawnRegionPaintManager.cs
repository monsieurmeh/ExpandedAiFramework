using UnityEngine;
using Il2Cpp;
using static ExpandedAiFramework.Utility;

namespace ExpandedAiFramework
{
    public class SpawnRegionPaintManager : BasePaintManager
    {
        private CustomSpawnRegion mSelectedSpawnRegion = null;
        private bool mMovingSpawnRegion = false;
        private Vector3 mOriginalPosition = Vector3.zero;
        private Dictionary<CustomSpawnRegion, GameObject> mSpawnRegionMarkers = new Dictionary<CustomSpawnRegion, GameObject>();
        private CustomSpawnRegion mHoveredSpawnRegion = null;
        private Color mOriginalHoverColor = Color.white;

        public override string TypeName => CommandString_SpawnRegion;
        public override string TypeInfo => "SpawnRegionPaintManager";

        public SpawnRegionPaintManager(EAFManager manager) : base(manager) { }

        public override void StartPaint(string[] args)
        {
            mCurrentPaintMode = PaintMode.Active;
            ShowAllSpawnRegions();
            
            // Create a cursor marker to show where you're aiming - simple red ball
            mPaintMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mPaintMarker.name = "SpawnRegionCursor";
            mPaintMarker.transform.localScale = new Vector3(3f, 3f, 3f);
            mPaintMarker.GetComponent<Renderer>().material.color = Color.red;
            UnityEngine.Object.Destroy(mPaintMarker.GetComponent<Collider>()); // Remove collider so it doesn't interfere
            
            this.LogAlwaysInstanced("Spawn Region Paint Mode activated. Left-click to move spawn regions, right-click for info, right-click empty space to exit.");
        }

        public override void ExitPaint()
        {
            if (mMovingSpawnRegion)
            {
                DropSpawnRegion(false);
            }
            
            HideAllSpawnRegions();
            mCurrentPaintMode = PaintMode.Inactive;
            CleanupPaintMarker();
            this.LogAlwaysInstanced("Spawn Region Paint Mode deactivated.");
        }

        protected override void HandleLeftClick()
        {
            if (mMovingSpawnRegion)
            {
                PlaceSpawnRegion();
            }
            else
            {
                CustomSpawnRegion clickedRegion = GetSpawnRegionUnderMouse();
                if (clickedRegion != null)
                {
                    StartMovingSpawnRegion(clickedRegion);
                }
                else
                {
                    this.LogAlwaysInstanced("UNIMPLEMENTED: Left-click on empty space");
                }
            }
        }

        protected override void HandleRightClick()
        {
            if (mMovingSpawnRegion)
            {
                DropSpawnRegion(false);
            }
            else
            {
                CustomSpawnRegion clickedRegion = GetSpawnRegionUnderMouse();
                if (clickedRegion != null)
                {
                    LogSpawnRegionInfo(clickedRegion);
                }
                else
                {
                    ExitPaint();
                }
            }
        }

        public override void ProcessCommand(string command, string[] args)
        {
            // SpawnRegionPaintManager doesn't need additional commands beyond the base paint functionality
            this.LogAlwaysInstanced($"SpawnRegionPaintManager doesn't support command: {command}");
        }

        protected override void UpdatePaintMarkerInternal()
        {
            base.UpdatePaintMarkerInternal();
            
            if (mMovingSpawnRegion && mSelectedSpawnRegion != null)
            {
                // Move the selected spawn region to follow the cursor
                mSelectedSpawnRegion.VanillaSpawnRegion.transform.position = mPaintMarkerPosition;
            }

            // Handle hover effects
            UpdateHoverEffects();
        }

        private void ShowAllSpawnRegions()
        {
            foreach (CustomSpawnRegion customSpawnRegion in mManager.SpawnRegionManager.CustomSpawnRegions.Values)
            {
                if (customSpawnRegion?.VanillaSpawnRegion == null) continue;
                
                SpawnRegion spawnRegion = customSpawnRegion.VanillaSpawnRegion;
                GameObject marker = CreateMarker(Vector3.zero, GetSpawnRegionColor(spawnRegion), "SpawnRegionDebugMarker", 100, 10);
                marker.transform.SetParent(spawnRegion.transform, false);
                
                // Add a cylindrical collider directly to the marker for easy clicking
                CapsuleCollider collider = marker.AddComponent<CapsuleCollider>();
                collider.height = 10f; // Height in local space (gets scaled by transform)
                collider.radius = 0.6f; // Radius in local space (gets scaled by transform) - 0.6 * 10 = 6 effective radius
                collider.center = new Vector3(0, 5f, 0); // Center in local space
                
                // Add tag to identify which spawn region this marker belongs to
                SpawnRegionColliderTag tag = marker.AddComponent<SpawnRegionColliderTag>();
                tag.SpawnRegion = customSpawnRegion;
                
                mSpawnRegionMarkers[customSpawnRegion] = marker;
            }
        }

        private void HideAllSpawnRegions()
        {
            foreach (CustomSpawnRegion customSpawnRegion in mManager.SpawnRegionManager.CustomSpawnRegions.Values)
            {
                if (customSpawnRegion?.VanillaSpawnRegion == null) continue;
                
                foreach (Transform child in customSpawnRegion.VanillaSpawnRegion.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name.Contains("SpawnRegionDebugMarker"))
                    {
                        GameObject.Destroy(child.gameObject);
                        break;
                    }
                }
            }
        }


        private void UpdateHoverEffects()
        {
            CustomSpawnRegion hoveredRegion = GetSpawnRegionUnderMouse();
            
            // Reset previous hover
            if (mHoveredSpawnRegion != null && mHoveredSpawnRegion != hoveredRegion)
            {
                SetSpawnRegionMarkerColor(mHoveredSpawnRegion, mOriginalHoverColor);
                mHoveredSpawnRegion = null;
            }
            
            // Set new hover
            if (hoveredRegion != null && hoveredRegion != mHoveredSpawnRegion && !mMovingSpawnRegion)
            {
                mHoveredSpawnRegion = hoveredRegion;
                mOriginalHoverColor = GetSpawnRegionColor(hoveredRegion.VanillaSpawnRegion);
                SetSpawnRegionMarkerColor(hoveredRegion, Color.white); // Highlight color
            }
        }

        private CustomSpawnRegion GetSpawnRegionUnderMouse()
        {
            if (GameManager.m_vpFPSCamera?.m_Camera == null) return null;
            
            Ray ray = GameManager.m_vpFPSCamera.m_Camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
            {
                if (hit.collider == null) return null;  
                if (!hit.collider.gameObject.TryGetComponent<SpawnRegionColliderTag>(out SpawnRegionColliderTag tag)) return null;
                return tag.SpawnRegion;
            }
            
            return null;
        }

        private void StartMovingSpawnRegion(CustomSpawnRegion spawnRegion)
        {
            mSelectedSpawnRegion = spawnRegion;
            mMovingSpawnRegion = true;
            mOriginalPosition = spawnRegion.VanillaSpawnRegion.transform.position;
            
            // Disable the collider while moving so it doesn't interfere with mouse detection
            if (mSpawnRegionMarkers.TryGetValue(spawnRegion, out GameObject marker))
            {
                CapsuleCollider collider = marker.GetComponent<CapsuleCollider>();
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }
            
            this.LogAlwaysInstanced($"Started moving spawn region {spawnRegion.VanillaSpawnRegion.GetHashCode()} from position {mOriginalPosition}");
        }

        private void PlaceSpawnRegion()
        {
            if (mSelectedSpawnRegion == null) return;
            
            Vector3 newPosition = mPaintMarkerPosition;
            mSelectedSpawnRegion.VanillaSpawnRegion.transform.position = newPosition;
            mSelectedSpawnRegion.VanillaSpawnRegion.m_Center = newPosition;
                mSelectedSpawnRegion.ModDataProxy.CurrentPosition = newPosition;
            
            // Re-enable the collider now that we're done moving
            if (mSpawnRegionMarkers.TryGetValue(mSelectedSpawnRegion, out GameObject marker))
            {
                CapsuleCollider collider = marker.GetComponent<CapsuleCollider>();
                if (collider != null)
                {
                    collider.enabled = true;
                }
            }
            
            this.LogAlwaysInstanced($"Moved spawn region {mSelectedSpawnRegion.VanillaSpawnRegion.GetHashCode()} to position {newPosition}");
            
            mMovingSpawnRegion = false;
            mSelectedSpawnRegion = null;
        }

        private void DropSpawnRegion(bool updatePosition)
        {
            if (mSelectedSpawnRegion == null) return;
            
            if (!updatePosition)
            {
                mSelectedSpawnRegion.VanillaSpawnRegion.transform.position = mOriginalPosition;
                mSelectedSpawnRegion.VanillaSpawnRegion.m_Center = mOriginalPosition;
                mSelectedSpawnRegion.ModDataProxy.CurrentPosition = mOriginalPosition;
                this.LogAlwaysInstanced($"Dropped spawn region {mSelectedSpawnRegion.VanillaSpawnRegion.GetHashCode()} back to original position {mOriginalPosition}");
            }
            
            // Re-enable the collider now that we're done moving
            if (mSpawnRegionMarkers.TryGetValue(mSelectedSpawnRegion, out GameObject marker))
            {
                CapsuleCollider collider = marker.GetComponent<CapsuleCollider>();
                if (collider != null)
                {
                    collider.enabled = true;
                }
            }
            
            mMovingSpawnRegion = false;
            mSelectedSpawnRegion = null;
        }

        private void LogSpawnRegionInfo(CustomSpawnRegion spawnRegion)
        {
            SpawnRegion vanilla = spawnRegion.VanillaSpawnRegion;
            this.LogAlwaysInstanced($"=== Spawn Region Info ===");
            this.LogAlwaysInstanced($"Hash Code: {vanilla.GetHashCode()}");
            this.LogAlwaysInstanced($"GUID: {spawnRegion.ModDataProxy.Guid}");
            this.LogAlwaysInstanced($"Position: {vanilla.transform.position}");
            this.LogAlwaysInstanced($"AI Sub Type: {vanilla.m_AiSubTypeSpawned}");
            this.LogAlwaysInstanced($"AI Type: {vanilla.m_AiTypeSpawned}");
            this.LogAlwaysInstanced($"Radius: {vanilla.m_Radius}");
            this.LogAlwaysInstanced($"Active Spawns: {spawnRegion.ActiveSpawns.Count}");
            this.LogAlwaysInstanced($"Is Active: {vanilla.gameObject.activeInHierarchy}");
            this.LogAlwaysInstanced($"========================");
        }

        private void SetSpawnRegionMarkerColor(CustomSpawnRegion spawnRegion, Color color)
        {
            if (mSpawnRegionMarkers.TryGetValue(spawnRegion, out GameObject marker))
            {
                Renderer renderer = marker.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = color;
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
    }

    // Helper component to tag colliders with their associated spawn region
    [RegisterTypeInIl2Cpp]
    public class SpawnRegionColliderTag : MonoBehaviour
    {
        public CustomSpawnRegion SpawnRegion { get; set; }
    }
}
