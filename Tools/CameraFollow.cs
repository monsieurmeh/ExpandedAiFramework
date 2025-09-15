using UnityEngine;


namespace ExpandedAiFramework
{
    public class CameraFollow
    {
        protected Transform mTarget;
        protected Transform mCamera;
        protected float mDistance = 5.0f;
        protected float mMinDistance = 3.0f;
        protected float mMaxDistance = 10.0f;
        protected float mXSpeed = 120.0f;
        protected float mYSpeed = 120.0f;
        protected float mZSpeed = 1.0f;
        protected float mYMinLimit = 20f;
        protected float mYMaxLimit = 80f;
        protected float mX = 0.0f;
        protected float mY = 0.0f;


        public void SetTarget(Transform target)
        {
            mTarget = target;
            mCamera = GameManager.m_vpFPSCamera.m_Camera.transform;
            mX = mCamera.eulerAngles.x;
            mY = mCamera.eulerAngles.y;
        }


        public void Update()
        {
            if (mCamera == null || mTarget == null)
            {
                return;
            }
            //this is not working ;/
            mCamera.position = mTarget.position + new Vector3(0.0f, 25f, 10.0f);
            /*
            mX += InputManager.GetAxisMouseX(GameManager.m_PlayerManager) * mXSpeed * Time.deltaTime;
            mY -= InputManager.GetAxisMouseY(GameManager.m_PlayerManager) * mYSpeed * Time.deltaTime;
            mDistance += InputManager.GetAxisScrollWheel(GameManager.m_PlayerManager) * mZSpeed * Time.deltaTime;
            mY = Mathf.Clamp(mY, mYMinLimit, mYMaxLimit);
            mDistance = Mathf.Clamp(mDistance, mMinDistance, mMaxDistance);
            Quaternion rotation = Quaternion.Euler(mX, mY, 0);
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -mDistance);
            Vector3 position = Quaternion.Euler(mY, mX, 0) * negDistance + mTarget.position;
            mCamera.rotation = rotation;
            mCamera.position = position;
            */
        }
    }
}
