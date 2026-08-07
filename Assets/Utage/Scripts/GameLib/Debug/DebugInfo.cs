using UnityEngine;

namespace Utage
{
    public class DebugLogInfo
    {
        public string LogString { get; }
        public string StackTrace { get; }
        public LogType LogType { get; }

        public DebugLogInfo(string logString, string stackTrace, LogType logType)
        {
            LogString = logString;
            StackTrace = stackTrace;
            LogType = logType;
        }

        public bool IsErrorLogType()
        {
            switch (LogType)
            {
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    return true;
                default:
                    return false;
            }
        }
    }
}
