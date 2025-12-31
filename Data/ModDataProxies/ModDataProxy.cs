using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;


namespace ExpandedAiFramework
{ 
    public abstract class ModDataProxy : SerializedData, IModDataProxy
    {
        protected string[] mCustomData;
        protected Vector3 mCurrentPosition;
        [JsonIgnore] public bool Fresh = false;

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
