using BepInEx.Logging;
using MiniMapLibrary;

namespace MiniMapMod
{
    internal static class Log
    {
        internal static ManualLogSource _logSource;

        internal static void Init(ManualLogSource logSource)
        {
            _logSource = logSource;
        }

        internal static void LogDebug(object data){ if(Settings.LogLevel > MiniMapLibrary.LogLevel.info) _logSource.LogDebug(data); }
        internal static void LogError(object data) => _logSource.LogError(data);
        internal static void LogFatal(object data) => _logSource.LogFatal(data);
        internal static void LogInfo(object data) { if (Settings.LogLevel > MiniMapLibrary.LogLevel.none) _logSource.LogDebug(data); }
        internal static void LogMessage(object data) => _logSource.LogMessage(data);
        internal static void LogWarning(object data) => _logSource.LogWarning(data);
    }
}