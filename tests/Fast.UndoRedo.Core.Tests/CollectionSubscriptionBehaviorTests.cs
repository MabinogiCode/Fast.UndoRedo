using System.Collections.ObjectModel;
using Fast.UndoRedo.Core.Logging;
using System.Linq;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Tests that exercise collection subscription behaviors through the public UndoRedoService API.
    /// </summary>
    public class CollectionSubscriptionBehaviorTests
    {
        /// <summary>
        /// Adding a single item to an attached ObservableCollection should push an Add action which can be undone.
        /// </summary>
        [Fact]
        public void AddSingleItemPushesActionAndUndoRestores()
        {
            var service = new UndoRedoService();
            var coll = new ObservableCollection<string> { "one" };

            service.AttachCollection(coll);

            coll.Add("two");

            Assert.True(service.CanUndo);
            Assert.Contains("Add", service.TopUndoDescription ?? string.Empty);

            service.Undo();
            Assert.Equal(new[] { "one" }, coll);

            service.Redo();
            Assert.Equal(new[] { "one", "two" }, coll);
        }

        /// <summary>
        /// Removing an item should push a Remove action that can be undone.
        /// </summary>
        [Fact]
        public void RemoveSingleItemPushesActionAndUndoRestores()
        {
            var service = new UndoRedoService();
            var coll = new ObservableCollection<string> { "a", "b", "c" };

            service.AttachCollection(coll);

            coll.RemoveAt(1);

            Assert.True(service.CanUndo);
            Assert.Contains("Remove", service.TopUndoDescription ?? string.Empty);

            service.Undo();
            Assert.Equal(new[] { "a", "b", "c" }, coll);
        }

        /// <summary>
        /// Replacing an item should push a Replace action that can be undone.
        /// </summary>
        [Fact]
        public void ReplaceItemPushesActionAndUndoRestores()
        {
            var service = new UndoRedoService();
            var coll = new ObservableCollection<string> { "x", "y" };

            service.AttachCollection(coll);

            coll[1] = "z";

            Assert.True(service.CanUndo);

            service.Undo();
            Assert.Equal(new[] { "x", "y" }, coll);
        }

        /// <summary>
        /// Moving an item should push a Move action that can be undone.
        /// </summary>
        [Fact]
        public void MoveItemPushesActionAndUndoRestores()
        {
            var service = new UndoRedoService();
            var coll = new ObservableCollection<string> { "first", "second", "third" };

            service.AttachCollection(coll);

            coll.Move(0, 2);

            Assert.True(service.CanUndo);
            Assert.Contains("Move", service.TopUndoDescription ?? string.Empty);

            service.Undo();
            Assert.Equal(new[] { "first", "second", "third" }, coll);
        }

        /// <summary>
        /// Clearing the collection should push a Clear action and undo should restore the snapshot.
        /// </summary>
        [Fact]
        public void ClearCollectionPushesClearActionAndUndoRestores()
        {
            var service = new UndoRedoService();
            var coll = new ObservableCollection<string> { "A", "B", "C" };

            service.AttachCollection(coll);

            coll.Clear();

            Assert.True(service.CanUndo);
            Assert.Contains("Clear", service.TopUndoDescription ?? string.Empty);

            service.Undo();
            Assert.Equal(new[] { "A", "B", "C" }, coll);
        }
    }
}
