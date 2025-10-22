using System;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Edge-case tests for UndoRedoService behavior.
    /// </summary>
    public class UndoRedoServiceEdgeTests
    {
        /// <summary>
        /// Verify Push handles null and does not change stack availability.
        /// </summary>
        [Fact]
        public void PushNullIsIgnoredAndStacksRemainEmpty()
        {
            var service = new Fast.UndoRedo.Core.UndoRedoService();
            service.Push(null);
            Assert.False(service.CanUndo);
            Assert.False(service.CanRedo);
        }

        /// <summary>
        /// Verifies calling Undo/Redo on empty stacks does not throw.
        /// </summary>
        [Fact]
        public void UndoAndRedoWhenEmptyDoNotThrow()
        {
            var service = new Fast.UndoRedo.Core.UndoRedoService();
            service.Undo();
            service.Redo();
            Assert.False(service.CanUndo);
            Assert.False(service.CanRedo);
        }

        /// <summary>
        /// Verifies StackUndo falls back to assignment when owner/property lookup fails.
        /// </summary>
        [Fact]
        public void StackUndoWithNoOwnerFallsBackToAssign()
        {
            var service = new Fast.UndoRedo.Core.UndoRedoService();
            int backing = 1;
            var result = service.StackUndo<int>(null, 42, ref backing, "NonExistent");
            Assert.Equal(42, backing);
            Assert.Equal(42, result);
        }

        /// <summary>
        /// Verify Subscribe throws when passed null observer.
        /// </summary>
        [Fact]
        public void SubscribeThrowsOnNullObserver()
        {
            var service = new Fast.UndoRedo.Core.UndoRedoService();
            Assert.Throws<ArgumentNullException>(() => service.Subscribe(null));
        }
    }
}
