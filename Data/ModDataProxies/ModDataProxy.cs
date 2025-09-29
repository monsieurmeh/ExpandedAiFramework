using UnityEngine;
using MelonLoader.TinyJSON;


namespace ExpandedAiFramework
{ 
    public abstract class ModDataProxy : SerializedData, IModDataProxy
    {
        [Include] protected string[] mCustomData;
        [Include] protected Vector3 mCurrentPosition;
        [Exclude] public bool Fresh = false;

        public string[] CustomData { get { return mCustomData; } set { mCustomData = value; } }
        public Vector3 CurrentPosition { get { return mCurrentPosition; } set { mCurrentPosition = value; } }
        public override string DisplayName { get { return $"{GetType().Name}-{mGuid}"; } }


        public ModDataProxy() : base() { }


        public ModDataProxy(Guid guid, string scene, Vector3 currentPosition) : base(guid, scene)
        {
            mCurrentPosition = currentPosition;
            mCustomData = [];
            Fresh = true;
        }
    }
}
