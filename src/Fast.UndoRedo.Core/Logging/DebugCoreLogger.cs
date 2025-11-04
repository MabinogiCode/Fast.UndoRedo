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

        /// <summary>
        /// Logs a warning message to the debug output.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        public void LogWarning(string message)
        {
            Debug.WriteLine($"WARNING: {message}");
        }

        /// <summary>
        /// Logs an error message to the debug output.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        public void LogError(string message)
        {
            Debug.WriteLine($"ERROR: {message}");
        }

        /// <summary>
        /// Logs a debug message to the debug output.
        /// </summary>
        /// <param name="message">The debug message to log.</param>
        public void LogDebug(string message)
        {
            Debug.WriteLine($"DEBUG: {message}");
        }
    }
}
