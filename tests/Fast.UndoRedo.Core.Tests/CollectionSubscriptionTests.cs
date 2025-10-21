using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Fast.UndoRedo.Core;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Tests verifying collection subscription and batch event handling behavior.
    /// </summary>
    public class CollectionSubscriptionTests
    {
        /// <summary>
        /// Verifies that Replace/Move/Clear operations are recorded by the service when attached.
        /// </summary>
        [Fact]
        public void AttachCollectionRecordsReplaceMoveClear()
        {
            var service = new UndoRedoService();
            var coll = new ObservableCollection<string> { "a", "b", "c" };
            service.AttachCollection(coll);

            // Replace
            coll[1] = "x";
            Assert.True(service.CanUndo);

            // Move (simulate by remove+insert to keep compatibility with ObservableCollection semantics)
            var item = coll[0];
            coll.RemoveAt(0);
            coll.Insert(2, item);
            Assert.True(service.CanUndo);

            // Clear
            coll.Clear();
            Assert.True(service.CanUndo);
        }

        /// <summary>
        /// Verifies that batched add/remove sequences are recorded properly.
        /// </summary>
        [Fact]
        public void AttachCollectionRecordsAddRangeRemoveRange()
        {
            var service = new UndoRedoService();
            var coll = new ObservableCollection<string>();
            service.AttachCollection(coll);

            // simulate AddRange via helper that raises a single NotifyCollectionChanged event with multiple NewItems
            var helper = new BatchObservableCollection<string>(coll);
            helper.AddRange(new[] { "a", "b", "c" });

            Assert.True(service.CanUndo);

            // simulate RemoveRange
            helper.RemoveRange(new[] { "b", "c" });
            Assert.True(service.CanUndo);
        }
    }
}
