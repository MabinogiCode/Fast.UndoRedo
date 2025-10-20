using System;

namespace Fast.UndoRedo.Core.Logging
{
    internal static class LoggerExtensions
    {
        public static void Log(this ICoreLogger logger, string message)
        {
            logger?.Log(message);
        }

        public static void LogException(this ICoreLogger logger, Exception ex)
        {
            logger?.LogException(ex);
        }
    }
}
