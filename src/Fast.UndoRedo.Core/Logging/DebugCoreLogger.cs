using System;
using System.Diagnostics;

namespace Fast.UndoRedo.Core.Logging
{
    public sealed class DebugCoreLogger : ICoreLogger
    {
        public void Log(string message)
        {
            Debug.WriteLine(message);
        }

        public void LogException(Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }
}
