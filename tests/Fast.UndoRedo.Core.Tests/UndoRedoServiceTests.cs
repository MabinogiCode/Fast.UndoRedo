using System.Collections.ObjectModel;
using Fast.UndoRedo.Core;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Tests for UndoRedoService basic push/undo/redo and collection attachment behaviors.
    /// </summary>
    public class UndoRedoServiceTests
    {
        /// <summary>
        /// Basic push/undo/redo functionality for a simple action.
        /// </summary>
        [Fact]
        public void PushUndoRedoWorksForSimpleAction()
        {
            var service = new UndoRedoService();
            var called = false;

            var action = new TestAction(() => called = true, () => called = false, "test");
            service.Push(action);

            Assert.True(service.CanUndo);

            // Undo should call the undo delegate
            service.Undo();
            Assert.True(called);
            Assert.True(service.CanRedo);

            // Redo should call the redo delegate
            service.Redo();
            Assert.False(called);
            Assert.True(service.CanUndo);
        }

        /// <summary>
        /// Verifies that attaching a collection records add/remove operations.
        /// </summary>
        [Fact]
        public void AttachCollectionRecordsAddRemove()
        {
            var service = new UndoRedoService();
            var coll = new ObservableCollection<string>();
            service.AttachCollection(coll);

            coll.Add("a");
            Assert.True(service.CanUndo);

            service.Undo();
            Assert.False(service.CanUndo);
        }
    }
}
