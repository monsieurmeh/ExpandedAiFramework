using UnityEngine;
using MelonLoader.TinyJSON;


namespace ExpandedAiFramework
{
    [Serializable]
    public abstract class MapData
    {
        [NonSerialized] protected bool mTransient = false;
        [NonSerialized] protected string mCachedString;
        [Include] protected Guid mGuid;
        [Include] protected string mName;
        [Include] protected string mScene;
        [Include] protected Vector3 mAnchorPosition;
        [Include] protected bool mClaimed;


        public Guid Guid { get { return mGuid; } }
        public string Name { get { return mName; } }
        public string Scene { get { return mScene; } }
        public bool Transient { get { return mTransient; } }
        public Vector3 AnchorPosition { get { return mAnchorPosition; } }
        public bool Claimed { get { return mClaimed; } }


        public MapData() { }

        public MapData(string name, string scene, Vector3 anchorPostion, bool transient)
        {
            mGuid = Guid.NewGuid();
            mName = name;
            mTransient = transient;
            mScene = scene;
            mAnchorPosition = anchorPostion;
            mClaimed = false;
            UpdateCachedString();
        }


        public virtual void UpdateCachedString()
        {
            if (mGuid == Guid.Empty)
            {
                mGuid = Guid.NewGuid();
            }
            mCachedString = mClaimed ? "Claimed " : "Unclaimed " + $"{GetType().Name} {Name} anchored at {AnchorPosition} in scene {Scene} with Guid {Guid}";
        }


        public override string ToString()
        {
            return mCachedString ?? "UNINITIALIZED";
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
