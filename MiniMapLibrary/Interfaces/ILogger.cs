using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMapLibrary
{
    public interface ILogger
    {
        void LogDebug(object data);
        void LogError(object data);
        void LogFatal(object data);
        void LogInfo(object data);
        void LogMessage(object data);
        void LogWarning(object data);
        void LogException(Exception head, string message = "");
    }
}
