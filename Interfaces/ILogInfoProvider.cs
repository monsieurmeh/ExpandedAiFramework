using ComplexLogger;
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
        public static void LogTraceInstanced(this ILogInfoProvider provider, string message, [CallerMemberName] string memberName = "")
        {
            Manager.Log(message, FlaggedLoggingLevel.Trace, provider.TypeInfo, provider.InstanceInfo, memberName);
        }


        public static void LogDebugInstanced(this ILogInfoProvider provider, string message, [CallerMemberName] string memberName = "")
        {
            Manager.Log(message, FlaggedLoggingLevel.Debug, provider.TypeInfo, provider.InstanceInfo, memberName);
        }


        public static void LogVerboseInstanced(this ILogInfoProvider provider, string message, [CallerMemberName] string memberName = "")
        {
            Manager.Log(message, FlaggedLoggingLevel.Verbose, provider.TypeInfo, provider.InstanceInfo, memberName);
        }


        public static void LogWarningInstanced(this ILogInfoProvider provider, string message, [CallerMemberName] string memberName = "")
        {
            Manager.Log(message, FlaggedLoggingLevel.Warning, provider.TypeInfo, provider.InstanceInfo, memberName);
        }


        public static void LogErrorInstanced(this ILogInfoProvider provider, string message, FlaggedLoggingLevel additionalFlags = 0U, [CallerMemberName] string memberName = "")
        {
            Manager.Log(message, FlaggedLoggingLevel.Error | additionalFlags, provider.TypeInfo, provider.InstanceInfo, memberName);
        }


        public static void LogAlwaysInstanced(this ILogInfoProvider provider, string message, [CallerMemberName] string memberName = "")
        {
            Manager.Log(message, FlaggedLoggingLevel.Always, provider.TypeInfo, provider.InstanceInfo, memberName);
        }
    }
}
