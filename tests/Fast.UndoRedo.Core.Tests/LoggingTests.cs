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
            var ex = Record.Exception(() => logger.Log("test message"));
            Assert.Null(ex);
        }

        /// <summary>
        /// Ensures that calling LogException does not throw an exception.
        /// </summary>
        [Fact]
        public void DebugCoreLoggerLogExceptionDoesNotThrow()
        {
            var logger = new DebugCoreLogger();
            var ex = Record.Exception(() => logger.LogException(new InvalidOperationException("boom")));
            Assert.Null(ex);
        }
    }
}
