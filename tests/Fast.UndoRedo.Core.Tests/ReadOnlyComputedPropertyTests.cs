using System;
using System.ComponentModel;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Verifies that attaching an object does not invoke get-only computed properties which may throw when backing state is null.
    /// </summary>
    public class ReadOnlyComputedPropertyTests
    {
        /// <summary>
        /// Attaching an object must not evaluate computed read-only properties that can throw; writable properties should still be tracked.
        /// </summary>
        [Fact]
        public void AttachDoesNotInvokeGetOnlyProperties()
        {
            var service = new UndoRedoService(new TestCoreLogger());
            var vm = new FaultyViewModel();

            // Attach should not throw even though the get-only property would throw if evaluated
            service.Attach(vm);

            // Changing a writable property should still produce an undo action
            vm.Value = 5;

            Assert.True(service.CanUndo);

            service.Undo();
            Assert.Equal(0, vm.Value);
        }
    }
}
