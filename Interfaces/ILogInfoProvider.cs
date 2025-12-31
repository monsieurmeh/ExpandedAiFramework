using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Profiling;

namespace ExpandedAiFramework
{
    public interface ILogInfoProvider
    {
        public string InstanceInfo { get; }
        public string TypeInfo { get; }
    }


    public static class ILogInfoProviderExtensions
    {
        public static void LogTraceInstanced(this ILogInfoProvider provider, string message,  LogCategoryFlags logCategoryFlags = LogCategoryFlags.General, [CallerMemberName] string memberName = "")  => LogInstanced(provider, message, logCategoryFlags | LogCategoryFlags.Trace, memberName);
        public static void LogDebugInstanced(this ILogInfoProvider provider, string message, LogCategoryFlags logCategoryFlags = LogCategoryFlags.General, [CallerMemberName] string memberName = "") => LogInstanced(provider, message, logCategoryFlags | LogCategoryFlags.Debug, memberName);

        public static void LogInstanced(
            this ILogInfoProvider provider, 
            string message, 
            LogCategoryFlags logCategoryFlags = LogCategoryFlags.General,
            [CallerMemberName] string memberName = "")
        {
            Log(
                message,
                logCategoryFlags,
                provider.TypeInfo,
                provider.InstanceInfo, 
                memberName);
        }

        public static void ErrorInstanced(
            this ILogInfoProvider provider,
            string message,
            LogCategoryFlags logCategoryFlags = LogCategoryFlags.General,
            [CallerMemberName] string memberName = "")
        {
            Error(
                message,
                logCategoryFlags,
                provider.TypeInfo, 
                provider.InstanceInfo, 
                memberName);
        }
    }
}
