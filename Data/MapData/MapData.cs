using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;


namespace ExpandedAiFramework
{
    public abstract class MapData : SerializedData, IMapData
    {
        [JsonIgnore] protected bool mTransient = false;
        protected string mName;
        protected Vector3 mAnchorPosition;
        protected bool mClaimed;


        public string Name { get { return mName; } }
        public bool Transient { get { return mTransient; } }
        public Vector3 AnchorPosition { get { return mAnchorPosition; } }
        public bool Claimed { get { return mClaimed; } }
        public override string DisplayName { get { return $"{mName} ({GetType().Name}-{mGuid})"; } }


        public MapData() : base() { }

        public MapData(string name, string scene, Vector3 anchorPostion, bool transient) : base(Guid.NewGuid(), scene)
        {
            mName = name;
            mTransient = transient;
            mAnchorPosition = anchorPostion;
            mClaimed = false;
        }


        public virtual bool Claim()
        {
            if (mClaimed)
            {
                return false;
            }
            mClaimed = true;
            return true;
        }


        public override int GetHashCode() => (Name, Scene).GetHashCode();
        public override bool Equals(object obj) => this.Equals(obj as MapData);
        public bool Equals(MapData data)
        {
            if (data is null)
            {
                return false;
            }

            if (GetType() != data.GetType())
            {
                return false;
            }

            return Name == data.Name && Scene == data.Scene;
        }


        public static bool operator ==(MapData lhs, MapData rhs)
        {
            if (lhs is null)
            {
                if (rhs is null)
                {
                    return true;
                }
                return false;
            }
            return lhs.Equals(rhs);
        }
        public static bool operator !=(MapData lhs, MapData rhs) => !(lhs == rhs);
    }
}
