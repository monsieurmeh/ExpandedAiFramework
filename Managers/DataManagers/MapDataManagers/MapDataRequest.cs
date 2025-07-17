using UnityEngine;

namespace ExpandedAiFramework
{
    public class MapDataRequest<T>
    {
        public Vector3 Position;
        public Action<T> Callback;
        public int ExtraCandidates;
        private string mCachedString;
        public object[] Args;

        public MapDataRequest(Vector3 position, Action<T> callback, int extraCandidates, params object[] args)
        {
            Position = position;
            Callback = callback;
            ExtraCandidates = extraCandidates;
            Args = args;
            mCachedString = $"{nameof(MapDataRequest<T>)}.<{typeof(T)}> at {Position} with {ExtraCandidates} extra candidates";
            for (int i = 0, iMax = args?.Length ?? 0; i < iMax; i++)
            {
                mCachedString += $" (Arg{i}: {args[i]})";
            }
        }

        public override string ToString()
        {
            return mCachedString;
        }
    }
}
