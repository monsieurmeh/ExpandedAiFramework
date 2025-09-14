using Il2CppVoice;
using MelonLoader.TinyJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandedAiFramework
{
    public interface ISubDataManager : ILogInfoProvider
    {
        void StartWorker();
        void StopWorker();
        void ScheduleRefresh(string sceneName);
        void ScheduleSave();
        void ScheduleLoad();
        void ScheduleClear();
        void ScheduleRequest(IRequest request);
        void ClearRequests();
    }
}
