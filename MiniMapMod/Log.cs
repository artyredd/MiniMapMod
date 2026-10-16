using BepInEx.Logging;
using MiniMapLibrary;
using System;

namespace MiniMapMod
{
    public class Log : ILogger
    {
        private readonly ManualLogSource _logSource;

        public Log(ManualLogSource logSource)
        {
            _logSource = logSource ?? throw new ArgumentNullException(nameof(logSource));
        }

        // BepInEx filters Debug out of the disk log by default, so debug output goes out as Info.
        public void LogDebug(object data) { if (Settings.DebugLogging) _logSource.LogInfo($"[DEBUG] {data}"); }
        public void LogError(object data) => _logSource.LogError(data);
        public void LogFatal(object data) => _logSource.LogFatal(data);
        public void LogInfo(object data) { if (Settings.LogLevel > MiniMapLibrary.LogLevel.none) _logSource.LogInfo(data); }
        public void LogMessage(object data) => _logSource.LogMessage(data);
        public void LogWarning(object data) => _logSource.LogWarning(data);

        public void LogException(Exception head, string message = "")
        {
            if (!string.IsNullOrEmpty(message))
                LogError(message);
            if (head != null)
                LogError(head);
        }
    }
}
