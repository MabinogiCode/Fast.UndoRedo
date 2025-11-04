using System;
using System.Collections.Generic;
using System.Linq;
using Fast.UndoRedo.Core.Logging;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Test logger that captures log messages for verification in tests.
    /// </summary>
    internal class TestCoreLogger : ICoreLogger
    {
        private readonly List<string> _messages = new List<string>();
        private readonly List<Exception> _exceptions = new List<Exception>();

        /// <summary>
        /// Gets the list of logged messages.
        /// </summary>
        public IReadOnlyList<string> Messages => _messages;

        /// <summary>
        /// Gets the list of logged exceptions.
        /// </summary>
        public IReadOnlyList<Exception> Exceptions => _exceptions;

        /// <summary>
        /// Gets the last logged message, or null if no messages have been logged.
        /// </summary>
        public string LastMessage => _messages.LastOrDefault();

        /// <summary>
        /// Gets the last logged exception, or null if no exceptions have been logged.
        /// </summary>
        public Exception LastException => _exceptions.LastOrDefault();

        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Log(string message)
        {
            _messages.Add(message);
        }

        /// <summary>
        /// Logs an exception.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        public void LogException(Exception ex)
        {
            _exceptions.Add(ex);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        public void LogWarning(string message)
        {
            _messages.Add($"WARNING: {message}");
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        public void LogError(string message)
        {
            _messages.Add($"ERROR: {message}");
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The debug message to log.</param>
        public void LogDebug(string message)
        {
            _messages.Add($"DEBUG: {message}");
        }

        /// <summary>
        /// Clears all logged messages and exceptions.
        /// </summary>
        public void Clear()
        {
            _messages.Clear();
            _exceptions.Clear();
        }
    }
}
