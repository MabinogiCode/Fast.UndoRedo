using Fast.UndoRedo.Core.Logging;
using System;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Tests for the core logging implementations.
    /// </summary>
    public class LoggingTests
    {
        /// <summary>
        /// Ensures that calling Log does not throw an exception.
        /// </summary>
        [Fact]
        public void DebugCoreLoggerLogDoesNotThrow()
        {
            var logger = new DebugCoreLogger();
            logger.Log("test message");
        }

        /// <summary>
        /// Ensures that calling LogException does not throw an exception.
        /// </summary>
        [Fact]
        public void DebugCoreLoggerLogExceptionDoesNotThrow()
        {
            var logger = new DebugCoreLogger();
            logger.LogException(new InvalidOperationException("boom"));
        }
    }
}
