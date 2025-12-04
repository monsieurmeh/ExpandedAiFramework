using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;


namespace ExpandedAiFramework
{
    public abstract class SerializedData : ISerializedData
    {
        protected Guid mGuid;
        protected string mScene;
        [JsonIgnore] protected string mCachedString;
        [JsonIgnore] protected string mDataLocation;

        public Guid Guid { get { return mGuid; } }
        public string Scene { get { return mScene; } }
        public string DataLocation { get { return mDataLocation; } set { mDataLocation = value; } }
        public abstract string DisplayName { get; }

        public SerializedData() { }

        public SerializedData(Guid guid, string scene)
        {
            mGuid = guid;
            mScene = scene;
        }


        public virtual bool PostProcess() => true;


        public override int GetHashCode() => mGuid.GetHashCode();


        public override bool Equals(object obj) => this.Equals(obj as SerializedData);


        public bool Equals(SerializedData data)
        {
            if (data is null)
            {
                return false;
            }

            if (GetType() != data.GetType())
            {
                return false;
            }

            return mGuid == data.Guid;
        }


        public static bool operator ==(SerializedData lhs, SerializedData rhs)
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

        public static bool operator !=(SerializedData lhs, SerializedData rhs) => !(lhs == rhs);
    }
}
