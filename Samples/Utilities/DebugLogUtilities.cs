using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Scripts.Utils
{
    /// <summary>
    /// Utility class to conditionally log based on whether a given DebugLogType is enabled, as specified in DebugLogTypeEnabledMap.
    /// Note: The internal Log function is stripped when DEBUG is not set/defined.
    /// </summary>
    public static class DebugLogUtilities
    {
        public const string DefaultDebugDefine = "DEBUG";
        private const bool AlwaysLogErrors = true;
        private const bool AlwaysLogWarnings = false;
        private const bool AlwaysLogLogs = false;

        private const bool UseInjectedLogFunction = true;

        public enum DebugLogType
        {
            LOG = 0,
            WARNING = 1,
            ERROR = 2,
        }
        
        /// <summary>
        /// Add new log types here, and then enable/disable them in DebugLogTypeEnabledMap below.
        /// </summary>
        public enum DebugInfoType
        {
            TARGETING = 0,
            FX = 1,
            SEATING = 2,
            AVATAR_MANAGER = 3,
            CONNECTION_MANAGER = 4,
        }

        private delegate void DebugLogFunction(string message, Object context);
        private delegate void LogFuction(string message);

        /// <summary>
        /// Specifies whether a given DebugLogType is enabled/disabled.
        /// </summary>
        private static readonly Dictionary<DebugInfoType, bool> DebugLogTypeEnabledMap =
            new Dictionary<DebugInfoType, bool>()
            {
                {DebugInfoType.TARGETING, false},
                {DebugInfoType.FX, false},
                {DebugInfoType.SEATING, false},
                {DebugInfoType.AVATAR_MANAGER, true},
                {DebugInfoType.CONNECTION_MANAGER, true},
            };

        [Conditional(DefaultDebugDefine)]
        public static void Log(DebugInfoType debugInfoType, string message, DebugLogType logType, Object context = null)
        {
            switch (logType)
            {
                case DebugLogType.LOG:
                    Log_Internal(Debug.Log, DebugTools.Log.Info, debugInfoType, message, context, AlwaysLogLogs);
                    break;
                case DebugLogType.WARNING:
                    Log_Internal(Debug.LogWarning, DebugTools.Log.Warning, debugInfoType, message, context, AlwaysLogWarnings);
                    break;
                case DebugLogType.ERROR:
                    Log_Internal(Debug.LogError, DebugTools.Log.Error, debugInfoType, message, context, AlwaysLogErrors);
                    break;
            }
        }
        
        [Conditional(DefaultDebugDefine)]
        public static void Log(DebugInfoType debugInfoType, string message, Object context = null)
        {
            Log_Internal(Debug.Log, DebugTools.Log.Info, debugInfoType, message, context, AlwaysLogLogs);
        }

        [Conditional(DefaultDebugDefine)]
        public static void LogWarning(DebugInfoType debugInfoType, string message, Object context = null)
        {
            Log_Internal(Debug.LogWarning, DebugTools.Log.Warning, debugInfoType, message, context, AlwaysLogWarnings);
        }

        [Conditional(DefaultDebugDefine)]
        public static void LogError(DebugInfoType debugInfoType, string message, Object context = null)
        {
            Log_Internal(Debug.LogError, DebugTools.Log.Error, debugInfoType, message, context, AlwaysLogErrors);
        }

        [Conditional(DefaultDebugDefine)]
        private static void Log_Internal(DebugLogFunction debugLogFunction, LogFuction logFunction, DebugInfoType debugInfoType, string message,
            Object context, bool forceLog = false)
        {
            if (forceLog || (DebugLogTypeEnabledMap.TryGetValue(debugInfoType, out var isEnabled) && isEnabled))
            {
                if (UseInjectedLogFunction)
                {
                    logFunction(message);
                }
                else
                {
                    debugLogFunction(message, context);
                }
            }
        }
    }
}