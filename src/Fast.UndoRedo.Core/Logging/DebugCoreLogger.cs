using System;
using System.Diagnostics;

namespace Fast.UndoRedo.Core.Logging
{
    /// <summary>
    /// Logger implementation that outputs messages to the debug console.
    /// </summary>
    public sealed class DebugCoreLogger : ICoreLogger
    {
        /// <summary>
        /// Logs a message to the debug output.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Log(string message)
        {
            Debug.WriteLine(message);
        }

        /// <summary>
        /// Logs an exception to the debug output.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        public void LogException(Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }
}
