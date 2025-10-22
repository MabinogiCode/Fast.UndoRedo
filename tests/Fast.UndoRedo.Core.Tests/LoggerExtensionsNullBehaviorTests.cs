using System;
using Fast.UndoRedo.Core.Logging;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Tests to ensure extension methods behave as no-ops when the logger is null.
    /// </summary>
    public class LoggerExtensionsNullBehaviorTests
    {
        /// <summary>
        /// Calling Log with a null logger should not throw.
        /// </summary>
        [Fact]
        public void LogWithNullLoggerDoesNotThrow()
        {
            ICoreLogger logger = null;
            var ex = Record.Exception(() => LoggerExtensions.Log(logger, "message"));
            Assert.Null(ex);
        }

        /// <summary>
        /// Calling LogException with a null logger should not throw.
        /// </summary>
        [Fact]
        public void LogExceptionWithNullLoggerDoesNotThrow()
        {
            ICoreLogger logger = null;
            var ex = Record.Exception(() => LoggerExtensions.LogException(logger, new InvalidOperationException("boom")));
            Assert.Null(ex);
        }
    }
}
