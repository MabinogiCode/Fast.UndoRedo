using System;

namespace Fast.UndoRedo.Core.Logging
{
    public interface ICoreLogger
    {
        void Log(string message);
        void LogException(Exception ex);
    }
}
