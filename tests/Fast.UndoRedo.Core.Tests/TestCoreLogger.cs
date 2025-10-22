using System;
using Fast.UndoRedo.Core.Logging;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Simple test logger implementation used by unit tests to capture log calls.
    /// </summary>
    public sealed class TestCoreLogger : ICoreLogger
    {
        /// <summary>
        /// Last message received by the logger.
        /// </summary>
        public string LastMessage { get; private set; }

        /// <summary>
        /// Last exception received by the logger.
        /// </summary>
        public Exception LastException { get; private set; }

        /// <summary>
        /// Records a message.
        /// </summary>
        /// <param name="message">The message to record.</param>
        public void Log(string message)
        {
            LastMessage = message;
        }

        /// <summary>
        /// Records an exception.
        /// </summary>
        /// <param name="ex">The exception to record.</param>
        public void LogException(Exception ex)
        {
            LastException = ex;
        }
    }
}
