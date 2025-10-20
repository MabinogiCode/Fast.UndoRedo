using System.Collections.ObjectModel;
using Fast.UndoRedo.Core;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    public class UndoRedoServiceTests
    {
        [Fact]
        public void PushUndoRedo_WorksForSimpleAction()
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

        [Fact]
        public void AttachCollection_RecordsAddRemove()
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

    internal class TestAction : IUndoableAction
    {
        private readonly System.Action _undo;
        private readonly System.Action _redo;

        public string Description { get; }

        public TestAction(System.Action undo, System.Action redo, string desc)
        {
            _undo = undo;
            _redo = redo;
            Description = desc;
        }

        public void Undo() => _undo();
        public void Redo() => _redo();
    }
}
