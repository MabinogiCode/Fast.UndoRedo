using Fast.UndoRedo.Core.Logging;
using System;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Tests for the logging extension methods on <see cref="ICoreLogger"/>.
    /// </summary>
    public class LoggerExtensionsTests
    {
        /// <summary>
        /// Verifies calling the extension Log with a null logger does not throw.
        /// </summary>
        [Fact]
        public void LogWithNullLoggerDoesNotThrow()
        {
            ICoreLogger logger = null;
            var ex = Record.Exception(() => LoggerExtensions.Log(logger, "message"));
            Assert.Null(ex);
        }

        /// <summary>
        /// Verifies calling the extension Log with a real logger does not throw.
        /// </summary>
        [Fact]
        public void LogWithLoggerCallsUnderlying()
        {
            var logger = new DebugCoreLogger();
            var ex = Record.Exception(() => LoggerExtensions.Log(logger, "message via extension"));
            Assert.Null(ex);
        }

        /// <summary>
        /// Verifies calling the extension LogException with a null logger does not throw.
        /// </summary>
        [Fact]
        public void LogExceptionWithNullLoggerDoesNotThrow()
        {
            ICoreLogger logger = null;
            var ex = Record.Exception(() => LoggerExtensions.LogException(logger, new InvalidOperationException("boom")));
            Assert.Null(ex);
        }

        /// <summary>
        /// Verifies calling the extension LogException with a real logger does not throw.
        /// </summary>
        [Fact]
        public void LogExceptionWithLoggerCallsUnderlying()
        {
            var logger = new DebugCoreLogger();
            var ex = Record.Exception(() => LoggerExtensions.LogException(logger, new InvalidOperationException("boom")));
            Assert.Null(ex);
        }
    }
}
