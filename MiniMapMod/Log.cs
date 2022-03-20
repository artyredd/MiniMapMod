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
            _logSource = logSource;
        }

        public void LogDebug(object data){ if(Settings.LogLevel > MiniMapLibrary.LogLevel.info) _logSource.LogDebug(data); }
        public void LogError(object data) => _logSource.LogError(data);
        public void LogFatal(object data) => _logSource.LogFatal(data);
        public void LogInfo(object data) { if (Settings.LogLevel > MiniMapLibrary.LogLevel.none) _logSource.LogDebug(data); }
        public void LogMessage(object data) => _logSource.LogMessage(data);
        public void LogWarning(object data) => _logSource.LogWarning(data);

        public void LogException(Exception head, string message = "")
        {
            LogError(message);

            // log the exception linked list's messages
            // this is only really used for weird event and async method
            // behaviour, most of the time this is unnessesary
            while (head != null)
            {
                LogError($"\t{head.Message}");

                LogError($"\t{head.StackTrace}");

                if (head.InnerException != null)
                {
                    head = head.InnerException;
                }
            }
        }
    }
}