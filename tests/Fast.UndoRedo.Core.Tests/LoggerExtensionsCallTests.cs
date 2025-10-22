using System;
using Fast.UndoRedo.Core.Logging;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Tests verifying the behavior of the logging extension methods.
    /// </summary>
    public class LoggerExtensionsCallTests
    {
        /// <summary>
        /// Verifies that calling the Log extension forwards the message to the underlying logger.
        /// </summary>
        [Fact]
        public void ExtensionLogCallsUnderlyingLogger()
        {
            var logger = new TestCoreLogger();
            LoggerExtensions.Log(logger, "hello world");
            Assert.Equal("hello world", logger.LastMessage);
        }

        /// <summary>
        /// Verifies that calling the LogException extension forwards the exception to the underlying logger.
        /// </summary>
        [Fact]
        public void ExtensionLogExceptionCallsUnderlyingLogger()
        {
            var logger = new TestCoreLogger();
            var ex = new InvalidOperationException("boom");
            LoggerExtensions.LogException(logger, ex);
            Assert.Same(ex, logger.LastException);
        }
    }
}
