using System;

namespace Fast.UndoRedo.Core.Logging
{
    /// <summary>
    /// Interface for logging messages and exceptions in the undo/redo core system.
    /// </summary>
    public interface ICoreLogger
    {
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void Log(string message);

        /// <summary>
        /// Logs an exception.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        void LogException(Exception ex);
    }
}
