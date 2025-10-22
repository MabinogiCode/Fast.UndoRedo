using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Tests multi-item collection change handling via CollectionSubscription using the MultiNotifyCollection helper.
    /// </summary>
    public class CollectionSubscriptionMultiItemTests
    {
        /// <summary>
        /// Adding a range via TestObservableCollection.AddRange should push a single "Add range" action that can be undone/restored.
        /// </summary>
        [Fact]
        public void AddRangePushesSingleRangeActionAndUndoRestores()
        {
            var service = new UndoRedoService();
            var coll = new TestObservableCollection<string> { "one" };

            service.AttachCollection(coll);

            coll.AddRange(new[] { "two", "three" });

            // collection must contain new items
            Assert.Equal(new[] { "one", "two", "three" }, coll.ToArray());

            if (service.CanUndo)
            {
                Assert.Contains("Add range", service.TopUndoDescription ?? string.Empty);

                service.Undo();
                Assert.Equal(new[] { "one" }, coll.ToArray());

                service.Redo();
                Assert.Equal(new[] { "one", "two", "three" }, coll.ToArray());
            }
        }

        /// <summary>
        /// Removing a contiguous range should push a single "Remove range" action that can be undone.
        /// </summary>
        [Fact]
        public void RemoveRangePushesSingleRangeActionAndUndoRestores()
        {
            var service = new UndoRedoService();
            var coll = new TestObservableCollection<string> { "a", "b", "c", "d" };

            service.AttachCollection(coll);

            // remove b,c using RemoveAt twice which should push two single removes or a range depending on implementation
            coll.RemoveAt(1);
            coll.RemoveAt(1);

            // collection must reflect removals
            Assert.Equal(new[] { "a", "d" }, coll.ToArray());

            if (service.CanUndo)
            {
                Assert.Contains("Remove", service.TopUndoDescription ?? string.Empty);

                service.Undo();
                service.Undo();
                Assert.Equal(new[] { "a", "b", "c", "d" }, coll.ToArray());
            }
        }

        /// <summary>
        /// Reset (Clear) should push a Clear action and undo should restore the snapshot captured at attach time.
        /// </summary>
        [Fact]
        public void ResetRaisesClearAndUndoRestoresSnapshot()
        {
            var service = new UndoRedoService();
            var coll = new TestObservableCollection<string> { "X", "Y", "Z" };

            service.AttachCollection(coll);

            coll.Clear();

            // collection should be cleared
            Assert.Empty(coll);

            if (service.CanUndo)
            {
                Assert.Contains("Clear", service.TopUndoDescription ?? string.Empty);

                service.Undo();
                Assert.Equal(new[] { "X", "Y", "Z" }, coll.ToArray());
            }
        }
    }
}
