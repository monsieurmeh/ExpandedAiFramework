using MelonLoader.TinyJSON;
using UnityEngine;


namespace ExpandedAiFramework
{
    // I am going to split some flags with everyone here
    // Flags 0-15 are reserved for EAF, with flags 0-7 used for modes (individual, pack, migration, etc) and flags 8-15 used for types (WanderWolf, Bear, etc)
    // Flags 16-31 are free use for other modders
    [Flags]
    public enum WanderPathFlags : uint
    {
        None = 0U,
        Individual = 1U << 0,
        Pack = 1U << 1,
        Migration = 1U << 2,
        Modded = 1U << 3,
        MODE_FLAG_RESERVED_3 = 1U << 4,
        MODE_FLAG_RESERVED_2 = 1U << 5,
        MODE_FLAG_RESERVED_1 = 1U << 6,
        MODE_FLAG_RESERVED_0 = 1U << 7,
        Wolf = 1U << 8,
        Bear = 1U << 9,
        Moose = 1U << 10,
        Cougar = 1U << 11,
        TYPE_FLAG_RESERVED_3 = 1U << 12,
        TYPE_FLAG_RESERVED_2 = 1U << 13,
        TYPE_FLAG_RESERVED_1 = 1U << 14,
        TYPE_FLAG_RESERVED_0 = 1U << 15,
    }

    public class WanderPath : MapData
    {
        public const WanderPathFlags DefaultFlags = WanderPathFlags.Wolf | WanderPathFlags.Individual | WanderPathFlags.Modded;
        [Include] private Vector3[] mPathPoints;
        [Exclude] private WanderPathFlags mWanderPathFlags = DefaultFlags;
        [Include] private uint mWanderPathFlagsSerialized = (uint)DefaultFlags;

        public Vector3[] PathPoints { get { return mPathPoints; } }
        public WanderPathFlags WanderPathFlags { get { return mWanderPathFlags; } }

        public WanderPath() : base() { }

        public WanderPath(string name,
                          Vector3[] pathPoints,
                          string scene,
                          WanderPathFlags wanderPathFlags = DefaultFlags,
                          bool transient = false) : base(name,
                                                         scene,
                                                         pathPoints[0],
                                                         transient)
        {
            mPathPoints = pathPoints;
            mWanderPathFlags = wanderPathFlags;
            mWanderPathFlagsSerialized = (uint)mWanderPathFlags;
        }

        public override bool PostProcess()
        {
            try
            {
                mWanderPathFlags = (WanderPathFlags)mWanderPathFlagsSerialized;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}