using System;

namespace Fast.UndoRedo.Core.Logging
{
    /// <summary>
    /// Extension methods for the <see cref="ICoreLogger"/> interface.
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Logs a message using the logger if it is not null.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The message to log.</param>
        public static void Log(this ICoreLogger logger, string message)
        {
            logger?.Log(message);
        }

        /// <summary>
        /// Logs an exception using the logger if it is not null.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="ex">The exception to log.</param>
        public static void LogException(this ICoreLogger logger, Exception ex)
        {
            logger?.LogException(ex);
        }
    }
}
