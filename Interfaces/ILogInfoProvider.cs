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
        public static void LogTraceInstanced(
            this ILogInfoProvider provider, 
            string message, 
            LogCategoryFlags logCategoryFlags = LogCategoryFlags.General,
            [CallerMemberName] string memberName = "")
        {
            EAFManager.LogStatic(
                message, 
                FlaggedLoggingLevel.Trace, 
                provider.TypeInfo, 
                logCategoryFlags, 
                provider.InstanceInfo, 
                memberName);
        }


        public static void LogDebugInstanced(
            this ILogInfoProvider provider, 
            string message, 
            LogCategoryFlags logCategoryFlags = LogCategoryFlags.General,
            [CallerMemberName] string memberName = "")
        {
            EAFManager.LogStatic(
                message, 
                FlaggedLoggingLevel.Debug, 
                provider.TypeInfo, 
                logCategoryFlags, 
                provider.InstanceInfo, 
                memberName);
        }


        public static void LogVerboseInstanced(
            this ILogInfoProvider provider, 
            string message, 
            LogCategoryFlags logCategoryFlags = LogCategoryFlags.General,
            [CallerMemberName] string memberName = "")
        {
            EAFManager.LogStatic(
                message, 
                FlaggedLoggingLevel.Verbose, 
                provider.TypeInfo, 
                logCategoryFlags, 
                provider.InstanceInfo,
                memberName);
        }


        public static void LogWarningInstanced(
            this ILogInfoProvider provider, 
            string message,
            LogCategoryFlags logCategoryFlags = LogCategoryFlags.General,
            [CallerMemberName] string memberName = "")
        {
            EAFManager.LogStatic(
                message, 
                FlaggedLoggingLevel.Warning, 
                provider.TypeInfo, 
                logCategoryFlags,
                provider.InstanceInfo,
                memberName);
        }


        public static void LogErrorInstanced(
            this ILogInfoProvider provider,
            string message,
            LogCategoryFlags logCategoryFlags = LogCategoryFlags.General,
            [CallerMemberName] string memberName = "")
        {
            EAFManager.LogStatic(
                message, 
                FlaggedLoggingLevel.Error, 
                provider.TypeInfo, 
                logCategoryFlags,
                provider.InstanceInfo, 
                memberName, 
                false);
        }


        public static void LogAlwaysInstanced(
            this ILogInfoProvider provider, 
            string message, 
            LogCategoryFlags logCategoryFlags = LogCategoryFlags.General,
            [CallerMemberName] string memberName = "")
        {
            EAFManager.LogStatic(
                message, 
                FlaggedLoggingLevel.Always, 
                provider.TypeInfo, 
                logCategoryFlags,
                provider.InstanceInfo, 
                memberName, 
                true);
        }
    }
}
