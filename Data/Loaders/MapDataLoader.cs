using UnityEngine;

namespace ExpandedAiFramework
{
    public abstract class MapDataLoader<T> where T : IMapData, new()
    {
        protected T mData;
        protected Action<T> mCallback;
        protected Func<T, bool> mFilter;
        protected CustomBaseAi mAi;
        protected SpawnModDataProxy mProxy;
        protected DataManager mDataManager;

        protected Guid mGuid = Guid.Empty;
        protected bool mConnected = false;
        protected bool mLoading = false;
        protected bool mNew = false;

        public T Data { get { return mData; } }
        public bool New { get { return mNew; } }

        public MapDataLoader(CustomBaseAi ai, SpawnModDataProxy proxy, DataManager dataManager, Func<T, bool> filter, Action<T> callback = null)
        {
            mAi = ai;
            mProxy = proxy;
            mDataManager = dataManager;
            mFilter = filter;
            mCallback = callback;
        }

        public bool Connected()
        {
            if (mConnected) return true;
            MaybeLoad();
            return false;
        }


        private void MaybeLoad()
        {
            if (mLoading) return;
            mLoading = true;

            if (ValidateSaved())
            {
                RequestSaved();
            }
            else
            {
                RequestNearest();
            }
        }

        private void RequestSaved()
        {
            mDataManager.ScheduleMapDataRequest<T>(
                new GetDataByGuidRequest<T>(
                    mGuid,
                    mProxy.Scene,
                    OnSaved,
                    false
                )
            );
        }

        private void OnSaved(T data, RequestResult result)
        {
            if (result == RequestResult.Succeeded)
            {
                Attach(data, result);
            }
            else
            {
                mAi.LogTraceInstanced($"Failed to fetch saved, requesting nearest", LogCategoryFlags.Ai | LogCategoryFlags.SerializedData);
                RequestNearest();
            }
        }

        private void RequestNearest()
        {
            mDataManager.ScheduleMapDataRequest<T>(
                new GetNearestMapDataRequest<T>(
                    mAi.transform.position,
                    mProxy.Scene,
                    OnNearest,
                    false,
                    mFilter,
                    3
                )
            );
        }

        private void OnNearest(T data, RequestResult result)
        {
            mNew = true;
            Attach(data, result);
        }

        private void Attach(T data, RequestResult result)
        {
            mData = data;
            mLoading = false;
            if (result != RequestResult.Succeeded)
            {
                mAi.LogErrorInstanced($"Failed to attach wander path!");
                mCallback?.Invoke(mData);
                return;
            }
            Save();
            AttachDetails();
            mConnected = true;
            data.Claim();
            mAi.LogTraceInstanced($"Claimed {typeof(T)} with guid <<<{mGuid}>>>", LogCategoryFlags.Ai);
            mCallback?.Invoke(mData);
        }

        protected virtual bool ValidateSaved()
        {
            if (mProxy == null)
            {
                mAi.LogTraceInstanced($"Null proxy", LogCategoryFlags.Ai);
                return false;
            }
            if (mProxy.CustomData == null)
            {
                mAi.LogTraceInstanced($"Null proxy custom data", LogCategoryFlags.Ai);
                return false;
            }
            if (mProxy.CustomData.Length < 2)
            {
                mAi.LogTraceInstanced($"Not enough length to proxy custom data (guid and waypoint index required)", LogCategoryFlags.Ai);
                return false;
            }
            if (!Guid.TryParse(mProxy.CustomData[0], out mGuid))
            {
                mAi.LogTraceInstanced($"Could not parse guid", LogCategoryFlags.Ai);
                return false;
            }
            return true;
        }

        private void Save()
        {
            mProxy.CustomData = [mGuid.ToString()];
            SaveDetails();
        }

        protected abstract bool ValidateDetails();

        protected abstract void SaveDetails();

        protected abstract void AttachDetails();
    }
}
