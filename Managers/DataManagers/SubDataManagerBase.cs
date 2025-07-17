using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandedAiFramework
{
    public abstract class SubDataManagerBase
    {
        protected DataManager mManager;

        public SubDataManagerBase(DataManager manager)
        {
            mManager = manager;
        }

        public abstract void StartWorker();
        public abstract void StopWorker();
        public abstract void RefreshData(string sceneName);
        public abstract void Save();
        public abstract void Load();
        public abstract void LoadAdditional(string pathFromModsFolder);
        public abstract void Clear();
    }
}
