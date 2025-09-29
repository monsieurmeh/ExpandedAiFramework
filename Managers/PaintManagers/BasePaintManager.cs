using UnityEngine;


namespace ExpandedAiFramework
{
    public enum PaintMode : int
    {
        Inactive = 0,
        Active = 1
    }

    public abstract class BasePaintManager : ILogInfoProvider
    {
        protected EAFManager mManager;
        protected PaintMode mCurrentPaintMode = PaintMode.Inactive;
        protected GameObject mPaintMarker = null;
        protected Vector3 mPaintMarkerPosition = Vector3.zero;
        protected Quaternion mPaintMarkerRotation = Quaternion.identity;

        public abstract string TypeName { get; }
        public virtual string InstanceInfo { get { return string.Empty; } }
        public abstract string TypeInfo { get; }
        public bool IsActive => mCurrentPaintMode == PaintMode.Active;

        public BasePaintManager(EAFManager manager)
        {
            mManager = manager;
        }

        public virtual void Initialize() { }
        public virtual void Shutdown() { CleanupPaintMarker(); }
        public virtual void UpdateFromManager() { UpdatePaintMarker(); }

        public abstract void StartPaint(IList<string> args);
        public abstract void ExitPaint();
        public abstract void ProcessCommand(string command, IList<string> args);

        public virtual void HandlePaintInput()
        {
            if (mCurrentPaintMode != PaintMode.Active) return;

            bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            
            if (Input.GetMouseButtonDown(0))
            {
                if (shiftPressed)
                {
                    HandleShiftLeftClick();
                }
                else
                {
                    HandleLeftClick();
                }
            }
            else if (Input.GetMouseButtonDown(1))
            {
                if (shiftPressed)
                {
                    HandleShiftRightClick();
                }
                else
                {
                    HandleRightClick();
                }
            }
        }

        protected virtual void HandleLeftClick() { }
        protected virtual void HandleShiftLeftClick() { }
        protected virtual void HandleRightClick() { ExitPaint(); }
        protected virtual void HandleShiftRightClick() { ExitPaint(); }

        protected virtual void UpdatePaintMarker()
        {
            if (mCurrentPaintMode == PaintMode.Inactive || GameManager.m_vpFPSCamera.m_Camera == null)
            {
                CleanupPaintMarker();
                return;
            }

            HandlePaintInput();
            UpdatePaintMarkerInternal();
        }

        protected virtual void UpdatePaintMarkerInternal()
        {
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

        protected virtual void CleanupPaintMarker()
        {
            if (mPaintMarker != null)
            {
                UnityEngine.Object.Destroy(mPaintMarker);
                mPaintMarker = null;
            }
        }

        protected GameObject CreateMarker(Vector3 position, Color color, string name, float height, float diameter = 5f)
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

        protected GameObject ConnectMarkers(Vector3 pos1, Vector3 pos2, Color color, string name, float height, float diameter = 5f)
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

        protected void Teleport(Vector3 position, Quaternion rotation)
        {
            GameManager.GetPlayerTransform().position = position + new Vector3(0f, 1f, 0f);
            GameManager.GetPlayerTransform().rotation = rotation;
        }
    }
}
